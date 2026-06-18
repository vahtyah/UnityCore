using System;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;

namespace StandardAssets
{
    /// <summary>
    /// Module quản lý hệ thống tim (lives): hồi tim theo thời gian, dùng tim, và tim vô hạn theo thời lượng.
    /// Giao tiếp với phần còn lại của game qua SATypedBus.
    /// </summary>
    [CreateAssetMenu(menuName = "SA/Modules/Heart", fileName = "Module_Heart", order = 10)]
    internal sealed class ModuleHeart : SAModule, ISARemoteConfig
    {
        internal static HeartSaveData saveData = new HeartSaveData();

        [SerializeField]
        private HeartData _data = new HeartData();

        private HeartData _runtimeData;

        private HeartManager heartManager;

        public HeartData EditorData => _data;

        public string RemoteConfigKey => "heart_config";

        // Phát khi số tim thay đổi
        public static event Action OnHeartsChanged;

        // Phát khi trạng thái tim vô hạn thay đổi
        public static event Action OnInfinityChanged;

        public string GetEditorJson()
        {
            return SADataProvider.ToJson(EditorData);
        }

        public override Task InitializeAsync()
        {
            _runtimeData = SADataProvider.Resolve("heart_config", _data);
            CreateHeartManager();
            saveData = Load<HeartSaveData>();
            if (saveData.lastHeartDateBin == 0)
            {
                UpdateSaveTimestamps();
            }
            long timestamp = Stopwatch.GetTimestamp();
            // Nếu tick hiện tại nhỏ hơn tick đã lưu -> có thể máy vừa khởi động lại (Stopwatch reset), hiệu chỉnh lại tim vô hạn
            if (timestamp < saveData.lastHeartTick)
            {
                RecalibrateInfinityHeart();
            }
            CalculateRegeneration();
            return Task.CompletedTask;
        }

        private void CreateHeartManager()
        {
            heartManager = new GameObject("HeartManager").AddComponent<HeartManager>();
            heartManager.Initialize(this);
            UnityEngine.Object.DontDestroyOnLoad(heartManager.gameObject);
        }

        public override void Subscribe()
        {
            SATypedBus.On<Ev.HeartAdd>(OnHeartAdd, -10);
            SATypedBus.On<Ev.HeartUse>(OnHeartUse);
            SATypedBus.On<Ev.HeartGet>(OnGetHeart);
            SATypedBus.On<Ev.HeartCommitSaved>(e => OnCommitSavedHeart(e.Value));
            SATypedBus.On<Ev.HeartIsFull>(OnIsFull);
            SATypedBus.On<Ev.HeartIsInfinity>(OnIsInfinity);
            SATypedBus.On<Ev.HeartGetTimerDisplay>(OnGetTimerDisplay);
            SATypedBus.On<Ev.HeartGetInfinityTimerDisplay>(OnGetInfinityTimerDisplay);
            SATypedBus.On<Ev.HeartGetNextSeconds>(OnGetNextSeconds);
            SATypedBus.On<Ev.HeartAddInfinity>(OnAddInfinityHeart);
            SATypedBus.On<Ev.HeartGetInfinity>(OnGetInfinityHeart);
            SATypedBus.On<Ev.HeartCommitSavedInfinity>(e => OnCommitSavedInfinityHeart(e.Minutes));
            SATypedBus.On<Ev.HeartGetSaved>(e => OnCommitSavedHeart(e.Value));
            SATypedBus.On<Ev.HeartGetInfinitySaved>(e => OnCommitSavedInfinityHeart(e.Minutes));
        }

        public void OnHeartAdd(Ev.HeartAdd e)
        {
            int num = e.Value;
            bool flag = e.Direct;
            bool flag2 = e.ResetTimestamp;
            bool flag3 = IsFull();
            if (flag)
            {
                saveData.hearts.have = Mathf.Min(saveData.hearts.have + num, _runtimeData.maxHearts);
            }
            else
            {
                int num2 = _runtimeData.maxHearts - saveData.hearts.have;
                saveData.hearts.saved = Mathf.Min(saveData.hearts.saved + num, num2);
            }
            bool flag4 = !flag3 && IsFull();
            if ((flag2 && flag) || flag4)
            {
                UpdateSaveTimestamps();
            }
            else
            {
                Save(saveData);
            }
            OnHeartsChanged?.Invoke();
        }

        public void OnHeartUse(Ev.HeartUse e)
        {
            int num = e.Value;
            int num2 = saveData.hearts.have + saveData.hearts.saved;
            if (num2 < num)
            {
                e.Reply?.Invoke(false);
                return;
            }
            bool flag = IsFull();
            int num3 = Mathf.Min(num, saveData.hearts.have);
            saveData.hearts.have -= num3;
            saveData.hearts.saved -= num - num3;
            if (flag && !IsFull())
            {
                UpdateSaveTimestamps();
            }
            else
            {
                Save(saveData);
            }
            OnHeartsChanged?.Invoke();
            e.Reply?.Invoke(true);
        }

        public void OnGetHeart(Ev.HeartGet e)
        {
            int result = (e.WithSaved ? (saveData.hearts.have + saveData.hearts.saved) : saveData.hearts.have);
            e.Reply?.Invoke(result);
        }

        public void OnCommitSavedHeart(int value)
        {
            int num = value;
            int num2 = Mathf.Min(num, saveData.hearts.saved);
            if (num2 > 0)
            {
                saveData.hearts.have += num2;
                saveData.hearts.saved -= num2;
                Save(saveData);
                OnHeartsChanged?.Invoke();
            }
        }

        public void OnIsFull(Ev.HeartIsFull e)
        {
            e.Reply?.Invoke(IsFull());
        }

        public void OnIsInfinity(Ev.HeartIsInfinity e)
        {
            e.Reply?.Invoke(IsInfinityHeart());
        }

        public void OnGetTimerDisplay(Ev.HeartGetTimerDisplay e)
        {
            e.Reply?.Invoke(GetHeartTimerDisplay());
        }

        public void OnGetInfinityTimerDisplay(Ev.HeartGetInfinityTimerDisplay e)
        {
            e.Reply?.Invoke(GetInfinityHeartTimerDisplay());
        }

        public void OnGetNextSeconds(Ev.HeartGetNextSeconds e)
        {
            e.Reply?.Invoke((float)GetNextHeartRemainingSeconds());
        }

        public void OnAddInfinityHeart(Ev.HeartAddInfinity e)
        {
            float num = e.Minutes;
            bool flag = e.Direct;
            long num2 = (long)((double)num * 60.0 * (double)Stopwatch.Frequency);
            if (flag)
            {
                long timestamp = Stopwatch.GetTimestamp();
                long num3 = ComputeNewInfinityEndTick(timestamp, num2);
                saveData.infinityEndTick.have = num3;
                double value = (double)(num3 - timestamp) / (double)Stopwatch.Frequency;
                saveData.infinityEndDateBin = DateTime.UtcNow.AddSeconds(value).ToBinary();
            }
            else
            {
                saveData.infinityEndTick.saved += num2;
            }
            Save(saveData);
            OnInfinityChanged?.Invoke();
        }

        public void OnCommitSavedInfinityHeart(float minutes)
        {
            float num = minutes;
            long val = (long)(num * 60f * (float)Stopwatch.Frequency);
            long num2 = Math.Min(val, saveData.infinityEndTick.saved);
            if (num2 > 0)
            {
                long timestamp = Stopwatch.GetTimestamp();
                long num3 = ComputeNewInfinityEndTick(timestamp, num2);
                saveData.infinityEndTick.have = num3;
                saveData.infinityEndTick.saved -= num2;
                double value = (double)(num3 - timestamp) / (double)Stopwatch.Frequency;
                saveData.infinityEndDateBin = DateTime.UtcNow.AddSeconds(value).ToBinary();
                Save(saveData);
                OnInfinityChanged?.Invoke();
            }
        }

        public void OnGetInfinityHeart(Ev.HeartGetInfinity e)
        {
            long num = (e.WithSaved ? (saveData.infinityEndTick.have + saveData.infinityEndTick.saved) : saveData.infinityEndTick.have);
            e.Reply?.Invoke(num);
        }

        /// <summary>
        /// Tính số tim đã hồi được từ lần cập nhật cuối và gửi lệnh Heart.Add nếu có.
        /// </summary>
        public void CalculateRegeneration()
        {
            int num = saveData.hearts.have + saveData.hearts.saved;
            if (num < _runtimeData.maxHearts)
            {
                long timestamp = Stopwatch.GetTimestamp();
                double secondsSinceLastTimestamp = GetSecondsSinceLastTimestamp(timestamp);
                float num2 = _runtimeData.minutesPerHeart * 60f;
                int num3 = Mathf.Min(_runtimeData.maxHearts - num, (int)(secondsSinceLastTimestamp / (double)num2));
                if (num3 > 0)
                {
                    SATypedBus.Publish(new Ev.HeartAdd { Value = num3, Direct = true, ResetTimestamp = true });
                }
            }
        }

        public double GetNextHeartRemainingSeconds()
        {
            if (IsFull() || IsInfinityHeart())
            {
                return 0.0;
            }
            long timestamp = Stopwatch.GetTimestamp();
            double secondsSinceLastTimestamp = GetSecondsSinceLastTimestamp(timestamp);
            float num = _runtimeData.minutesPerHeart * 60f;
            double num2 = secondsSinceLastTimestamp % (double)num;
            return (double)num - num2;
        }

        private double GetSecondsSinceLastTimestamp(long currentTick)
        {
            // Nếu Stopwatch bị reset (máy khởi động lại), dùng đồng hồ thực để tính, đồng thời chống chỉnh giờ lùi
            if (currentTick < saveData.lastHeartTick)
            {
                DateTime utcNow = DateTime.UtcNow;
                DateTime dateTime = DateTime.FromBinary(saveData.lastHeartDateBin);
                if (utcNow < dateTime)
                {
                    Debug.LogWarning("[Heart] Time-Travel detected (backward). Ignoring elapsed time.");
                    return 0.0;
                }
                return (utcNow - dateTime).TotalSeconds;
            }
            return (double)(currentTick - saveData.lastHeartTick) / (double)Stopwatch.Frequency;
        }

        public string GetHeartTimerDisplay()
        {
            if (IsInfinityHeart())
            {
                return GetInfinityHeartTimerDisplay();
            }
            if (IsFull())
            {
                return "Full";
            }
            return FormatTime(TimeSpan.FromSeconds(GetNextHeartRemainingSeconds()));
        }

        public string GetInfinityHeartTimerDisplay()
        {
            double infinityHeartRemainingSeconds = GetInfinityHeartRemainingSeconds();
            return (infinityHeartRemainingSeconds > 0.0) ? ("∞ " + FormatTime(TimeSpan.FromSeconds(infinityHeartRemainingSeconds))) : string.Empty;
        }

        public bool IsFull()
        {
            return saveData.hearts.have + saveData.hearts.saved >= _runtimeData.maxHearts;
        }

        public bool IsInfinityHeart()
        {
            return GetInfinityHeartRemainingSeconds() > 0.0;
        }

        public double GetInfinityHeartRemainingSeconds()
        {
            if (saveData.infinityEndTick.have == 0)
            {
                return 0.0;
            }
            long timestamp = Stopwatch.GetTimestamp();
            long num = ResolveInfinityEndTick(timestamp);
            if (num <= 0)
            {
                return 0.0;
            }
            double num2 = (double)(num - timestamp) / (double)Stopwatch.Frequency;
            return (num2 > 0.0) ? num2 : 0.0;
        }

        public float GetInfinityHeartTime()
        {
            return (float)GetInfinityHeartRemainingSeconds();
        }

        public void ResetInfinityHeart()
        {
            saveData.infinityEndTick = (have: 0L, saved: 0L);
            saveData.infinityEndDateBin = 0L;
            Save(saveData);
            OnInfinityChanged?.Invoke();
        }

        private long ComputeNewInfinityEndTick(long currentTick, long ticksToAdd)
        {
            long num = ResolveInfinityEndTick(currentTick);
            long num2 = ((num > currentTick) ? num : currentTick);
            return num2 + ticksToAdd;
        }

        private long ResolveInfinityEndTick(long currentTick)
        {
            if (saveData.infinityEndTick.have == 0)
            {
                return 0L;
            }
            if (currentTick >= saveData.lastHeartTick)
            {
                return saveData.infinityEndTick.have;
            }
            // Stopwatch đã reset: quy đổi từ mốc thời gian thực
            if (saveData.infinityEndDateBin == 0)
            {
                return 0L;
            }
            DateTime dateTime = DateTime.FromBinary(saveData.infinityEndDateBin);
            double totalSeconds = (dateTime - DateTime.UtcNow).TotalSeconds;
            return (totalSeconds > 0.0) ? (currentTick + (long)(totalSeconds * (double)Stopwatch.Frequency)) : 0;
        }

        private void RecalibrateInfinityHeart()
        {
            if (saveData.infinityEndDateBin != 0)
            {
                DateTime dateTime = DateTime.FromBinary(saveData.infinityEndDateBin);
                double totalSeconds = (dateTime - DateTime.UtcNow).TotalSeconds;
                if (totalSeconds <= 0.0)
                {
                    saveData.infinityEndTick.have = 0L;
                    saveData.infinityEndDateBin = 0L;
                }
                else
                {
                    long timestamp = Stopwatch.GetTimestamp();
                    saveData.infinityEndTick.have = timestamp + (long)(totalSeconds * (double)Stopwatch.Frequency);
                }
                Save(saveData);
            }
        }

        private void UpdateSaveTimestamps()
        {
            saveData.lastHeartTick = Stopwatch.GetTimestamp();
            saveData.lastHeartDateBin = DateTime.UtcNow.ToBinary();
            Save(saveData);
        }

        // Định dạng thời gian còn lại thành chuỗi hiển thị ngắn gọn (d/h/m/s)
        private static string FormatTime(TimeSpan time)
        {
            if (time.TotalDays >= 1.0)
            {
                return $"{(int)time.TotalDays}d {time.Hours}h";
            }
            if (time.TotalHours >= 1.0)
            {
                return $"{(int)time.TotalHours}h {time.Minutes}m";
            }
            if (time.TotalMinutes >= 1.0)
            {
                return $"{time.Minutes}m {time.Seconds}s";
            }
            return $"{time.Seconds}s";
        }
    }
}
