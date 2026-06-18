using TMPro;
using UnityEngine;

namespace StandardAssets
{
    /// <summary>
    /// Hiển thị số level hiện tại lên TextMeshPro.
    /// Tự cập nhật khi có transition (chuyển scene/màn).
    /// </summary>
    public class LevelDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _text;

        [Tooltip("Prefix shown before the level number. Leave empty to show only the number.")]
        [SerializeField] private string _prefix = "Level";

        private void Awake()
        {
            UpdateDisplay();
            SATypedBus.On<Ev.Transition>(e =>
            {
                if (e.State)
                    UpdateDisplay();
            });
        }

        private void UpdateDisplay()
        {
            if (_text == null || ModuleLevel.Instance == null) return;

            string displayNumber = ModuleLevel.Instance.GetDisplayNumber();
            _text.SetText(string.IsNullOrEmpty(_prefix) ? displayNumber : _prefix + " " + displayNumber);
        }
    }
}
