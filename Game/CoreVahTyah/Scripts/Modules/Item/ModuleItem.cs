using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VahTyah.Inspector;

namespace VahTyah
{
    [CreateAssetMenu(menuName = "VahTyah/Modules/Item", fileName = "Module_Item")]
    [ModuleRequires(typeof(ModuleSave))]
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
        }

        [BoxGroup("Items")] public List<ItemDefinition> Items = new List<ItemDefinition>();

        [BoxGroup("Animation")] public float SpawnRadius = 120f;
        [BoxGroup("Animation")] public float StaggerDelay = 0.04f;
        [BoxGroup("Animation")] public float Duration = 1f;
        [BoxGroup("Animation")] public float CurveStrength = 400f;
        [BoxGroup("Animation")] public AnimationCurve MoveCurve = DefaultMoveCurve();
        [BoxGroup("Animation")] public AnimationCurve ScaleCurve = DefaultScaleCurve();
        [BoxGroup("Animation")] public int MaxPoolSize = 20;
        [BoxGroup("Animation")] public int CanvasSortingOrder = 20;

        private const string SaveKey = "items";

        private ItemSaveData _save;
        private ItemAnimationRunner _runner;
        private readonly Dictionary<string, int> _inFlight = new Dictionary<string, int>();

        public override UniTask InitializeAsync(Transform holder)
        {
            var saveService = Services.Get<SaveService>();
            _save = saveService.Load<ItemSaveData>(SaveKey);

            foreach (var def in Items)
            {
                if (def.StartAmount > 0 && !_save.TryGet(def.Key, out _))
                    _save.GetOrCreate(def.Key).Current = def.StartAmount;
            }
            Persist();

            var canvasObj = new GameObject("[ItemAnimation]");
            canvasObj.transform.SetParent(holder);
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CanvasSortingOrder;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            _runner = canvasObj.AddComponent<ItemAnimationRunner>();
            _runner.Initialize(this);

            return UniTask.CompletedTask;
        }

        public override void Subscribe()
        {
            EventBus.On<ItemAdd>(OnAdd, -10);
            EventBus.On<ItemGet>(OnGet);
            EventBus.On<ItemCommitPending>(OnCommitPending, -10);
            EventBus.OnAsync<ItemAnimationPlay>(OnAnimationPlay);
        }

        private void OnAdd(ItemAdd e)
        {
            var entry = _save.GetOrCreate(e.Key);
            if (e.Pending)
                entry.Pending += e.Value;
            else
                entry.Current += e.Value;

            Persist();
            EventBus.Publish(new ItemChanged { Key = e.Key }).Forget();
        }

        private void OnGet(ItemGet e)
        {
            var entry = _save.GetOrCreate(e.Key);
            _inFlight.TryGetValue(e.Key, out int flight);
            int result = e.Pending ? (entry.Pending - flight) : entry.Current;
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
            _inFlight.TryGetValue(e.Key, out int flight);
            _inFlight[e.Key] = flight + e.Value;

            Vector3 start = e.From != null
                ? e.From.position
                : new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

            return _runner.Play(e.Key, start, e.Value);
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

        private static AnimationCurve DefaultMoveCurve() => new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.4f, 0f), new Keyframe(1f, 1f, 4.8f, 4.8f));

        private static AnimationCurve DefaultScaleCurve() => new AnimationCurve(
            new Keyframe(0f, 0.4f, 12f, 12f), new Keyframe(0.1f, 1.5f),
            new Keyframe(0.5f, 1f), new Keyframe(1f, 0.5f, -12f, -12f));
    }
}
