using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace StandardAssets
{
    /// <summary>
    /// Component (gắn lên canvas riêng) chịu trách nhiệm phát animation bay của item:
    /// spawn prefab từ pool, bay theo đường cong Bezier tới vị trí ItemDisplay, rồi
    /// phát particle/sound/haptic và gửi message "Item.GetPending" khi tới đích.
    /// </summary>
    public class ItemAnimationRunner : MonoBehaviour
    {
        private ModuleItem _config;
        private readonly Dictionary<string, ItemAnimationPool> _pools = new Dictionary<string, ItemAnimationPool>();
        private bool _useDOTween;

        internal void Initialize(ModuleItem config)
        {
            _config = config;
            _useDOTween = DOTweenBridge.IsAvailable();
        }

        /// <summary>
        /// Phát animation cho <paramref name="value"/> item kiểu <paramref name="itemKey"/>
        /// bay từ <paramref name="start"/> tới ItemDisplay tương ứng. Trả về Task hoàn tất
        /// khi tất cả item đã tới đích.
        /// </summary>
        public Task Play(string itemKey, Vector3 start, int value)
        {
            if (!ItemDisplay.TryFind(itemKey, out var position))
            {
                Debug.LogWarning("[ItemAnimation] No ItemDisplay found for '" + itemKey + "'");
                return Task.CompletedTask;
            }
            ItemAnimationPool pool = GetOrCreatePool(itemKey);
            if (pool == null)
            {
                Debug.LogWarning("[ItemAnimation] No prefab configured for '" + itemKey + "'");
                return Task.CompletedTask;
            }
            // Số prefab thực sự spawn bị giới hạn bởi maxPoolSize và số instance còn rảnh.
            int count = Mathf.Clamp(value, 1, _config.maxPoolSize);
            count = Mathf.Min(count, pool.AvailableCount);
            if (count == 0)
            {
                Debug.LogWarning("[ItemAnimation] Pool exhausted for '" + itemKey + "'");
                return Task.CompletedTask;
            }
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            int[] remaining = new int[1] { count };
            int valuePerItem = value / count;
            int remainder = value % count;
            for (int i = 0; i < count; i++)
            {
                if (!pool.TryGet(out var rt))
                {
                    break;
                }
                // Chia tổng value cho các prefab; phần dư rải vào những prefab đầu tiên.
                int itemValue = valuePerItem + ((i < remainder) ? 1 : 0);
                float radius = (count > 1) ? _config.spawnRadius : 0f;
                Vector3 spawn = start + (Vector3)(UnityEngine.Random.insideUnitCircle * radius);
                Vector3 toTarget = position - spawn;
                float magnitude = toTarget.magnitude;
                Vector3 dir = (magnitude > Mathf.Epsilon) ? (toTarget / magnitude) : Vector3.right;
                // Vector vuông góc với hướng bay, dùng để tạo độ cong.
                Vector3 perp = new Vector3(0f - dir.y, dir.x, 0f);
                float curve = Mathf.Min(_config.curveStrength, magnitude * 0.5f);
                float side = (UnityEngine.Random.value > 0.5f) ? 1f : -1f;
                // Hai điểm điều khiển của đường Bezier bậc 3.
                Vector3 ctrl = spawn + dir * (magnitude * 0.25f) + perp * (curve * side) + Vector3.up * (curve * 0.4f);
                Vector3 ctrl2 = position - dir * (magnitude * 0.15f) + perp * (curve * 0.15f * side);
                float screenDiagonal = Mathf.Sqrt((float)(Screen.width * Screen.width + Screen.height * Screen.height));
                float duration = _config.duration * Mathf.Lerp(0.35f, 1f, Mathf.Clamp01(magnitude / screenDiagonal));
                float delay = (float)i * _config.staggerDelay;
                AnimateItem(rt, pool, itemKey, itemValue, spawn, ctrl, ctrl2, position, delay, duration, remaining, tcs);
            }
            return tcs.Task;
        }

        private void AnimateItem(RectTransform rt, ItemAnimationPool pool, string itemKey, int itemValue, Vector3 spawn, Vector3 ctrl0, Vector3 ctrl1, Vector3 targetPos, float delay, float duration, int[] remaining, TaskCompletionSource<bool> tcs)
        {
            float[] t = new float[1];
            Func<float> getter = () => t[0];
            Action<float> setter = x =>
            {
                if (!rt.gameObject.activeSelf)
                {
                    rt.position = spawn;
                    rt.gameObject.SetActive(true);
                }
                t[0] = x;
                float moveT = _config.moveCurve.Evaluate(x);
                rt.position = CubicBezier(spawn, ctrl0, ctrl1, targetPos, moveT);
                float scale = _config.scaleCurve.Evaluate(x);
                rt.localScale = new Vector3(scale, scale, scale);
            };
            Action onComplete = () =>
            {
                // Item đã tới đích: cộng vào giá trị hiện tại (gửi qua bus).
                SATypedBus.Publish(new Ev.ItemGetPending { Key = itemKey, Value = itemValue });

                ModuleItem.ItemDefinition def = FindItem(itemKey);
                if (def != null)
                {
                    if (def.particleEffect.IsSet)
                    {
                        SATypedBus.Publish(new Ev.ParticlePlay
                        {
                            Type = def.particleEffect.value,
                            Position = new Vector3(targetPos.x - (float)Screen.width * 0.5f, targetPos.y - (float)Screen.height * 0.5f, 0f),
                            PositionIsScreenOffset = true,
                            Transform = null
                        });
                    }
                    if (def.collectSound.IsSet)
                    {
                        SATypedBus.Publish(new Ev.SoundPlay { Type = def.collectSound.value, Volume = 1f, Pitch = 1f });
                    }
                    if (def.haptic != HapticType.None)
                    {
                        SATypedBus.Publish(new Ev.HapticPlay { Types = new HapticType[1] { def.haptic }, Force = false });
                    }
                }
                pool.Release(rt);
                remaining[0]--;
                if (remaining[0] <= 0)
                {
                    tcs.TrySetResult(true);
                }
            };
            StartCoroutine(TweenCoroutine(duration, delay, setter, onComplete));
        }

        // Tween thủ công bằng coroutine (dùng khi không phụ thuộc DOTween).
        private static IEnumerator TweenCoroutine(float duration, float delay, Action<float> setter, Action onComplete)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                setter(Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            setter(1f);
            onComplete?.Invoke();
        }

        // Đường cong Bezier bậc 3 từ a -> b qua hai điểm điều khiển c0, c1.
        private static Vector3 CubicBezier(Vector3 a, Vector3 c0, Vector3 c1, Vector3 b, float t)
        {
            float u = 1f - t;
            return u * u * u * a + 3f * u * u * t * c0 + 3f * u * t * t * c1 + t * t * t * b;
        }

        private ItemAnimationPool GetOrCreatePool(string itemKey)
        {
            if (_pools.TryGetValue(itemKey, out var value))
            {
                return value;
            }
            ModuleItem.ItemDefinition def = FindItem(itemKey);
            if (def?.prefab == null)
            {
                return null;
            }
            ItemAnimationPool pool = new ItemAnimationPool(transform, _config.maxPoolSize, def.prefab);
            _pools[itemKey] = pool;
            return pool;
        }

        private ModuleItem.ItemDefinition FindItem(string key)
        {
            foreach (ModuleItem.ItemDefinition item in _config.items)
            {
                if (item.name == key)
                {
                    return item;
                }
            }
            return null;
        }
    }
}
