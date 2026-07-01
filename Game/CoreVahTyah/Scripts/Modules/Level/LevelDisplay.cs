using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace VahTyah
{
    /// <summary>
    /// Hiển thị số level hiện tại lên TextMeshPro.
    /// Tự cập nhật khi level đổi hoặc khi transition cover (chuyển màn).
    /// </summary>
    public class LevelDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _text;

        [Tooltip("Tiền tố trước số level. Để trống = chỉ hiện số.")]
        [SerializeField] private string _prefix = "Level";

        private string _format;

        private void Awake()
        {
            _format = string.IsNullOrEmpty(_prefix) ? "{0}" : _prefix + " {0}";
        }

        private void OnEnable()
        {
            UpdateDisplay();
            this.On<LevelChanged>(_ => UpdateDisplay());
            this.On<TransitionRequest>(e => { if (e.Cover) UpdateDisplay(); });
        }

        private void UpdateDisplay()
        {
            if (_text == null) return;

            int level = 0;
            EventBus.Publish(new LevelGet { Reply = v => level = v }).Forget();
            _text.SetText(_format, level);
        }
    }
}
