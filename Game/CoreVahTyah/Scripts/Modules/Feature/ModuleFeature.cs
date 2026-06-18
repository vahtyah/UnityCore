using System;
using System.Threading.Tasks;
using UnityEngine;

namespace VahTyah
{
    [CreateAssetMenu(menuName = "VahTyah/Modules/Feature", fileName = "Module_Feature")]
    public sealed class ModuleFeature : Module
    {
        [SerializeField] internal FeatureDefinition[] Definitions = Array.Empty<FeatureDefinition>();
        [SerializeField] private GameObject _unlockViewPrefab;

        private FeatureDefinition[] _sorted;
        private int _pendingIndex = -1;

        public override Task InitializeAsync(Transform holder)
        {
            _sorted = BuildSorted();

            if (_unlockViewPrefab != null)
            {
                var go = Instantiate(_unlockViewPrefab, holder);
                go.GetComponent<FeatureUnlockView>()?.Initialize(this);
                go.SetActive(false);
            }

            return Task.CompletedTask;
        }

        public override void Subscribe()
        {
            EventBus.On<FeatureRefresh>(OnRefresh);
            EventBus.On<FeatureConsumePending>(OnConsumePending);
        }

        private void OnRefresh(FeatureRefresh e)
        {
            int level = GetCurrentLevel();
            int index = FindActiveIndex(level);

            if (index < 0)
            {
                EventBus.Publish(new FeatureState { Active = false });
                return;
            }

            var def = _sorted[index];
            int floor = GetFloor(index);
            float pMin = Mathf.InverseLerp(floor, def.LevelMax, level - 1);
            float pMax = Mathf.InverseLerp(floor, def.LevelMax, level);
            bool unlocked = level >= def.LevelMax;

            EventBus.Publish(new FeatureState
            {
                Active = true,
                Index = index,
                ProgressMin = pMin,
                ProgressMax = pMax,
                Unlocked = unlocked,
                Icon = def.Icon,
                IconDark = def.IconDark,
                ConditionText = def.ConditionText
            });

            if (unlocked)
            {
                _pendingIndex = index;
                EventBus.Publish(new FeatureUnlocked { Index = index });
            }
        }

        private void OnConsumePending(FeatureConsumePending e)
        {
            if (_pendingIndex < 0)
            {
                e.Reply?.Invoke(false);
                return;
            }

            int index = _pendingIndex;
            _pendingIndex = -1;
            EventBus.Publish(new FeaturePendingUnlock { Index = index });
            e.Reply?.Invoke(true);
        }

        internal FeatureDefinition GetDefinition(int index)
        {
            return (uint)index < (uint)_sorted.Length ? _sorted[index] : null;
        }

        private int FindActiveIndex(int level)
        {
            for (int i = 0; i < _sorted.Length && level >= _sorted[i].LevelMin; i++)
            {
                if (level <= _sorted[i].LevelMax)
                    return i;
            }
            return -1;
        }

        private int GetFloor(int index)
        {
            if (index == 0)
                return _sorted[0].LevelMin - 1;

            int prevMax = _sorted[index - 1].LevelMax;
            int curMin = _sorted[index].LevelMin;
            return curMin < prevMax ? prevMax : curMin - 1;
        }

        private FeatureDefinition[] BuildSorted()
        {
            var arr = (FeatureDefinition[])Definitions.Clone();
            Array.Sort(arr, (a, b) => a.LevelMax.CompareTo(b.LevelMax));
            return arr;
        }

        private static int GetCurrentLevel()
        {
            int level = 0;
            EventBus.Publish(new LevelGet { Reply = v => level = v });
            return level;
        }
    }
}
