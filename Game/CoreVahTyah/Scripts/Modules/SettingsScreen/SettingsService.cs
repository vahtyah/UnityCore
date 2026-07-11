using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VahTyah
{
    public class SettingsService
    {
        private const string SaveKey = "settings";

        private readonly SaveService _save;
        private SettingsSaveData _data;

        public SettingsService(SaveService save) => _save = save;

        public bool Sound => _data.Sound;
        public bool Sfx => _data.Sfx;
        public bool Haptics => _data.Haptics;
        public float MusicVolume => _data.MusicVolume;

        public async UniTask InitAsync() => _data = await _save.LoadAsync<SettingsSaveData>(SaveKey);

        public void SetSound(bool on)
        {
            if (_data.Sound == on) return;
            _data.Sound = on;
            Persist();
        }

        public void SetSfx(bool on)
        {
            if (_data.Sfx == on) return;
            _data.Sfx = on;
            Persist();
        }

        public void SetHaptics(bool on)
        {
            if (_data.Haptics == on) return;
            _data.Haptics = on;
            Persist();
        }

        public void SetMusicVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            if (Mathf.Approximately(_data.MusicVolume, volume)) return;
            _data.MusicVolume = volume;
            _save.Set(SaveKey, _data); // volume không phải toggle → chỉ lưu, không publish SettingsChanged
        }

        private void Persist()
        {
            _save.Set(SaveKey, _data);
            EventBus.Publish(new SettingsChanged { Sound = _data.Sound, Sfx = _data.Sfx, Haptics = _data.Haptics });
        }

        public UniTask FlushAsync() => _save.SaveAllAsync();
    }

}