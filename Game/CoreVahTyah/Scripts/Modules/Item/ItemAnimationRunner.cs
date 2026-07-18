using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;

namespace VahTyah
{
    public class ItemAnimationRunner : MonoBehaviour
    {
        private ModuleItem _config;

        internal void Initialize(ModuleItem config)
        {
            _config = config;
        }

        // Đăng ký sẵn pool (dùng chung PoolService) cho mọi item có Prefab; prewarm theo MaxPoolSize của profile.
        internal void Prewarm()
        {
            foreach (var def in _config.Items)
            {
                if (def.Prefab == null) continue;
                var p = _config.GetProfile(def.Animation);
                Pool.Register(def.Prefab, Mathf.Max(1, p.MaxPoolSize));
            }
        }

        public async UniTask Play(string itemKey, Vector3 start, int value)
        {
            var def = _config.FindItem(itemKey);
            if (def == null || def.Prefab == null)
            {
                CommitAll(itemKey, value);   // không tìm thấy item / thiếu Prefab → cộng thẳng
                return;
            }

            var profile = _config.GetProfile(def.Animation);

            if (profile.Style == ItemAnimationStyle.None)
            {
                CommitAll(itemKey, value);   // profile None → cộng thẳng, không animation
                return;
            }

            if (!ItemDisplay.TryFind(itemKey, out var targetPos))
            {
                CommitAll(itemKey, value);   // không có ItemDisplay → cộng thẳng
                return;
            }

            // PoolService auto-grow nên chỉ cap số mảnh bay = MaxPoolSize; value chia đều, dư rải vào mảnh đầu.
            int maxPool = Mathf.Max(1, profile.MaxPoolSize);
            int count = Mathf.Clamp(value, 1, maxPool);
            int perItem = value / count;
            int remainder = value % count;

            float screenDiag = Mathf.Sqrt(Screen.width * Screen.width + Screen.height * Screen.height);
            var ct = this.GetCancellationTokenOnDestroy();
            var tasks = new List<UniTask>(count);

            for (int i = 0; i < count; i++)
            {
                int itemValue = perItem + (i < remainder ? 1 : 0);
                float radius = count > 1 ? profile.SpawnRadius : 0f;
                float delay = i * profile.StaggerDelay;

                Vector3 spawn, ctrl0, ctrl1, target;
                float dur;

                if (profile.Style == ItemAnimationStyle.PopInPlace)
                {
                    // Bung tại chỗ: đứng yên ngay ItemDisplay (rải nhẹ nếu nhiều mảnh), chỉ scale-pop.
                    spawn = targetPos + (Vector3)(UnityEngine.Random.insideUnitCircle * radius);
                    ctrl0 = ctrl1 = target = spawn;   // Bezier(spawn,spawn,spawn,spawn) = đứng yên
                    dur = profile.Duration;
                }
                else // FlyToTarget
                {
                    spawn = start + (Vector3)(UnityEngine.Random.insideUnitCircle * radius);
                    target = targetPos;

                    Vector3 toTarget = target - spawn;
                    float mag = toTarget.magnitude;
                    Vector3 dir = mag > Mathf.Epsilon ? toTarget / mag : Vector3.right;
                    Vector3 perp = new Vector3(-dir.y, dir.x, 0f);

                    float curveStr = Mathf.Min(profile.CurveStrength, mag * 0.5f);
                    float side = UnityEngine.Random.value > 0.5f ? 1f : -1f;
                    ctrl0 = spawn + dir * (mag * 0.25f) + perp * (curveStr * side) + Vector3.up * (curveStr * 0.4f);
                    ctrl1 = target - dir * (mag * 0.15f) + perp * (curveStr * 0.15f * side);

                    dur = profile.Duration * Mathf.Lerp(0.35f, 1f, Mathf.Clamp01(mag / screenDiag));
                }

                tasks.Add(AnimateOne(def.Prefab, itemKey, itemValue, spawn, ctrl0, ctrl1, target, profile, delay, dur, ct));
            }

            await UniTask.WhenAll(tasks);
        }

        // Bay/bung 1 mảnh: (tuỳ) delay stagger → Spawn từ Pool → LitMotion drive t:0→1 (scaled time) set
        // position theo Bezier + scale theo ScaleCurve. Commit + Despawn trong finally để chạy hết là không
        // rò pending; bị destroy lúc tear-down (go fake-null) thì bỏ qua, ModuleItem reconcile Pending lần load sau.
        private async UniTask AnimateOne(GameObject prefab, string itemKey, int itemValue,
            Vector3 spawn, Vector3 ctrl0, Vector3 ctrl1, Vector3 target,
            ItemAnimationProfile profile, float delay, float duration, CancellationToken ct)
        {
            var move = profile.MoveCurve ?? ItemAnimationProfile.DefaultMove();
            var scale = profile.ScaleCurve ?? ItemAnimationProfile.DefaultScale();

            GameObject go = null;
            try
            {
                if (delay > 0f)
                    await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: ct);

                go = Pool.Spawn(prefab, spawn, Quaternion.identity, transform);
                var rt = (RectTransform)go.transform;
                rt.localScale = Vector3.one * scale.Evaluate(0f);   // set scale đầu tránh pop 1 frame (Despawn không reset)

                var s = new FlyState
                {
                    Rt = rt, Spawn = spawn, Ctrl0 = ctrl0, Ctrl1 = ctrl1, Target = target, Move = move, Scale = scale
                };

                await LMotion.Create(0f, 1f, Mathf.Max(0.0001f, duration))
                    .Bind(s, static (t, st) =>
                    {
                        st.Rt.position = CubicBezier(st.Spawn, st.Ctrl0, st.Ctrl1, st.Target, st.Move.Evaluate(t));
                        float sc = st.Scale.Evaluate(t);
                        st.Rt.localScale = new Vector3(sc, sc, sc);
                    })
                    .ToUniTask(ct);
            }
            catch (OperationCanceledException) { }
            finally
            {
                if (go != null)   // Unity: false nếu chưa spawn (cancel lúc delay) hoặc đã destroy (tear-down)
                {
                    EventBus.Publish(new ItemCommitPending { Key = itemKey, Value = itemValue }).Forget();
                    PlayCollectFeedback(profile, target);
                    Pool.Despawn(go);
                }
            }
        }

        // Phát mỗi khi 1 mảnh chạm đích. Shortcut Sound/Haptic/Particles đều null-safe (no-op nếu module chưa boot).
        // Haptic mặc định không force → HapticService tự gate bằng cooldown, tránh spam khi nhiều mảnh đáp dồn.
        private static void PlayCollectFeedback(ItemAnimationProfile p, Vector3 target)
        {
            if (p.CollectSound != SoundId.None) Sound.Play(p.CollectSound);
            if (p.CollectHaptic != HapticType.None) Haptic.Play(p.CollectHaptic);
            // target là screen-px (canvas overlay). Particles.Play spawn world-space → chỉ khớp nếu effect
            // được author theo toạ độ này / game map screen≈world; nếu lệch cần ScreenToWorld (xem chú thích).
            if (p.CollectParticle != ParticleId.None) Particles.Play(p.CollectParticle, target);
        }

        private static void CommitAll(string key, int value)
        {
            if (value > 0)
                EventBus.Publish(new ItemCommitPending { Key = key, Value = value }).Forget();
        }

        private static Vector3 CubicBezier(Vector3 a, Vector3 c0, Vector3 c1, Vector3 b, float t)
        {
            float u = 1f - t;
            return u * u * u * a + 3f * u * u * t * c0 + 3f * u * t * t * c1 + t * t * t * b;
        }

        private sealed class FlyState
        {
            public RectTransform Rt;
            public Vector3 Spawn, Ctrl0, Ctrl1, Target;
            public AnimationCurve Move, Scale;
        }
    }
}
