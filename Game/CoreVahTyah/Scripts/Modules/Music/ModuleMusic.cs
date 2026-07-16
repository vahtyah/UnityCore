using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VahTyah.Inspector;

namespace VahTyah
{
    /// <summary>
    /// Dựng MusicPlayer (persistent dưới holder) + đăng ký <see cref="MusicService"/> vào Services.
    /// Boot SAU ModuleSave và ModuleSettingsScreen (service cần <see cref="SettingsService"/> để đọc cờ + volume).
    /// </summary>
    [CreateAssetMenu(menuName = "VahTyah/Modules/Music", fileName = "Module_Music")]
    [ModuleRequires(typeof(ModuleSettings))]
    public sealed class ModuleMusic : Module
    {
        [BoxGroup("Settings")]
        [Tooltip("Thời gian crossfade khi đổi track (giây).")]
        [SerializeField] private float _crossfade = 0.6f;

        [BoxGroup("Tracks")] public List<MusicEntry> Tracks = new List<MusicEntry>();

        public override UniTask InitializeAsync(Transform holder)
        {
            var go = new GameObject("[MusicPlayer]");
            go.transform.SetParent(holder);
            var player = go.AddComponent<MusicPlayer>();
            player.Init(_crossfade);

            Services.TryGet<SettingsService>(out var settings); // null trong editor/partial boot → coi như bật, volume 1
            Services.Register(new MusicService(player, Tracks, settings));

            return UniTask.CompletedTask;
        }
    }
}
