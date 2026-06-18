using System;
using System.Threading.Tasks;
using UnityEngine;

namespace StandardAssets
{
    /// <summary>
    /// Module quản lý các "feature" mở khoá theo level: tính tiến trình, phát state,
    /// và xử lý unlock đang chờ (pending) để view hiển thị đúng lúc.
    /// </summary>
    [CreateAssetMenu(menuName = "SA/Modules/Feature", fileName = "Module_Feature", order = 7)]
    internal sealed class ModuleFeature : SAModule
    {
        [SerializeField]
        internal FeatureDefinition[] definitions = Array.Empty<FeatureDefinition>();

        [SerializeField]
        private GameObject _unlockViewPrefab;

        private FeatureDefinition[] _sorted;

        private int _pendingUnlockIndex = -1;

        public override Task InitializeAsync()
        {
            _sorted = BuildSorted();
            if (_unlockViewPrefab != null)
            {
                GameObject val = Instantiate(_unlockViewPrefab);
                DontDestroyOnLoad(val);
                SAUIGroupManager.Attach(val, UIFeature.UnlockView);
                val.GetComponent<FeatureUnlockView>()?.Initialize(this);
                val.SetActive(false);
            }
            return Task.CompletedTask;
        }

        public override void Subscribe()
        {
            SATypedBus.On<Ev.FeatureCmdRefresh>(OnRefresh);
            SATypedBus.On<Ev.FeatureCmdConsumePendingUnlock>(OnConsumePendingUnlock);
        }

        private void OnRefresh(Ev.FeatureCmdRefresh e)
        {
            int level = SA.CurrentLevel();
            int index = FindActiveIndex(level);
            if (index < 0)
            {
                SATypedBus.Publish(new Ev.FeatureOnState { Active = false });
                return;
            }

            FeatureDefinition def = _sorted[index];
            int floor = GetFloor(index);
            float pMin = Mathf.InverseLerp(floor, def.rangeMax, level - 1);
            float pMax = Mathf.InverseLerp(floor, def.rangeMax, level);
            bool unlocked = level >= def.rangeMax;
            SATypedBus.Publish(new Ev.FeatureOnState
            {
                Active = true,
                Index = index,
                PMin = pMin,
                PMax = pMax,
                Unlocked = unlocked,
                Sprite = def.sprite,
                SpriteDark = def.spriteDark,
                ConditionText = def.conditionText
            });
            if (unlocked)
            {
                _pendingUnlockIndex = index;
                SATypedBus.Publish(new Ev.FeatureOnUnlocked { Index = index });
            }
        }

        private void OnConsumePendingUnlock(Ev.FeatureCmdConsumePendingUnlock e)
        {
            if (_pendingUnlockIndex < 0)
            {
                e.Reply?.Invoke(false);
                return;
            }
            int pendingUnlockIndex = _pendingUnlockIndex;
            _pendingUnlockIndex = -1;
            SATypedBus.Publish(new Ev.FeatureOnPendingUnlock { Index = pendingUnlockIndex });
            e.Reply?.Invoke(true);
        }

        internal FeatureDefinition GetDefinition(int index)
        {
            return ((uint)index < (uint)_sorted.Length) ? _sorted[index] : null;
        }

        // Tìm feature đang áp dụng cho level: rangeMin <= level <= rangeMax (đã sort theo rangeMax).
        private int FindActiveIndex(int level)
        {
            for (int i = 0; i < _sorted.Length && level >= _sorted[i].rangeMin; i++)
            {
                if (level <= _sorted[i].rangeMax)
                {
                    return i;
                }
            }
            return -1;
        }

        // Mốc dưới của thanh tiến trình cho feature thứ index (nối tiếp feature trước đó).
        private int GetFloor(int index)
        {
            if (index == 0)
            {
                return _sorted[0].rangeMin - 1;
            }
            int prevMax = _sorted[index - 1].rangeMax;
            int curMin = _sorted[index].rangeMin;
            return (curMin < prevMax) ? prevMax : (curMin - 1);
        }

        private FeatureDefinition[] BuildSorted()
        {
            FeatureDefinition[] array = (FeatureDefinition[])definitions.Clone();
            Array.Sort(array, (a, b) => a.rangeMax.CompareTo(b.rangeMax));
            return array;
        }
    }
}
