using System;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VahTyah.Inspector;
using Debug = UnityEngine.Debug;

namespace VahTyah
{
    [CreateAssetMenu(menuName = "VahTyah/Modules/Heart", fileName = "Module_Heart")]
    [ModuleRequires(typeof(ModuleSave), typeof(ModuleCollectFly))]
    public sealed class ModuleHeart : Module
    {
        [BoxGroup("Settings")] [Min(1)] public int MaxHearts = 5;
        [BoxGroup("Settings")] [Min(0.01f)] public float MinutesPerHeart = 1f;

        [BoxGroup("Collect Animation")]
        [Tooltip("Prefab tim bay (UI Image + RectTransform) cho HeartCollect. Thiếu → cộng thẳng.")]
        public GameObject FlyPrefab;
        [BoxGroup("Collect Animation")]
        [Tooltip("Profile animation, khai báo ở ModuleCollectFly.")]
        public CollectAnimId Animation = CollectAnimId.Default;

        private const string SaveKey = "hearts";

        private HeartSaveData _save;

        public override UniTask InitializeAsync(Transform holder)
        {
            _save = Services.Get<SaveService>().Load<HeartSaveData>(SaveKey);

            if (_save.LastRegenTick == 0)
                UpdateTimestamps();

            if (Stopwatch.GetTimestamp() < _save.LastRegenTick)
                RecalibrateInfinity();

            TryRegenerate();

            var go = new GameObject("[HeartTicker]");
            go.transform.SetParent(holder);
            go.hideFlags = HideFlags.HideInHierarchy;
            go.AddComponent<HeartTicker>().Initialize(this);

            if (FlyPrefab != null)
                Services.Get<CollectFlyService>().Prewarm(FlyPrefab, Animation);

            return UniTask.CompletedTask;
        }

        public override void Subscribe()
        {
            EventBus.On<HeartAdd>(OnAdd, -10);
            EventBus.On<HeartUse>(OnUse);
            EventBus.On<HeartGet>(e => e.Reply?.Invoke(_save.Hearts));
            EventBus.On<HeartIsFull>(e => e.Reply?.Invoke(IsFull()));
            EventBus.On<HeartIsInfinity>(e => e.Reply?.Invoke(IsInfinity()));
            EventBus.On<HeartGetTimer>(e => e.Reply?.Invoke(GetTimerDisplay()));
            EventBus.On<HeartAddInfinity>(OnAddInfinity);
            EventBus.On<HeartGetInfinityTimer>(e => e.Reply?.Invoke(GetInfinityDisplay()));
            EventBus.OnAsync<HeartCollect>(OnHeartCollect);
        }

        private UniTask OnHeartCollect(HeartCollect e)
        {
            if (e.Value <= 0) return UniTask.CompletedTask;

            if (FlyPrefab == null || !HeartDisplay.TryFind(out var target))
            {
                OnAdd(new HeartAdd { Value = e.Value, Direct = e.Direct });   // không bay được → cộng thẳng
                return UniTask.CompletedTask;
            }

            Vector3 start = e.From != null
                ? e.From.position
                : new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

            // Mỗi tim đáp → +iv (tim không có pending bucket, cộng thẳng khi chạm).
            return Services.Get<CollectFlyService>().Fly(FlyPrefab, start, target, Animation, e.Value,
                iv => OnAdd(new HeartAdd { Value = iv, Direct = e.Direct }));
        }

        private void OnAdd(HeartAdd e)
        {
            bool wasFull = IsFull();

            // Direct = cộng vượt MaxHearts (thưởng đặc biệt); mặc định cap ở MaxHearts.
            _save.Hearts = e.Direct
                ? _save.Hearts + e.Value
                : Mathf.Min(_save.Hearts + e.Value, MaxHearts);

            if (!wasFull && IsFull())
                UpdateTimestamps();
            else
                Persist();

            EventBus.Publish(new HeartChanged()).Forget();
        }

        private void OnUse(HeartUse e)
        {
            if (IsInfinity())   // đang vô hạn tim → dùng luôn thành công, KHÔNG trừ tim
            {
                e.Reply?.Invoke(true);
                return;
            }

            if (_save.Hearts < e.Value)
            {
                e.Reply?.Invoke(false);
                return;
            }

            bool wasFull = IsFull();
            _save.Hearts -= e.Value;

            if (wasFull && !IsFull())
                UpdateTimestamps();
            else
                Persist();

            EventBus.Publish(new HeartChanged()).Forget();
            e.Reply?.Invoke(true);
        }

        private void OnAddInfinity(HeartAddInfinity e)
        {
            long ticksToAdd = (long)(e.Minutes * 60.0 * Stopwatch.Frequency);
            long now = Stopwatch.GetTimestamp();
            long currentEnd = ResolveInfinityEnd(now);
            long baseT = currentEnd > now ? currentEnd : now;

            _save.InfinityEndTick = baseT + ticksToAdd;
            _save.InfinityEndDateBin = DateTime.UtcNow
                .AddSeconds((double)(_save.InfinityEndTick - now) / Stopwatch.Frequency)
                .ToBinary();

            Persist();
            EventBus.Publish(new HeartInfinityChanged()).Forget();
        }

        internal void TryRegenerate()
        {
            if (_save.Hearts >= MaxHearts) return;

            long now = Stopwatch.GetTimestamp();
            double elapsed = GetElapsedSeconds(now);
            float interval = MinutesPerHeart * 60f;
            int gained = Mathf.Min(MaxHearts - _save.Hearts, (int)(elapsed / interval));

            if (gained > 0)
            {
                _save.Hearts = Mathf.Min(_save.Hearts + gained, MaxHearts);
                UpdateTimestamps();
                EventBus.Publish(new HeartChanged()).Forget();
            }
        }

        internal bool IsFull() => _save.Hearts >= MaxHearts;

        internal bool IsInfinity() => GetInfinityRemaining() > 0.0;

        private double GetNextHeartSeconds()
        {
            if (IsFull() || IsInfinity()) return 0.0;

            long now = Stopwatch.GetTimestamp();
            double elapsed = GetElapsedSeconds(now);
            float interval = MinutesPerHeart * 60f;
            double within = elapsed % interval;
            return interval - within;
        }

        private double GetInfinityRemaining()
        {
            if (_save.InfinityEndTick == 0) return 0.0;

            long now = Stopwatch.GetTimestamp();
            long end = ResolveInfinityEnd(now);
            if (end <= now) return 0.0;

            return (double)(end - now) / Stopwatch.Frequency;
        }

        private double GetElapsedSeconds(long currentTick)
        {
            if (currentTick < _save.LastRegenTick)
            {
                var utcNow = DateTime.UtcNow;
                var saved = DateTime.FromBinary(_save.LastRegenDateBin);
                if (utcNow < saved) return 0.0;
                return (utcNow - saved).TotalSeconds;
            }
            return (double)(currentTick - _save.LastRegenTick) / Stopwatch.Frequency;
        }

        private long ResolveInfinityEnd(long currentTick)
        {
            if (_save.InfinityEndTick == 0) return 0;

            if (currentTick >= _save.LastRegenTick)
                return _save.InfinityEndTick;

            if (_save.InfinityEndDateBin == 0) return 0;

            var endDate = DateTime.FromBinary(_save.InfinityEndDateBin);
            double secs = (endDate - DateTime.UtcNow).TotalSeconds;
            return secs > 0 ? currentTick + (long)(secs * Stopwatch.Frequency) : 0;
        }

        private void RecalibrateInfinity()
        {
            if (_save.InfinityEndDateBin == 0) return;

            var endDate = DateTime.FromBinary(_save.InfinityEndDateBin);
            double secs = (endDate - DateTime.UtcNow).TotalSeconds;
            if (secs <= 0)
            {
                _save.InfinityEndTick = 0;
                _save.InfinityEndDateBin = 0;
            }
            else
            {
                long now = Stopwatch.GetTimestamp();
                _save.InfinityEndTick = now + (long)(secs * Stopwatch.Frequency);
            }
            Persist();
        }

        // Timer gộp (1 field): infinity → "∞ + giờ", full → "Full", else đếm ngược.
        private string GetTimerDisplay()
        {
            if (IsInfinity()) return "∞ " + GetInfinityDisplay();
            if (IsFull()) return "Full";
            return FormatTime(TimeSpan.FromSeconds(GetNextHeartSeconds()));
        }

        // Giờ còn của infinity — THÔ (không ∞), để display tự quyết cách trình bày (vd count = "∞", timer = giờ).
        private string GetInfinityDisplay()
        {
            double remaining = GetInfinityRemaining();
            return remaining > 0 ? FormatTime(TimeSpan.FromSeconds(remaining)) : string.Empty;
        }

        private void UpdateTimestamps()
        {
            _save.LastRegenTick = Stopwatch.GetTimestamp();
            _save.LastRegenDateBin = DateTime.UtcNow.ToBinary();
            Persist();
        }

        private void Persist()
        {
            Services.Get<SaveService>().Set(SaveKey, _save);
        }

        private static string FormatTime(TimeSpan t)
        {
            if (t.TotalDays >= 1) return $"{(int)t.TotalDays}d {t.Hours}h";
            if (t.TotalHours >= 1) return $"{(int)t.TotalHours}h {t.Minutes}m";
            if (t.TotalMinutes >= 1) return $"{t.Minutes}m {t.Seconds}s";
            return $"{t.Seconds}s";
        }
    }
}
