using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace VahTyah
{
    public class LevelDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private string _prefix = "Level";

        private string _format;

        private void Awake()
        {
            _format = string.IsNullOrEmpty(_prefix) ? "{0}" : _prefix + " {0}";
        }

        private void OnEnable()
        {
            UpdateDisplay();
            this.On<LevelLoadRequest>(_ => UpdateDisplay());
        }

        private void UpdateDisplay()
        {
            if (text == null) return;

            int level = 0;
            EventBus.Publish(new LevelGet { Reply = v => level = v }).Forget();
            text.SetText(_format, level);
        }
    }
}
