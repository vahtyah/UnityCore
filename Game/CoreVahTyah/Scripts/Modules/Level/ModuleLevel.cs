using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VahTyah.Inspector;

namespace VahTyah
{
    [CreateAssetMenu(menuName = "VahTyah/Modules/Level", fileName = "Module_Level")]
    [ModuleRequires(typeof(ModuleSave))]
    public sealed class ModuleLevel : Module
    {
        [BoxGroup("Config")]
        [SerializeField] private LevelDatabaseConfig levelDatabaseConfig;
        
        [BoxGroup("Config")]
        [SerializeField] private List<LevelRange> nonLoopLevels = new List<LevelRange>();

        [BoxGroup("Test Mode")]
        [SerializeField] private bool _enableTestLevel;
        [BoxGroup("Test Mode")]
        [SerializeField] private int _testLevel = 1;

        [BoxGroup("Display")]
        [Tooltip("Prefab UI hiện số level. Spawn 1 lần lúc boot, sống xuyên scene. Để trống = không spawn.\nGắn UIGroup trên prefab nếu muốn ẩn/hiện theo màn.")]
        [SerializeField] private GameObject _displayPrefab;

        private const string SaveKey = "level";

        private LevelSaveData _save;
        private GameObject _displayInstance;
        private List<int> _loopPool;

        private int _totalLevels => levelDatabaseConfig?.Levels?.Length ?? 0;

        // Loop pool chỉ phụ thuộc _config (cố định lúc runtime) → build 1 lần, cache lại.
        private List<int> LoopPool
        {
            get
            {
                if (_loopPool == null) _loopPool = BuildLoopPool();
                return _loopPool;
            }
        }

        public int Current { get; private set; } = 1;

        public int LevelIndex
        {
            get
            {
                if (_enableTestLevel)
                    return (_testLevel - 1) % Mathf.Max(1, _totalLevels);

                if (Current <= _totalLevels)
                    return Mathf.Min(Current - 1, _totalLevels - 1);

                List<int> pool = LoopPool;
                if (pool.Count == 0)
                    return _totalLevels - 1;

                return pool[(Current - _totalLevels - 1) % pool.Count];
            }
        }

        public override UniTask InitializeAsync(Transform holder)
        {
            var save = Services.Get<SaveService>();
            _save = save.Load<LevelSaveData>(SaveKey);
            Current = _save.Level;

            SpawnDisplay(holder);
            return UniTask.CompletedTask;
        }

        private void SpawnDisplay(Transform holder)
        {
            if (_displayPrefab == null) return;
            _displayInstance = Instantiate(_displayPrefab, holder);
        }

        public override void Subscribe()
        {
            EventBus.On<SceneLoaded>(OnSceneLoaded);
            EventBus.On<LevelCompleted>(OnCompleted);
            EventBus.On<LevelStarted>(OnStarted);
            EventBus.On<LevelSet>(OnSet);
            EventBus.On<LevelGet>(e => e.Reply?.Invoke(Current));
            EventBus.On<LevelGetIndex>(e => e.Reply?.Invoke(LevelIndex));
            EventBus.On<LevelGetTries>(e => e.Reply?.Invoke(_save.Tries));
            EventBus.On<TransitionRequest>(OnTransition, -10);
        }

        private void OnSceneLoaded(SceneLoaded obj)
        {
            if(obj.Index == 1) EventBus.Publish(new LevelLoadRequest());
        }

        private void OnCompleted(LevelCompleted e)
        {
            if (_enableTestLevel) return;

            Advance();

            if (!e.ShowScreen)
                Current = _save.Level;
        }

        private void OnStarted(LevelStarted e)
        {
            _save.Tries++;
            Persist();
        }

        private void OnSet(LevelSet e)
        {
            _save.Level = Mathf.Max(1, e.Level);
            _save.Tries = 0;
            Current = _save.Level; // #1: sync ngay để display + LevelIndex phản ánh đúng level vừa set
            Persist();
            EventBus.Publish(new LevelChanged()).Forget();
        }

        private void OnTransition(TransitionRequest e)
        {
            if (e.Cover)
                Current = _save.Level;
        }

        private void Advance()
        {
            Current = _save.Level;
            _save.Level++;
            _save.Tries = 0;
            Persist();
            EventBus.Publish(new LevelChanged()).Forget();
        }

        private void Persist()
        {
            var save = Services.Get<SaveService>();
            save.Set(SaveKey, _save);
        }

        private List<int> BuildLoopPool()
        {
            var pool = new List<int>();
            for (int i = 0; i < _totalLevels; i++)
            {
                int level = i + 1;
                bool excluded = false;
                foreach (var r in nonLoopLevels)
                {
                    if (r.Contains(level)) { excluded = true; break; }
                }
                if (!excluded) pool.Add(i);
            }
            return pool;
        }

        private void OnValidate()
        {
            _testLevel = Mathf.Max(1, _testLevel);
            foreach (var r in nonLoopLevels)
            {
                r.From = Mathf.Max(1, r.From);
                r.To = Mathf.Max(r.From, r.To);
            }
            _loopPool = null; // config đổi trong Editor → build lại pool ở lần đọc kế
        }
    }
}
