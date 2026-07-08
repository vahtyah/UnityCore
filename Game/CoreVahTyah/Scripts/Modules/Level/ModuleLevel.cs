using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VahTyah
{
    [CreateAssetMenu(menuName = "VahTyah/Modules/Level", fileName = "Module_Level")]
    public sealed class ModuleLevel : Module
    {
        [SerializeField] private LevelConfig _config = new LevelConfig();

        [Header("Test Mode")]
        [SerializeField] private bool _enableTestLevel;
        [SerializeField] private int _testLevel = 1;

        [Header("Display")]
        [Tooltip("Prefab UI hiện số level. Spawn 1 lần lúc boot, sống xuyên scene. Để trống = không spawn.\nGắn UIGroup trên prefab nếu muốn ẩn/hiện theo màn.")]
        [SerializeField] private GameObject _displayPrefab;

        private const string SaveKey = "level";

        private LevelSaveData _save;
        private GameObject _displayInstance;

        public static int Current { get; private set; } = 1;

        public int LevelIndex
        {
            get
            {
                if (_enableTestLevel)
                    return (_testLevel - 1) % Mathf.Max(1, _config.TotalLevels);

                if (Current <= _config.TotalLevels)
                    return Mathf.Min(Current - 1, _config.TotalLevels - 1);

                List<int> pool = GetLoopPool();
                if (pool.Count == 0)
                    return _config.TotalLevels - 1;

                return pool[(Current - _config.TotalLevels - 1) % pool.Count];
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
            Persist();
            EventBus.Publish(new LevelChanged { Level = _save.Level }).Forget();
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
            EventBus.Publish(new LevelChanged { Level = _save.Level }).Forget();
        }

        private void Persist()
        {
            var save = Services.Get<SaveService>();
            save.Set(SaveKey, _save);
        }

        private List<int> GetLoopPool()
        {
            var pool = new List<int>();
            for (int i = 0; i < _config.TotalLevels; i++)
            {
                int level = i + 1;
                bool excluded = false;
                foreach (var r in _config.NonLoopLevels)
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
            foreach (var r in _config.NonLoopLevels)
            {
                r.From = Mathf.Max(1, r.From);
                r.To = Mathf.Max(r.From, r.To);
            }
        }
    }
}
