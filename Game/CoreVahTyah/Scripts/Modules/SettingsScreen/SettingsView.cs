using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace VahTyah
{
    public class SettingsView : MonoBehaviour
    {
        [SerializeField] private PopupAnimator _view;
        [SerializeField] private Toggle _musicToggle;
        [SerializeField] private Toggle _sfxToggle;
        [SerializeField] private Toggle _hapticsToggle;
        [SerializeField] private Button _closeButton;
        
        private SettingsService _service;
        
        public void Bind(SettingsService service)
        {
            _service = service;
            _musicToggle.onValueChanged.AddListener(_service.SetSound);
            _sfxToggle.onValueChanged.AddListener(_service.SetSfx);
            _hapticsToggle.onValueChanged.AddListener(_service.SetHaptics);
            _closeButton.onClick.AddListener(Close);
        }

        public void Open()
        {
            _musicToggle.isOn = _service.Sound;
            _sfxToggle.isOn = _service.Sfx;
            _hapticsToggle.isOn = _service.Haptics;
            _view.Show();
        }

        private void Close()
        {
            _ = _service.FlushAsync();
            _view.Hide();
        }
    }
}
