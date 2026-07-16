using System;
using LitMotion;
using UnityEngine;

namespace VahTyah
{
    /// <summary>
    /// Phát nhạc nền với crossfade (2 AudioSource). Persistent (do ModuleMusic đặt dưới holder).
    ///
    /// Fade dùng LitMotion với <see cref="MotionScheduler.UpdateIgnoreTimeScale"/> — chạy cả khi game pause
    /// (timeScale = 0) và huỷ được, thay cho coroutine + unscaledTime cũ.
    ///
    /// Tách bạch hai thứ:
    ///  - <b>Mix crossfade</b> (<c>_fadeT</c> 0→1) do LitMotion lái.
    ///  - <b>Gain sống</b> = volume × duck × cờ Sound. Đổi bất cứ lúc nào (kể cả GIỮA fade) đều áp ngay
    ///    qua <see cref="ApplyMix"/> — sửa lỗi cũ: đổi volume/mute lúc đang fade bị bỏ qua.
    /// </summary>
    public sealed class MusicPlayer : MonoBehaviour
    {
        private AudioSource _a;
        private AudioSource _b;
        private AudioSource _to;      // slot hiện hành (nghe được khi _playing)
        private AudioSource _from;     // slot đang fade-out (null khi không crossfade)
        private AudioClip _currentClip;
        private bool _playing;

        private float _fade = 0.6f;
        private float _fadeT = 1f;     // 0..1: tiến độ crossfade
        private MotionHandle _fadeMotion;
        private MotionHandle _duckMotion;
        private Action _onFadeDone;

        // Gain sống (SSOT là settings; duck/pause là runtime).
        private float _volume = 1f;    // MusicVolume
        private bool _soundOn = true;  // cờ Sound
        private float _duck = 1f;      // hệ số duck (1 = full)
        private bool _paused;

        private float Gain => _soundOn ? _volume * _duck : 0f;

        public void Init(float fadeDuration)
        {
            _fade = Mathf.Max(0.01f, fadeDuration);

            _a = gameObject.AddComponent<AudioSource>();
            _b = gameObject.AddComponent<AudioSource>();
            foreach (var s in new[] { _a, _b })
            {
                s.loop = true;
                s.playOnAwake = false;
                s.volume = 0f;
            }
            _to = _a;
        }

        // ── Settings (gọi từ MusicService) ─────────────────────────────

        public void Configure(float volume, bool soundOn)
        {
            _volume = Mathf.Clamp01(volume);
            _soundOn = soundOn;
            ApplyMix();
        }

        public void SetActive(bool soundOn)
        {
            _soundOn = soundOn;
            ApplyMix();
        }

        public void SetVolume(float volume)
        {
            _volume = Mathf.Clamp01(volume);
            ApplyMix();
        }

        // ── Playback ───────────────────────────────────────────────────

        public void Play(AudioClip clip)
        {
            if (clip == null) return;
            if (clip == _currentClip && _playing && !_fadeMotion.IsActive()) return;

            _currentClip = clip;

            // Bỏ nguồn fade-out dở dang để chỉ crossfade tối đa 2 nguồn.
            if (_from != null) HardStop(_from);

            if (_playing)
            {
                _from = _to;             // track cũ fade-out
                _to = Other(_to);
            }
            // else: _to đang là slot im lặng, tái dùng làm track mới.

            _to.clip = clip;
            _to.volume = 0f;
            _to.Play();
            if (_paused) _to.Pause();    // giữ nguyên trạng thái pause
            _playing = true;

            StartFade();
        }

        public void Stop()
        {
            if (!_playing && _from == null) return;

            _currentClip = null;
            if (_from != null) HardStop(_from);

            if (_playing)
            {
                _from = _to;             // track hiện hành fade-out
                _to = Other(_to);
            }
            _playing = false;

            StartFade();
        }

        // ── Pause/Resume (game pause) ──────────────────────────────────

        public void Pause()
        {
            if (_paused) return;
            _paused = true;
            if (_a != null) _a.Pause();
            if (_b != null) _b.Pause();
        }

        public void Resume()
        {
            if (!_paused) return;
            _paused = false;
            if (_a != null) _a.UnPause();
            if (_b != null) _b.UnPause();
        }

        // ── Duck (hạ nhạc nền khi mở popup) ────────────────────────────

        /// <summary>Hạ gain nhạc nền xuống <paramref name="factor"/> (0..1) trong <paramref name="duration"/> giây.</summary>
        public void Duck(float factor, float duration = 0.25f)
        {
            factor = Mathf.Clamp01(factor);
            if (_duckMotion.IsActive()) _duckMotion.Cancel();

            if (duration <= 0f)
            {
                _duck = factor;
                ApplyMix();
                return;
            }

            _duckMotion = LMotion.Create(_duck, factor, duration)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(this, static (d, self) => { self._duck = d; self.ApplyMix(); });
        }

        /// <summary>Trả nhạc nền về full gain.</summary>
        public void Unduck(float duration = 0.25f) => Duck(1f, duration);

        // ── Nội bộ ─────────────────────────────────────────────────────

        private AudioSource Other(AudioSource s) => s == _a ? _b : _a;

        private void StartFade()
        {
            if (_fadeMotion.IsActive()) _fadeMotion.Cancel();
            _fadeT = 0f;
            ApplyMix();
            _fadeMotion = LMotion.Create(0f, 1f, _fade)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithOnComplete(_onFadeDone ??= OnFadeDone)
                .Bind(this, static (t, self) => { self._fadeT = t; self.ApplyMix(); });
        }

        private void OnFadeDone()
        {
            if (_from != null) HardStop(_from);
            _from = null;
            _fadeT = 1f;
            ApplyMix();
        }

        /// <summary>Áp gain sống lên 2 nguồn theo mix hiện tại — an toàn gọi bất cứ lúc nào.</summary>
        private void ApplyMix()
        {
            float g = Gain;
            if (_from != null) _from.volume = (1f - _fadeT) * g;
            if (_to != null) _to.volume = (_playing ? _fadeT : 0f) * g;
        }

        private static void HardStop(AudioSource s)
        {
            s.Stop();
            s.clip = null;
            s.volume = 0f;
        }

        private void OnDestroy()
        {
            if (_fadeMotion.IsActive()) _fadeMotion.Cancel();
            if (_duckMotion.IsActive()) _duckMotion.Cancel();
        }
    }
}
