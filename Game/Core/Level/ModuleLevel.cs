using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace StandardAssets
{
    /// <summary>
    /// Module quản lý tiến trình level — đây là nơi THỰC SỰ XỬ LÝ "Level.Completed"/"Level.Failed"
    /// do SA.LevelEnded() phát ra:
    ///   - SA.LevelEnded(completed:true)  -> "Level.Completed" -> OnLevelCompleted() -> Advance() (lên level + lưu)
    ///   - SA.LevelEnded(completed:false) -> "Level.Failed"    -> (module này không tự xử lý fail)
    /// Ngoài ra trả lời các truy vấn Level.Get/GetIndex/GetTries/Set/IsInSequence.
    /// </summary>
    [CreateAssetMenu(menuName = "SA/Modules/Level", fileName = "Module_Level", order = 13)]
    internal sealed class ModuleLevel : SAModule, ISARemoteConfig
    {
        [Header("Progression")]
        [SerializeField] private LevelData _data = new LevelData();
        private LevelData _runtimeData;

        [Header("Test Mode")]
        [Tooltip("When enabled, always loads testLevelIndex regardless of player progression.")]
        [SerializeField] private bool enableTestLevel = false;

        [ShowIf("enableTestLevel")]
        [Tooltip("The level number to always load during test mode (1-based, min 1).")]
        [SerializeField] private int testLevel = 1;

        [Header("Display")]
        [Tooltip("Spawned once at boot and kept alive across scenes.")]
        [SerializeField] private GameObject levelDisplayPrefab;
        private static GameObject _displayInstance;

        public static ModuleLevel Instance { get; set; }

        public LevelData EditorData => _data;

        public string RemoteConfigKey => "level_config";

        internal static LevelSaveData SaveData { get; private set; } = new LevelSaveData();

        public static int Current { get; private set; } = 1;

        // Chỉ số (0-based) của asset level cần load, có xử lý test mode + loop pool.
        private int LevelIndex
        {
            get
            {
                if (enableTestLevel)
                    return (testLevel - 1) % Mathf.Max(1, _runtimeData.totalLevels);

                if (Current <= _runtimeData.totalLevels)
                    return Mathf.Min(Current - 1, _runtimeData.totalLevels - 1);

                List<int> loopPool = GetLoopPool();
                if (loopPool.Count == 0)
                    return _runtimeData.totalLevels - 1;

                return loopPool[(Current - _runtimeData.totalLevels - 1) % loopPool.Count];
            }
        }

        private static int CurrentRunTries => SaveData.currentRunTries;

        public string GetEditorJson() => SADataProvider.ToJson(EditorData);

        private void OnValidate()
        {
            testLevel = Mathf.Max(1, testLevel);
            foreach (LevelRange r in _data.nonLoopLevels)
            {
                r.from = Mathf.Max(1, r.from);
                r.to = Mathf.Max(r.from, r.to);
            }
        }

        public override Task InitializeAsync()
        {
            Instance = this;
            _runtimeData = SADataProvider.Resolve("level_config", _data);
            SaveData = Load<LevelSaveData>();
            Current = SaveData.level;

            if (levelDisplayPrefab != null)
            {
                _displayInstance = Instantiate(levelDisplayPrefab);
                DontDestroyOnLoad(_displayInstance);
                SAUIGroupManager.Attach(_displayInstance, UILevel.LevelDisplay);
            }
            return Task.CompletedTask;
        }

        public override void Subscribe()
        {
            SATypedBus.On<Ev.LevelCompleted>(OnLevelCompleted);
            SATypedBus.On<Ev.LevelStarted>(OnLevelStarted);
            SATypedBus.On<Ev.LevelGet>(OnGetLevel);
            SATypedBus.On<Ev.LevelGetIndex>(OnGetLevelIndex);
            SATypedBus.On<Ev.LevelGetTries>(OnGetLevelTries);
            SATypedBus.On<Ev.LevelSet>(OnSetLevel);
            SATypedBus.On<Ev.Transition>(OnTransition, -10);
            SATypedBus.On<Ev.LevelIsInSequence>(e =>
            {
                bool includeLast = e.IncludeLast;
                bool result = IsInSequence(Current, out LevelSequence seq) && (includeLast || Current < seq.to);
                e.Reply?.Invoke(result);
            });
        }

        private void OnLevelCompleted(Ev.LevelCompleted e)
        {
            if (!enableTestLevel)
            {
                Advance();
                // Nếu không hiện win screen (vd: trong sequence), giữ Current ở giá trị mới đã lưu.
                if (!e.ShowScreen)
                    Current = SaveData.level;
            }
        }

        private void OnLevelStarted(Ev.LevelStarted e)
        {
            SaveData.currentRunTries++;
            Save(SaveData);
        }

        private void OnSetLevel(Ev.LevelSet e)
        {
            SaveData.level = Mathf.Max(1, e.Level);
            SaveData.currentRunTries = 0;
            Save(SaveData);
            SATypedBus.Publish(new Ev.LevelOnChanged { Level = SaveData.level });
        }

        private void OnGetLevel(Ev.LevelGet e) => e.Reply?.Invoke(Current);
        private void OnGetLevelIndex(Ev.LevelGetIndex e) => e.Reply?.Invoke(LevelIndex);
        private void OnGetLevelTries(Ev.LevelGetTries e) => e.Reply?.Invoke(CurrentRunTries);

        private void OnTransition(Ev.Transition e)
        {
            if (e.State)
                Current = SaveData.level;
        }

        // Lên level: lưu Current cũ, tăng level, reset số lần thử, phát "Level.OnChanged".
        private void Advance()
        {
            Current = SaveData.level;
            SaveData.level++;
            SaveData.currentRunTries = 0;
            Save(SaveData);
            SATypedBus.Publish(new Ev.LevelOnChanged { Level = SaveData.level });
        }

        // Danh sách index (0-based) các level được phép lặp (loại bỏ nonLoopLevels).
        private List<int> GetLoopPool()
        {
            List<int> pool = new List<int>();
            for (int i = 0; i < _runtimeData.totalLevels; i++)
            {
                int level = i + 1;
                bool excluded = false;
                foreach (LevelRange r in _runtimeData.nonLoopLevels)
                {
                    if (r.Contains(level)) { excluded = true; break; }
                }
                if (!excluded) pool.Add(i);
            }
            return pool;
        }

        public bool IsInSequence(int level, out LevelSequence seq)
        {
            foreach (LevelSequence s in _runtimeData.sequences)
            {
                if (level >= s.from && level <= s.to)
                {
                    seq = s;
                    return true;
                }
            }
            seq = null;
            return false;
        }

        // Số hiển thị: nếu trong sequence thì hiện dạng "from-to", ngược lại hiện số đơn.
        public string GetDisplayNumber()
        {
            if (IsInSequence(Current, out LevelSequence seq))
            {
                int baseNum = Current / Mathf.Max(1, _runtimeData.totalLevels) * Mathf.Max(1, _runtimeData.totalLevels);
                return $"{baseNum + seq.from}-{baseNum + seq.to}";
            }
            return Current.ToString();
        }
    }
}
