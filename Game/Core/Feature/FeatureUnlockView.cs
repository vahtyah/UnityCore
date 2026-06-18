using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace StandardAssets
{
    /// <summary>
    /// View hiển thị màn "đã mở khoá feature": cập nhật ảnh/tiêu đề/mô tả
    /// rồi bật UI group khi nhận event PendingUnlock (lúc chuyển cảnh kết thúc).
    /// </summary>
    public class FeatureUnlockView : MonoBehaviour
    {
        [Header("Visuals")]
        [SerializeField]
        private Image[] _img;

        [SerializeField]
        private TMP_Text[] _txtTitle;

        [SerializeField]
        private TMP_Text[] _txtDescription;

        [Header("Events")]
        public UnityEvent onUnlockShown;

        private ModuleFeature _module;

        internal void Initialize(ModuleFeature module)
        {
            _module = module;
        }

        private void Awake()
        {
            SATypedBus.On<Ev.Transition>(OnTransition);
            SATypedBus.On<Ev.FeatureOnPendingUnlock>(OnPendingUnlock);
        }

        private void OnTransition(Ev.Transition e)
        {
            // Khi transition mở (state=false) thì tiêu thụ pending unlock đang chờ.
            if (!e.State)
            {
                SATypedBus.Publish(new Ev.FeatureCmdConsumePendingUnlock());
            }
        }

        private void OnPendingUnlock(Ev.FeatureOnPendingUnlock e)
        {
            int index = e.Index;
            FeatureDefinition featureDefinition = (_module != null) ? _module.GetDefinition(index) : null;
            if (featureDefinition == null)
            {
                return;
            }

            foreach (Image val in _img)
            {
                val.sprite = featureDefinition.sprite;
            }
            foreach (TMP_Text val2 in _txtTitle)
            {
                val2.SetText(featureDefinition.unlockTitle);
            }
            foreach (TMP_Text val3 in _txtDescription)
            {
                val3.SetText(featureDefinition.unlockDescription);
            }
            SA.SetUIGroupVisible(UIFeature.UnlockView, visible: true);
            onUnlockShown?.Invoke();
        }
    }
}
