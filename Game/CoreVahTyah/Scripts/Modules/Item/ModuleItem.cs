using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VahTyah.Inspector;

namespace VahTyah
{
    [CreateAssetMenu(menuName = "VahTyah/Modules/Item", fileName = "Module_Item")]
    [ModuleRequires(typeof(ModuleSave), typeof(ModuleCollectFly))]   // dùng CollectFlyService để bay
    public sealed class ModuleItem : Module
    {
        [Serializable]
        public class ItemDefinition
        {
            public string Key;
            public string Title;
            public Sprite Icon;
            public GameObject Prefab;
            public int StartAmount;

            [Tooltip("Profile animation (khai báo ở ModuleCollectFly). Mặc định = Default.")]
            public CollectAnimId Animation = CollectAnimId.Default;
        }

        [BoxGroup("Items")] public List<ItemDefinition> Items = new List<ItemDefinition>();

        private const string SaveKey = "items";

        private ItemSaveData _save;
        private readonly Dictionary<string, int> _inFlight = new Dictionary<string, int>();

        public override UniTask InitializeAsync(Transform holder)
        {
            _inFlight.Clear();   // SO giữ state runtime giữa các lần Play trong Editor → dọn khi init

            var saveService = Services.Get<SaveService>();
            _save = saveService.Load<ItemSaveData>(SaveKey);

            foreach (var def in Items)
            {
                if (def.StartAmount > 0 && !_save.TryGet(def.Key, out _))
                    _save.GetOrCreate(def.Key).Current = def.StartAmount;
            }

            // Reconcile Pending mồ côi: item đang bay lúc app bị kill → _inFlight (không save) mất,
            // còn Pending (save) kẹt lại. Gộp thẳng vào Current để không mất; luôn zero Pending lúc boot.
            foreach (var entry in _save.Items)
            {
                if (entry.Pending > 0)
                    entry.Current += entry.Pending;
                entry.Pending = 0;
            }
            Persist();

            // Prewarm pool cho từng item qua service bay dùng chung.
            var fly = Services.Get<CollectFlyService>();
            foreach (var def in Items)
                if (def.Prefab != null)
                    fly.Prewarm(def.Prefab, def.Animation);

            return UniTask.CompletedTask;
        }

        public override void Subscribe()
        {
            EventBus.On<ItemAdd>(OnAdd, -10);
            EventBus.On<ItemGet>(OnGet);
            EventBus.On<ItemCommitPending>(OnCommitPending, -10);
            EventBus.OnAsync<ItemAnimationPlay>(OnAnimationPlay);
            EventBus.OnAsync<ItemCollect>(OnCollect);
            EventBus.On<ItemTrySpend>(OnTrySpend, -10);
        }

        private void OnAdd(ItemAdd e)
        {
            var entry = _save.GetOrCreate(e.Key);
            if (e.Pending)
                entry.Pending += e.Value;
            else
                entry.Current = Mathf.Max(0, entry.Current + e.Value);   // Current không xuống âm

            Persist();
            EventBus.Publish(new ItemChanged { Key = e.Key }).Forget();
        }

        private void OnGet(ItemGet e)
        {
            // Query thuần: không GetOrCreate để tránh tạo entry rỗng khi chỉ đọc.
            int current = 0, pending = 0;
            if (_save.TryGet(e.Key, out var entry)) { current = entry.Current; pending = entry.Pending; }
            _inFlight.TryGetValue(e.Key, out int flight);
            int result = e.Pending ? (pending - flight) : current;
            e.Reply?.Invoke(result);
        }

        private void OnCommitPending(ItemCommitPending e)
        {
            var entry = _save.GetOrCreate(e.Key);
            _inFlight.TryGetValue(e.Key, out int flight);
            int amount = Mathf.Min(e.Value, flight);

            entry.Current += amount;
            entry.Pending -= amount;
            _inFlight[e.Key] = flight - amount;

            Persist();
            if (amount > 0)
                EventBus.Publish(new ItemChanged { Key = e.Key }).Forget();
        }

        private UniTask OnAnimationPlay(ItemAnimationPlay e)
        {
            if (e.Value <= 0) return UniTask.CompletedTask;

            _inFlight.TryGetValue(e.Key, out int flight);
            _inFlight[e.Key] = flight + e.Value;

            var def = FindItem(e.Key);
            if (def?.Prefab == null || !ItemDisplay.TryFind(e.Key, out var target))
            {
                // Không bay được → commit thẳng (pending → current), không mất.
                EventBus.Publish(new ItemCommitPending { Key = e.Key, Value = e.Value }).Forget();
                return UniTask.CompletedTask;
            }

            Vector3 start = e.From != null
                ? e.From.position
                : new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

            // Mỗi mảnh đáp → commit pending phần của mảnh đó (currency logic ở LẠI ModuleItem).
            return Services.Get<CollectFlyService>().Fly(def.Prefab, start, target, def.Animation, e.Value,
                iv => EventBus.Publish(new ItemCommitPending { Key = e.Key, Value = iv }).Forget());
        }

        // Safe API: add pending rồi play animation trong 1 nhịp — caller không thể desync pending/inFlight.
        private UniTask OnCollect(ItemCollect e)
        {
            if (e.Value <= 0) return UniTask.CompletedTask;

            OnAdd(new ItemAdd { Key = e.Key, Value = e.Value, Pending = true });
            return OnAnimationPlay(new ItemAnimationPlay { Key = e.Key, From = e.From, Value = e.Value });
        }

        private void OnTrySpend(ItemTrySpend e)
        {
            if (e.Value <= 0 || !_save.TryGet(e.Key, out var entry) || entry.Current < e.Value)
            {
                e.Reply?.Invoke(false);
                return;
            }

            entry.Current -= e.Value;
            Persist();
            EventBus.Publish(new ItemChanged { Key = e.Key }).Forget();
            e.Reply?.Invoke(true);
        }

        private void Persist()
        {
            Services.Get<SaveService>().Set(SaveKey, _save);
        }

        internal ItemDefinition FindItem(string key)
        {
            foreach (var def in Items)
                if (def.Key == key) return def;
            return null;
        }
    }
}
