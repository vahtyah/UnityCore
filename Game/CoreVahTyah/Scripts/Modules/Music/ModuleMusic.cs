using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VahTyah
{
    [CreateAssetMenu(menuName = "VahTyah/Modules/Music", fileName = "Module_Music")]
    public sealed class ModuleMusic : Module
    {
        [Tooltip("Thời gian crossfade khi đổi track (giây).")]
        [SerializeField] private float _crossfade = 0.6f;

        public List<MusicEntry> Tracks = new List<MusicEntry>();

        private const string SaveKey = "music";

        private MusicPlayer _player;
        private MusicSaveData _save;
        private readonly Dictionary<int, AudioClip> _byId = new Dictionary<int, AudioClip>();

        public override UniTask InitializeAsync(Transform holder)
        {
            _save = Services.Get<SaveService>().Load<MusicSaveData>(SaveKey);

            _byId.Clear();
            foreach (var t in Tracks)
                if (t.Clip != null)
                    _byId[(int)t.Id] = t.Clip;

            var go = new GameObject("[MusicPlayer]");
            go.transform.SetParent(holder);
            _player = go.AddComponent<MusicPlayer>();
            _player.Init(_crossfade);
            _player.Configure(_save.Volume, _save.Active);

            return UniTask.CompletedTask;
        }

        public override void Subscribe()
        {
            EventBus.On<MusicPlay>(e =>
            {
                if (_byId.TryGetValue((int)e.Id, out var clip))
                    _player.Play(clip);
            });
            EventBus.On<MusicStop>(_ => _player.Stop());
            EventBus.On<MusicSetVolume>(e =>
            {
                _save.Volume = Mathf.Clamp01(e.Volume);
                _player.SetVolume(_save.Volume);
                PersistAndNotify();
            });
            EventBus.On<MusicSetActive>(e =>
            {
                _save.Active = e.Active;
                _player.SetActive(e.Active);
                PersistAndNotify();
            });
            EventBus.On<MusicGet>(e => e.Reply?.Invoke(_save.Active, _save.Volume));
        }

        private void PersistAndNotify()
        {
            Services.Get<SaveService>().Set(SaveKey, _save);
            EventBus.Publish(new MusicChanged { Active = _save.Active, Volume = _save.Volume }).Forget();
        }
    }
}
