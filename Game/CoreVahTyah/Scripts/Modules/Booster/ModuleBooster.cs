using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VahTyah.Inspector;

namespace VahTyah
{
    [CreateAssetMenu(menuName = "VahTyah/Modules/Booster", fileName = "Module_Booster")]
    [ModuleRequires(typeof(ModuleSave), typeof(ModuleItem))]
    public sealed class ModuleBooster : Module
    {
        [BoxGroup("Boosters")] public List<BoosterDefinition> Boosters = new List<BoosterDefinition>();

        [BoxGroup("Prefabs")] public GameObject BuyPanelPrefab;
        [BoxGroup("Prefabs")] public GameObject BoosterButtonsPrefab;

        private const string SaveKey = "boosters";

        private BoosterSaveData _save;
        private readonly Dictionary<string, bool> _freeFlags = new Dictionary<string, bool>();

        public override UniTask InitializeAsync(Transform holder)
        {
            _freeFlags.Clear();
            _save = Services.Get<SaveService>().Load<BoosterSaveData>(SaveKey);

            foreach (var e in _save.Entries) e.UsesThisLevel = 0;
            Persist();

            if (BuyPanelPrefab != null)
            {
                var go = Instantiate(BuyPanelPrefab, holder);
                go.SetActive(false);
            }

            if (BoosterButtonsPrefab != null)
            {
                Instantiate(BoosterButtonsPrefab, holder);
            }

            return UniTask.CompletedTask;
        }

        public override void Subscribe()
        {
            EventBus.On<BoosterActivate>(OnActivate, -10);
            EventBus.On<BoosterGetState>(OnGetState);
            EventBus.On<BoosterSetFree>(OnSetFree);
            EventBus.On<LevelStarted>(OnLevelStarted);
        }

        private void OnSetFree(BoosterSetFree e)
        {
            if (Find(e.Key) == null) return;
            _freeFlags[e.Key] = true;
            EventBus.Publish(new BoosterChanged { Key = e.Key }).Forget();
        }

        private void OnGetState(BoosterGetState e)
        {
            var def = Find(e.Key);
            if (def == null)
            {
                e.Reply?.Invoke(new BoosterState());
                return;
            }

            var entry = _save.GetOrCreate(e.Key);
            _freeFlags.TryGetValue(e.Key, out bool free);

            e.Reply?.Invoke(new BoosterState
            {
                Exists = true,
                Locked = def.IsLockedAtLevel(GetCurrentLevel()),
                UnlockLevel = def.UnlockLevel,
                Maxed = def.Cap && entry.UsesThisLevel >= def.Prices.Count,
                Free = free,
                Price = def.CalculatePrice(entry.UsesThisLevel),
                HasSeparateCost = def.HasSeparateCost,
                CurrencyKey = def.CurrencyKey,
            });
        }

        private void OnActivate(BoosterActivate e)
        {
            var def = Find(e.Key);
            if (def == null)
            {
                Debug.LogWarning($"[Booster] No definition for key '{e.Key}'.");
                e.Reply?.Invoke(false);
                return;
            }

            if (def.IsLockedAtLevel(GetCurrentLevel()))
            {
                e.Reply?.Invoke(false);
                return;
            }

            var entry = _save.GetOrCreate(e.Key);

            if (def.Cap && entry.UsesThisLevel >= def.Prices.Count)
            {
                e.Reply?.Invoke(false);
                return;
            }

            if (_freeFlags.TryGetValue(e.Key, out bool free) && free)
            {
                _freeFlags[e.Key] = false;
                Commit(entry, e.Key);
                e.Reply?.Invoke(true);
                return;
            }

            int price = def.CalculatePrice(entry.UsesThisLevel);
            if (TrySpend(def.CurrencyKey, price))
            {
                Commit(entry, e.Key);
                e.Reply?.Invoke(true);
                return;
            }

            e.Reply?.Invoke(false);
            EventBus.Publish(new BoosterInsufficient { Key = e.Key }).Forget();

            if (def.OpenShop)
            {
                if (Services.TryGet<UIGroupService>(out var ui))
                    ui.Show(UIGroupId.Shop);
                return;
            }

            EventBus.Publish(new BoosterShowBuyPanel
            {
                Key = def.Key,
                CurrencyKey = def.BuyPanelCurrencyKey,
                Cost = def.BuyPanelPrice,
                BuyAmount = def.BuyAmount,
                AdAmount = def.AdAmount,
                ShowAdButton = def.ShowAdButton && EventBus.HasListeners<AdShowRewarded>(),
                Title = def.Title,
                Description = def.Description,
                Icon = def.Icon,
            }).Forget();
        }

        private void Commit(BoosterSaveData.Entry entry, string key)
        {
            entry.UsesThisLevel++;
            Persist();
            EventBus.Publish(new BoosterActivated { Key = key }).Forget();
            EventBus.Publish(new BoosterChanged { Key = key }).Forget();
        }

        private static bool TrySpend(string currencyKey, int price)
        {
            if (price <= 0) return true;
            bool ok = false;
            EventBus.Publish(new ItemTrySpend { Key = currencyKey, Value = price, Reply = r => ok = r }).Forget();
            return ok;
        }

        private void OnLevelStarted(LevelStarted e)
        {
            foreach (var def in Boosters)
                _save.GetOrCreate(def.Key).UsesThisLevel = 0;
            _freeFlags.Clear();
            Persist();
        }

        private void Persist() => Services.Get<SaveService>().Set(SaveKey, _save);

        private BoosterDefinition Find(string key)
        {
            foreach (var d in Boosters)
                if (d.Key == key)
                    return d;
            return null;
        }

        private static int GetCurrentLevel()
        {
            int level = 0;
            EventBus.Publish(new LevelGet { Reply = v => level = v }).Forget();
            return level;
        }
    }
}