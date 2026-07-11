using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace VahTyah
{
    public class LevelDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI[] _texts;
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
            if (_texts == null || _texts.Length == 0) return;

            int level = 0;
            EventBus.Publish(new LevelGet { Reply = v => level = v }).Forget();
            for (var i = 0; i < _texts.Length; i++)
            {
                var text = _texts[i];
                text.SetText(_format, level);
            }
        }
    }
}
