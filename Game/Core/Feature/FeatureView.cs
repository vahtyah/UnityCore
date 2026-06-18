using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace StandardAssets
{
    /// <summary>
    /// View hiển thị tiến trình của feature hiện tại (ảnh, điều kiện, thanh fill).
    /// Lấy trạng thái qua event Feature.OnState mỗi khi bật.
    /// </summary>
    public class FeatureView : MonoBehaviour
    {
        [Header("Visuals")]
        [SerializeField]
        private Image _img;

        [SerializeField]
        private Image _imgDark;

        [SerializeField]
        private TMP_Text _txtCondition;

        [Header("Events")]
        public UnityEvent onFeatureActive;

        public UnityEvent onFeatureInactive;

        public UnityEvent<float, float> onSetProgress;

        public UnityEvent onFillEnd;

        public UnityEvent onFillComplete;

        private bool _pendingUnlock;

        private bool _stateReceived;

        private void OnEnable()
        {
            _stateReceived = false;
            this.On<Ev.FeatureOnState>(OnState);
            SATypedBus.Publish(new Ev.FeatureCmdRefresh());
            // Nếu refresh đồng bộ không trả state (không có feature active) -> coi như inactive.
            if (!_stateReceived)
            {
                onFeatureInactive?.Invoke();
            }
        }

        private void OnState(Ev.FeatureOnState e)
        {
            _stateReceived = true;
            if (e.Active)
            {
                onFeatureActive?.Invoke();
                _pendingUnlock = e.Unlocked;
                if (_img != null)
                {
                    _img.sprite = e.Sprite;
                }
                if (_imgDark != null)
                {
                    _imgDark.sprite = e.SpriteDark;
                }
                if (_txtCondition != null)
                {
                    _txtCondition.SetText(e.ConditionText);
                }
                onSetProgress?.Invoke(e.PMin, e.PMax);
            }
            else
            {
                onFeatureInactive?.Invoke();
            }
        }

        public void OnFillAnimationEnd()
        {
            onFillEnd?.Invoke();
            if (_pendingUnlock)
            {
                onFillComplete?.Invoke();
            }
        }
    }
}
