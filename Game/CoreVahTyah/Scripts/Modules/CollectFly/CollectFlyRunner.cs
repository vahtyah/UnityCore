using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;

namespace VahTyah
{
    /// <summary>
    /// Component trên canvas <c>[CollectFly]</c>: bay N sprite pooled (PoolService) từ <c>start</c> → <c>targetPos</c>,
    /// gọi <c>onPieceLanded(pieceValue)</c> MỖI mảnh đáp. KHÔNG biết item/heart/currency — việc cộng số do caller
    /// xử lý qua callback (commit pending / add heart / ...). Register gián tiếp qua <see cref="CollectFlyService"/>.
    /// </summary>
    public class CollectFlyRunner : MonoBehaviour
    {
        internal void Prewarm(GameObject prefab, int size)
        {
            if (prefab != null) Pool.Register(prefab, Mathf.Max(1, size));
        }

        public async UniTask Fly(GameObject prefab, Vector3 start, Vector3 targetPos,
            CollectProfile profile, int value, Action<int> onPieceLanded)
        {
            if (prefab == null || profile == null || profile.Style == CollectFlyStyle.None)
            {
                onPieceLanded?.Invoke(value);   // không bay → callback nguyên value
                return;
            }

            int maxPool = Mathf.Max(1, profile.MaxPoolSize);
            int count = Mathf.Clamp(value, 1, maxPool);
            int perItem = value / count;
            int remainder = value % count;

            float screenDiag = Mathf.Sqrt(Screen.width * Screen.width + Screen.height * Screen.height);
            var ct = this.GetCancellationTokenOnDestroy();
            var tasks = new List<UniTask>(count);

            for (int i = 0; i < count; i++)
            {
                int pieceValue = perItem + (i < remainder ? 1 : 0);
                float radius = count > 1 ? profile.SpawnRadius : 0f;
                float delay = i * profile.StaggerDelay;

                Vector3 spawn, ctrl0, ctrl1, target;
                float dur;

                if (profile.Style == CollectFlyStyle.PopInPlace)
                {
                    // Bung tại chỗ: đứng yên ngay đích (rải nhẹ nếu nhiều mảnh), chỉ scale-pop.
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

                tasks.Add(AnimateOne(prefab, pieceValue, spawn, ctrl0, ctrl1, target, profile, delay, dur, onPieceLanded, ct));
            }

            await UniTask.WhenAll(tasks);
        }

        // Bay/bung 1 mảnh: (tuỳ) delay stagger → Spawn từ Pool → LitMotion drive t:0→1 (scaled time) set
        // position theo Bezier + scale theo ScaleCurve. onPieceLanded + feedback + Despawn trong finally để chạy
        // hết là không rò; bị destroy lúc tear-down (go fake-null) thì bỏ qua (caller reconcile nếu cần).
        private async UniTask AnimateOne(GameObject prefab, int pieceValue,
            Vector3 spawn, Vector3 ctrl0, Vector3 ctrl1, Vector3 target,
            CollectProfile profile, float delay, float duration, Action<int> onPieceLanded, CancellationToken ct)
        {
            var move = profile.MoveCurve ?? CollectProfile.DefaultMove();
            var scale = profile.ScaleCurve ?? CollectProfile.DefaultScale();

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
                    onPieceLanded?.Invoke(pieceValue);
                    PlayCollectFeedback(profile, target);
                    Pool.Despawn(go);
                }
            }
        }

        // Phát mỗi khi 1 mảnh chạm đích. Shortcut Sound/Haptic/Particles đều null-safe (no-op nếu module chưa boot).
        // Haptic mặc định không force → HapticService tự gate bằng cooldown, tránh spam khi nhiều mảnh đáp dồn.
        private static void PlayCollectFeedback(CollectProfile p, Vector3 target)
        {
            if (p.CollectSound != SoundId.None) Sound.Play(p.CollectSound);
            if (p.CollectHaptic != HapticType.None) Haptic.Play(p.CollectHaptic);
            // target là screen-px (canvas overlay). Particles.Play spawn world-space → chỉ khớp nếu effect được
            // author theo toạ độ này / game map screen≈world; nếu lệch cần ScreenToWorld.
            if (p.CollectParticle != ParticleId.None) Particles.Play(p.CollectParticle, target);
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
