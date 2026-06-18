using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace VahTyah
{
    public class FeatureUnlockView : MonoBehaviour
    {
        [SerializeField] private Image[] _icons;
        [SerializeField] private TMP_Text[] _titles;
        [SerializeField] private TMP_Text[] _descriptions;

        public UnityEvent OnUnlockShown;

        private ModuleFeature _module;

        internal void Initialize(ModuleFeature module)
        {
            _module = module;
        }

        private void Awake()
        {
            EventBus.On<TransitionRequest>(e =>
            {
                if (!e.Cover)
                    EventBus.Publish(new FeatureConsumePending());
            });

            EventBus.On<FeaturePendingUnlock>(OnPending);
        }

        private void OnPending(FeaturePendingUnlock e)
        {
            var def = _module?.GetDefinition(e.Index);
            if (def == null) return;

            foreach (var img in _icons)
                img.sprite = def.Icon;
            foreach (var txt in _titles)
                txt.SetText(def.UnlockTitle);
            foreach (var txt in _descriptions)
                txt.SetText(def.UnlockDescription);

            gameObject.SetActive(true);
            OnUnlockShown?.Invoke();
        }
    }
}
