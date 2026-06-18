using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace StandardAssets
{
    /// <summary>
    /// Module phát particle theo type id, hỗ trợ cả particle world-space (có pool) lẫn UI.
    /// </summary>
    [CreateAssetMenu(menuName = "SA/Modules/Particle", fileName = "Module_Particle", order = 15)]
    internal sealed class ModuleParticle : SAModule
    {
        [Tooltip("Maximum pooled world-space instances per effect. When full, excess plays are skipped.")]
        [Min(1f)]
        public int MaxPoolSizePerEffect = 5;

        [Header("UI Particles")]
        [Tooltip("Sorting order of the UI particle canvas. Must be higher than your highest game canvas.")]
        public int UICanvasSortingOrder = 20;

        public List<ParticleEntry> Effects = new List<ParticleEntry>();

        private GameObject _parent;
        private RectTransform _uiRoot;
        private List<ParticleSystem>[] _pools;
        private GameObject[] _uiInstances;
        private ParticleEntry[] _indexCache;

        // Type UIParticle (ParticleEffectForUGUI) được resolve bằng reflection vì là dependency tùy chọn.
        private static Type _uiParticleType;

        private static Type UIParticleType
        {
            get
            {
                if (_uiParticleType != null)
                {
                    return _uiParticleType;
                }
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (Assembly assembly in assemblies)
                {
                    _uiParticleType = assembly.GetType("Voodoo.UI.Particles.UIParticle");
                    if (_uiParticleType != null)
                    {
                        break;
                    }
                }
                return _uiParticleType;
            }
        }

        public override Task InitializeAsync()
        {
            _parent = new GameObject("[SA] ParticlePool");
            UnityEngine.Object.DontDestroyOnLoad(_parent);

            GameObject val = new GameObject("[SA] ParticleUICanvas");
            UnityEngine.Object.DontDestroyOnLoad(val);
            Canvas val2 = val.AddComponent<Canvas>();
            val2.renderMode = RenderMode.ScreenSpaceOverlay;
            val2.sortingOrder = UICanvasSortingOrder;
            val.AddComponent<CanvasScaler>();
            _uiRoot = val.GetComponent<RectTransform>();

            _pools = new List<ParticleSystem>[Effects.Count];
            _uiInstances = new GameObject[Effects.Count];
            for (int i = 0; i < Effects.Count; i++)
            {
                _pools[i] = new List<ParticleSystem>();
            }
            return Task.CompletedTask;
        }

        public override void Subscribe()
        {
            SATypedBus.On<Ev.ParticlePlay>(OnPlayParticle);
        }

        private void OnPlayParticle(Ev.ParticlePlay e)
        {
            if (_pools == null)
            {
                return;
            }
            int typeId = e.Type;
            ParticleEntry particleEntry = Get(typeId);
            if (particleEntry != null)
            {
                // Nếu prefab có component UIParticle thì phát theo UI, ngược lại phát world-space.
                if (UIParticleType != null && particleEntry.Prefab != null && particleEntry.Prefab.GetComponent(UIParticleType) != null)
                {
                    PlayUI(e, typeId, particleEntry);
                }
                else
                {
                    PlayWorld(e, typeId, particleEntry);
                }
            }
        }

        private void PlayWorld(Ev.ParticlePlay e, int typeId, ParticleEntry entry)
        {
            if (entry.Prefab == null)
            {
                return;
            }

            Transform val = e.Transform;
            Vector3 position;
            if (val != null)
            {
                position = val.position;
            }
            else
            {
                // PositionIsScreenOffset=true: e.Position là offset từ tâm màn hình (new Vector3(x,y,0) đã sẵn);
                // false: e.Position là world position. Cả hai trường hợp dùng trực tiếp e.Position.
                position = e.Position;
            }

            ParticleSystem pooledWorld = GetPooledWorld(typeId, entry);
            if (pooledWorld != null)
            {
                pooledWorld.transform.position = position;
                pooledWorld.Play();
            }
        }

        private ParticleSystem GetPooledWorld(int idx, ParticleEntry entry)
        {
            List<ParticleSystem> list = _pools[idx];
            foreach (ParticleSystem item in list)
            {
                if (!item.isPlaying)
                {
                    return item;
                }
            }
            if (list.Count >= MaxPoolSizePerEffect)
            {
                Debug.LogWarning($"[SA Particle] Pool full for '{entry.Name}' (max {MaxPoolSizePerEffect}).");
                return null;
            }
            ParticleSystem val = UnityEngine.Object.Instantiate(entry.Prefab, _parent.transform);
            val.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            list.Add(val);
            return val;
        }

        private void PlayUI(Ev.ParticlePlay e, int typeId, ParticleEntry entry)
        {
            Vector2 val2;
            if (e.PositionIsScreenOffset)
            {
                // Offset so với tâm màn hình -> quy về tọa độ screen.
                val2 = new Vector2(e.Position.x + Screen.width * 0.5f, e.Position.y + Screen.height * 0.5f);
            }
            else
            {
                Vector3 val3 = e.Position;
                if (Camera.main == null)
                {
                    Debug.LogWarning("[SA Particle] PlayUI with Vector3 requires a Camera tagged 'MainCamera'.");
                    return;
                }
                val2 = Camera.main.WorldToScreenPoint(val3);
            }

            GameObject orCreateUIInstance = GetOrCreateUIInstance(typeId, entry);
            if (orCreateUIInstance != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_uiRoot, val2, null, out Vector2 val4);
                orCreateUIInstance.transform.localPosition = new Vector3(val4.x, val4.y, 0f);
                UIParticlePlay(orCreateUIInstance);
            }
        }

        private GameObject GetOrCreateUIInstance(int idx, ParticleEntry entry)
        {
            if (_uiInstances[idx] != null)
            {
                return _uiInstances[idx];
            }
            GameObject val = UnityEngine.Object.Instantiate(entry.Prefab.gameObject, _uiRoot);
            _uiInstances[idx] = val;
            return val;
        }

        private static void UIParticlePlay(GameObject go)
        {
            ParticleSystem componentInChildren = go.GetComponentInChildren<ParticleSystem>();
            if (componentInChildren != null)
            {
                componentInChildren.Play();
            }
        }

        public ParticleEntry Get(int typeId)
        {
            if (_indexCache == null || _indexCache.Length != Effects.Count)
            {
                RebuildCache();
            }
            if (typeId < 0 || typeId >= _indexCache.Length)
            {
                Debug.LogWarning($"[SA Particle] ParticleType id {typeId} out of range.");
                return null;
            }
            return _indexCache[typeId];
        }

        private void RebuildCache()
        {
            _indexCache = new ParticleEntry[Effects.Count];
            for (int i = 0; i < Effects.Count; i++)
            {
                _indexCache[i] = Effects[i];
            }
        }

        private void OnValidate()
        {
            _indexCache = null;
        }
    }
}
