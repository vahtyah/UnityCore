using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace VahTyah
{
    public class FeatureView : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private Image _iconDark;
        [SerializeField] private TMP_Text _conditionText;

        public UnityEvent OnActive;
        public UnityEvent OnInactive;
        public UnityEvent<float, float> OnSetProgress;
        public UnityEvent OnFillEnd;
        public UnityEvent OnFillComplete;

        private bool _pendingUnlock;
        private bool _received;

        private void OnEnable()
        {
            _received = false;
            this.On<FeatureState>(OnState);
            EventBus.Publish(new FeatureRefresh());

            if (!_received)
                OnInactive?.Invoke();
        }

        private void OnState(FeatureState e)
        {
            _received = true;

            if (!e.Active)
            {
                OnInactive?.Invoke();
                return;
            }

            OnActive?.Invoke();
            _pendingUnlock = e.Unlocked;

            if (_icon != null) _icon.sprite = e.Icon;
            if (_iconDark != null) _iconDark.sprite = e.IconDark;
            if (_conditionText != null) _conditionText.SetText(e.ConditionText);

            OnSetProgress?.Invoke(e.ProgressMin, e.ProgressMax);
        }

        public void OnFillAnimationEnd()
        {
            OnFillEnd?.Invoke();
            if (_pendingUnlock)
                OnFillComplete?.Invoke();
        }
    }
}
