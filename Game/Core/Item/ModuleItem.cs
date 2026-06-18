using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace StandardAssets
{
    /// <summary>
    /// Module quản lý "item" (tài nguyên đếm được): lưu/cộng/trừ số lượng, animation bay,
    /// và hiển thị qua ItemDisplay. Cấu hình qua ScriptableObject, hỗ trợ remote config.
    /// </summary>
    [CreateAssetMenu(menuName = "SA/Modules/Item", fileName = "Module_Item", order = 12)]
    internal sealed class ModuleItem : SAModule, ISARemoteConfig
    {
        /// <summary>Định nghĩa đầy đủ của một item, bao gồm cả asset Unity (icon, prefab).</summary>
        [Serializable]
        public class ItemDefinition
        {
            public string name;
            public string title;

            [TextArea(1, 3)]
            public string description;

            [Tooltip("Icon-Sprite for ItemTarget display.")]
            public Sprite icon;

            [Tooltip("Prefab that is spawned for animations (needs RectTransform as Root).")]
            public GameObject prefab;

            [SAEnumFilter("Particle")]
            [Tooltip("Particle effect played when this item is used. Tag your enum with [SAEnum(\"Particle\")].")]
            public SAEnumRef particleEffect;

            [SAEnumFilter("Sound")]
            [Tooltip("Sound played when this item is collected. Tag your enum with [SAEnum(\"Sound\")].")]
            public SAEnumRef collectSound;

            public HapticType haptic = HapticType.None;

            [Tooltip("Amount granted once to new players (only applied if no save entry exists yet).")]
            public int startAmount = 0;
        }

        // Dữ liệu save hiện tại (số lượng từng item). Tĩnh để ItemDisplay truy cập.
        internal static SaveDataItem saveDataItem;

        public List<ItemDefinition> items = new List<ItemDefinition>();

        [Header("Item Animation")]
        [Tooltip("Random radius around the start point when spawning.")]
        public float spawnRadius = 120f;

        [Tooltip("Time delay between individual items (seconds).")]
        public float staggerDelay = 0.04f;

        [Tooltip("Flight duration per item (seconds).")]
        public float duration = 1f;

        [Tooltip("Strength of the curve. Larger values = stronger curve.")]
        public float curveStrength = 400f;

        [Tooltip("Easing curve for the movement (X = time 0–1, Y = position 0–1).")]
        public AnimationCurve moveCurve = DefaultMoveCurve();

        [Tooltip("Scale curve (X = time 0–1, Y = scale). For example 0→1.2→1→0 for Pop-In + Absorb.")]
        public AnimationCurve scaleCurve = DefaultScaleCurve();

        [Tooltip("Maximum number of active prefabs at the same time.")]
        public int maxPoolSize = 20;

        [Tooltip("Sorting Order of the animations canvas.")]
        public int canvasSortingOrder = 20;

        [Header("Default Start Position")]
        [Tooltip("Default start position for animations when no Transform is specified (screen coordinates). (0,0) = screen center.")]
        public Vector2 defaultStartPosition;

        [Header("Display")]
        [Tooltip("Spawned once at boot and kept alive across scenes.")]
        public GameObject displayPrefab;

        private ItemAnimationRunner _runner;

        // Số item "đang bay" theo từng key (đã trừ khỏi pending, chưa cộng vào current).
        private readonly Dictionary<string, int> _inFlight = new Dictionary<string, int>();

        private static GameObject _displayInstance;

        public static ModuleItem Instance { get; private set; }

        /// <summary>Vị trí spawn mặc định (mặc định là tâm màn hình nếu chưa cấu hình).</summary>
        public Vector3 DefaultStartPosition => (defaultStartPosition == Vector2.zero)
            ? new Vector3((float)Screen.width * 0.5f, (float)Screen.height * 0.5f, 0f)
            : new Vector3(defaultStartPosition.x, defaultStartPosition.y, 0f);

        public string RemoteConfigKey => "item_config";

        private static AnimationCurve DefaultMoveCurve()
        {
            return new AnimationCurve(new Keyframe[3]
            {
                new Keyframe(0f, 0f, 0f, 0f),
                new Keyframe(0.4f, 0f, 0f, 0f),
                new Keyframe(1f, 1f, 4.808643f, 4.808643f)
            });
        }

        private static AnimationCurve DefaultScaleCurve()
        {
            return new AnimationCurve(new Keyframe[4]
            {
                new Keyframe(0.013832f, 0.42987f, 12.41912f, 12.41912f),
                new Keyframe(0.1f, 1.5f, 5.584559f, 5.584559f),
                new Keyframe(0.5f, 1f, 0f, 0f),
                new Keyframe(1f, 0.5f, -12.73655f, -12.73655f)
            });
        }

        public override Task InitializeAsync()
        {
            Instance = this;
            ItemData data = SADataProvider.Resolve("item_config", GetEditorData());
            ApplyItemData(data);
            saveDataItem = Load<SaveDataItem>();
            // Cấp số lượng khởi đầu cho người chơi mới (chỉ khi chưa có save entry).
            foreach (ItemDefinition item in items)
            {
                if (item.startAmount > 0 && !saveDataItem.TryGet(item.name, out var _))
                {
                    SaveDataItem.ItemEntry orCreate = saveDataItem.GetOrCreate(item.name);
                    orCreate.current = item.startAmount;
                }
            }
            Save(saveDataItem);

            // Tạo canvas riêng (giữ qua scene) để chạy animation item.
            GameObject canvasObj = new GameObject("SA_ItemAnimationCanvas");
            UnityEngine.Object.DontDestroyOnLoad(canvasObj);
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = canvasSortingOrder;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            _runner = canvasObj.AddComponent<ItemAnimationRunner>();
            _runner.Initialize(this);

            if (displayPrefab != null)
            {
                _displayInstance = UnityEngine.Object.Instantiate(displayPrefab);
                UnityEngine.Object.DontDestroyOnLoad(_displayInstance);
                SAUIGroupManager.Attach(_displayInstance, UIItem.ItemDisplay);
            }
            return Task.CompletedTask;
        }

        public override void Subscribe()
        {
            SATypedBus.On<Ev.ItemAdd>(OnItemAdd, -10);
            SATypedBus.On<Ev.ItemGet>(OnItemGet);
            SATypedBus.On<Ev.ItemGetPending>(OnItemGetPending, -10);
            SATypedBus.OnAsync<Ev.ItemAnimationPlay>(OnItemAnimationPlayAsync);
            SATypedBus.On<Ev.ItemShowDisplay>((Ev.ItemShowDisplay e) =>
            {
                if (_displayInstance != null)
                {
                    _displayInstance.SetActive(true);
                }
            });
            SATypedBus.On<Ev.ItemHideDisplay>((Ev.ItemHideDisplay e) =>
            {
                if (_displayInstance != null)
                {
                    _displayInstance.SetActive(false);
                }
            });
            SATypedBus.On<Ev.ItemGetData>(OnItemGetData);
            ItemDisplay.RefreshAll();
        }

        public string GetEditorJson()
        {
            return SADataProvider.ToJson(GetEditorData());
        }

        /// <summary>Trích xuất dữ liệu serialize được từ cấu hình SO (cho remote config / editor).</summary>
        public ItemData GetEditorData()
        {
            ItemData itemData = new ItemData
            {
                spawnRadius = spawnRadius,
                staggerDelay = staggerDelay,
                duration = duration,
                curveStrength = curveStrength,
                maxPoolSize = maxPoolSize,
                canvasSortingOrder = canvasSortingOrder,
                defaultStartPosition = defaultStartPosition
            };
            foreach (ItemDefinition item in items)
            {
                itemData.items.Add(new ItemDefinitionEntry
                {
                    name = item.name,
                    title = item.title,
                    description = item.description,
                    particleEffect = item.particleEffect.GetEnum()?.ToString() ?? string.Empty,
                    collectSound = item.collectSound.GetEnum()?.ToString() ?? string.Empty,
                    haptic = item.haptic,
                    startAmount = item.startAmount
                });
            }
            return itemData;
        }

        // Áp dữ liệu (remote/editor) ngược lại vào cấu hình SO đang chạy.
        private void ApplyItemData(ItemData data)
        {
            spawnRadius = data.spawnRadius;
            staggerDelay = data.staggerDelay;
            duration = data.duration;
            curveStrength = data.curveStrength;
            maxPoolSize = data.maxPoolSize;
            canvasSortingOrder = data.canvasSortingOrder;
            defaultStartPosition = data.defaultStartPosition;
            foreach (ItemDefinitionEntry entry in data.items)
            {
                ItemDefinition def = items.Find(d => d.name == entry.name);
                if (def != null)
                {
                    def.title = entry.title;
                    def.description = entry.description;
                    def.haptic = entry.haptic;
                    def.startAmount = entry.startAmount;
                    if (!string.IsNullOrEmpty(entry.particleEffect))
                    {
                        def.particleEffect = ResolveItemEnumRef(def.particleEffect, entry.particleEffect, "Particle");
                    }
                    if (!string.IsNullOrEmpty(entry.collectSound))
                    {
                        def.collectSound = ResolveItemEnumRef(def.collectSound, entry.collectSound, "Sound");
                    }
                }
            }
        }

        // Dò assembly để tìm enum (có [SAEnum(category)]) chứa value name, rồi tạo SAEnumRef.
        private static SAEnumRef ResolveItemEnumRef(SAEnumRef existing, string name, string category)
        {
            Type type = existing.IsSet ? Type.GetType(existing.typeName) : null;
            if (type == null)
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (Assembly assembly in assemblies)
                {
                    Type[] types = assembly.GetTypes();
                    foreach (Type candidate in types)
                    {
                        if (candidate.IsEnum)
                        {
                            object[] attrs = candidate.GetCustomAttributes(typeof(SAEnumAttribute), inherit: false);
                            if (attrs.Length != 0 && ((SAEnumAttribute)attrs[0]).Category == category && Enum.IsDefined(candidate, name))
                            {
                                type = candidate;
                                break;
                            }
                        }
                    }
                }
            }
            if (type == null)
            {
                return existing;
            }
            try
            {
                SAEnumRef result = default(SAEnumRef);
                result.typeName = type.AssemblyQualifiedName;
                result.value = (int)Enum.Parse(type, name);
                return result;
            }
            catch
            {
                return existing;
            }
        }

        // Cộng item vào current (hoặc pending nếu cờ pending bật).
        private void OnItemAdd(Ev.ItemAdd e)
        {
            string key = e.Key;
            int value = e.Value;
            bool pending = e.Pending;
            SaveDataItem.ItemEntry entry = GetOrCreateItem(key);
            if (!pending)
            {
                entry.current += value;
            }
            else
            {
                entry.pending += value;
            }
            Save(saveDataItem);
            SATypedBus.Publish(new Ev.ItemOnChanged { Key = key });
        }

        // Trả về số lượng hiện tại (hoặc pending còn lại sau khi trừ phần đang bay).
        private void OnItemGet(Ev.ItemGet e)
        {
            string key = e.Key;
            bool pending = e.Pending;
            SaveDataItem.ItemEntry entry = GetOrCreateItem(key);
            _inFlight.TryGetValue(key, out var inFlight);
            int result = pending ? (entry.pending - inFlight) : entry.current;
            e.Reply?.Invoke(result);
        }

        // Chuyển một phần pending (đã bay tới đích) sang current.
        private void OnItemGetPending(Ev.ItemGetPending e)
        {
            string key = e.Key;
            int value = e.Value;
            SaveDataItem.ItemEntry entry = GetOrCreateItem(key);
            _inFlight.TryGetValue(key, out var inFlight);
            int amount = Mathf.Min(value, inFlight);
            entry.current += amount;
            entry.pending -= amount;
            _inFlight[key] = inFlight - amount;
            Save(saveDataItem);
            if (amount > 0)
            {
                SATypedBus.Publish(new Ev.ItemOnChanged { Key = key });
            }
        }

        private void OnItemGetData(Ev.ItemGetData e)
        {
            string key = e.Key;
            ItemDefinition result = null;
            foreach (ItemDefinition item in items)
            {
                if (item.name == key)
                {
                    result = item;
                    break;
                }
            }
            e.Reply?.Invoke(result);
        }

        private SaveDataItem.ItemEntry GetOrCreateItem(string key)
        {
            SaveDataItem.ItemEntry entry = saveDataItem.GetOrCreate(key);
            Save(saveDataItem);
            return entry;
        }

        // Bắt đầu animation: ghi nhận số item đang bay rồi gọi runner.
        private Task OnItemAnimationPlayAsync(Ev.ItemAnimationPlay e)
        {
            string key = e.Key;
            Transform from = e.From;
            int value = e.Value;
            _inFlight.TryGetValue(key, out var inFlight);
            _inFlight[key] = inFlight + value;
            Vector3 start = (from != null) ? from.position : DefaultStartPosition;
            return _runner.Play(key, start, value);
        }
    }
}
