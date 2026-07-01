using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VahTyah
{
    public class ItemAnimationRunner : MonoBehaviour
    {
        private ModuleItem _config;
        private readonly Dictionary<string, ItemAnimationPool> _pools = new Dictionary<string, ItemAnimationPool>();

        internal void Initialize(ModuleItem config)
        {
            _config = config;
        }

        public UniTask Play(string itemKey, Vector3 start, int value)
        {
            if (!ItemDisplay.TryFind(itemKey, out var targetPos))
                return UniTask.CompletedTask;

            var pool = GetOrCreatePool(itemKey);
            if (pool == null)
                return UniTask.CompletedTask;

            int count = Mathf.Clamp(value, 1, _config.MaxPoolSize);
            count = Mathf.Min(count, pool.AvailableCount);
            if (count == 0)
                return UniTask.CompletedTask;

            var tcs = new UniTaskCompletionSource();
            int remaining = count;
            int perItem = value / count;
            int remainder = value % count;

            for (int i = 0; i < count; i++)
            {
                if (!pool.TryGet(out var rt)) break;

                int itemValue = perItem + ((i < remainder) ? 1 : 0);
                float radius = count > 1 ? _config.SpawnRadius : 0f;
                Vector3 spawn = start + (Vector3)(UnityEngine.Random.insideUnitCircle * radius);

                Vector3 toTarget = targetPos - spawn;
                float mag = toTarget.magnitude;
                Vector3 dir = mag > Mathf.Epsilon ? toTarget / mag : Vector3.right;
                Vector3 perp = new Vector3(-dir.y, dir.x, 0f);

                float curve = Mathf.Min(_config.CurveStrength, mag * 0.5f);
                float side = UnityEngine.Random.value > 0.5f ? 1f : -1f;
                Vector3 ctrl0 = spawn + dir * (mag * 0.25f) + perp * (curve * side) + Vector3.up * (curve * 0.4f);
                Vector3 ctrl1 = targetPos - dir * (mag * 0.15f) + perp * (curve * 0.15f * side);

                float screenDiag = Mathf.Sqrt(Screen.width * Screen.width + Screen.height * Screen.height);
                float dur = _config.Duration * Mathf.Lerp(0.35f, 1f, Mathf.Clamp01(mag / screenDiag));
                float delay = i * _config.StaggerDelay;

                StartCoroutine(AnimateRoutine(rt, pool, itemKey, itemValue,
                    spawn, ctrl0, ctrl1, targetPos, delay, dur, () =>
                    {
                        remaining--;
                        if (remaining <= 0) tcs.TrySetResult();
                    }));
            }

            return tcs.Task;
        }

        private IEnumerator AnimateRoutine(RectTransform rt, ItemAnimationPool pool,
            string itemKey, int itemValue,
            Vector3 spawn, Vector3 ctrl0, Vector3 ctrl1, Vector3 target,
            float delay, float duration, Action onDone)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            rt.position = spawn;
            rt.gameObject.SetActive(true);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float moveT = _config.MoveCurve.Evaluate(t);
                rt.position = CubicBezier(spawn, ctrl0, ctrl1, target, moveT);
                float scale = _config.ScaleCurve.Evaluate(t);
                rt.localScale = new Vector3(scale, scale, scale);
                yield return null;
            }

            EventBus.Publish(new ItemCommitPending { Key = itemKey, Value = itemValue }).Forget();
            pool.Release(rt);
            onDone?.Invoke();
        }

        private static Vector3 CubicBezier(Vector3 a, Vector3 c0, Vector3 c1, Vector3 b, float t)
        {
            float u = 1f - t;
            return u * u * u * a + 3f * u * u * t * c0 + 3f * u * t * t * c1 + t * t * t * b;
        }

        private ItemAnimationPool GetOrCreatePool(string itemKey)
        {
            if (_pools.TryGetValue(itemKey, out var pool))
                return pool;

            var def = _config.FindItem(itemKey);
            if (def?.Prefab == null)
                return null;

            pool = new ItemAnimationPool(transform, _config.MaxPoolSize, def.Prefab);
            _pools[itemKey] = pool;
            return pool;
        }
    }
}
