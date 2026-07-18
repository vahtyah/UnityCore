using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VahTyah.Inspector;

namespace VahTyah
{
    [CreateAssetMenu(menuName = "VahTyah/Modules/Item", fileName = "Module_Item")]
    [ModuleRequires(typeof(ModuleSave), typeof(ModulePool))]   // dùng PoolService cho item fly
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

            [Tooltip("Chọn profile animation (khai báo ở AnimationProfiles). Mặc định = Default.")]
            public ItemAnimationId Animation = ItemAnimationId.Default;
        }

        [BoxGroup("Items")] public List<ItemDefinition> Items = new List<ItemDefinition>();

        [BoxGroup("Animation")]
        [Tooltip("Profile animation dùng chung. Nên có 1 profile Id=Default làm fallback.")]
        public List<ItemAnimationProfile> AnimationProfiles = new List<ItemAnimationProfile> { new ItemAnimationProfile() };

        [BoxGroup("Canvas")] public int CanvasSortingOrder = 20;

        private const string SaveKey = "items";

        private ItemSaveData _save;
        private ItemAnimationRunner _runner;
        private readonly Dictionary<string, int> _inFlight = new Dictionary<string, int>();
        private readonly Dictionary<ItemAnimationId, ItemAnimationProfile> _profiles = new Dictionary<ItemAnimationId, ItemAnimationProfile>();
        private ItemAnimationProfile _fallbackProfile;

        public override UniTask InitializeAsync(Transform holder)
        {
            _inFlight.Clear();   // SO giữ state runtime giữa các lần Play trong Editor → dọn khi init
            BuildProfiles();

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

            var canvasObj = new GameObject("[ItemAnimation]");
            canvasObj.transform.SetParent(holder);
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CanvasSortingOrder;

            // Mirror CanvasScaler của HUD (ScaleWithScreenSize, 1080x1920, Expand) để item bay
            // scale đồng nhất giữa các độ phân giải; mặc định ConstantPixelSize làm sprite lệch size.
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            _runner = canvasObj.AddComponent<ItemAnimationRunner>();
            _runner.Initialize(this);
            _runner.Prewarm();

            return UniTask.CompletedTask;
        }

        private void BuildProfiles()
        {
            _profiles.Clear();
            foreach (var p in AnimationProfiles)
            {
                if (p == null) continue;
                _profiles[p.Id] = p;   // last-wins nếu trùng Id
            }

            if (!_profiles.TryGetValue(ItemAnimationId.Default, out _fallbackProfile) || _fallbackProfile == null)
            {
                _fallbackProfile = new ItemAnimationProfile();   // default code nếu thiếu profile Default
                Debug.LogWarning("[Item] Thiếu AnimationProfile 'Default' — dùng default code làm fallback.");
            }
        }

        /// <summary>Profile cho <paramref name="id"/>; thiếu → Default (không bao giờ null sau khi init).</summary>
        internal ItemAnimationProfile GetProfile(ItemAnimationId id)
            => _profiles.TryGetValue(id, out var p) ? p : _fallbackProfile;

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
            if (e.Value <= 0) return UniTask.CompletedTask;   // guard giá trị vô nghĩa

            _inFlight.TryGetValue(e.Key, out int flight);
            _inFlight[e.Key] = flight + e.Value;

            Vector3 start = e.From != null
                ? e.From.position
                : new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

            return _runner.Play(e.Key, start, e.Value);
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
