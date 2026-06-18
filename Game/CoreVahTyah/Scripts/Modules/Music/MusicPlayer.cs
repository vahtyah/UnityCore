using System.Collections;
using UnityEngine;

namespace VahTyah
{
    /// <summary>
    /// Phát nhạc nền với crossfade (2 AudioSource). Persistent (do ModuleMusic đặt dưới holder).
    /// Fade dùng unscaledTime để chạy cả khi game pause.
    /// </summary>
    public sealed class MusicPlayer : MonoBehaviour
    {
        private AudioSource _a;
        private AudioSource _b;
        private AudioSource _current;
        private AudioClip _currentClip;

        private float _fade = 0.6f;
        private float _volume = 1f;
        private bool _active = true;
        private Coroutine _routine;

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
            _current = _a;
        }

        public void Configure(float volume, bool active)
        {
            _volume = Mathf.Clamp01(volume);
            _active = active;
            ApplyVolume();
        }

        public void Play(AudioClip clip)
        {
            if (clip == null) return;
            if (clip == _currentClip && _current.isPlaying) return;

            _currentClip = clip;

            var from = _current;
            var to = _current == _a ? _b : _a;
            to.clip = clip;
            to.volume = 0f;
            to.Play();
            _current = to;

            StartFade(from, to);
        }

        public void Stop()
        {
            _currentClip = null;
            StartFade(_current, null);
        }

        public void SetActive(bool active)
        {
            _active = active;
            ApplyVolume();
        }

        public void SetVolume(float volume)
        {
            _volume = Mathf.Clamp01(volume);
            ApplyVolume();
        }

        private void ApplyVolume()
        {
            // Áp ngay nếu không đang crossfade; nếu đang fade thì để fade tự tới target.
            if (_routine != null) return;
            if (_current != null && _current.isPlaying)
                _current.volume = _active ? _volume : 0f;
        }

        private void StartFade(AudioSource from, AudioSource to)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(FadeRoutine(from, to));
        }

        private IEnumerator FadeRoutine(AudioSource from, AudioSource to)
        {
            float target = _active ? _volume : 0f;
            float fromStart = from != null ? from.volume : 0f;
            float t = 0f;

            while (t < _fade)
            {
                t += Time.unscaledDeltaTime;
                float k = t / _fade;
                if (from != null) from.volume = Mathf.Lerp(fromStart, 0f, k);
                if (to != null) to.volume = Mathf.Lerp(0f, target, k);
                yield return null;
            }

            if (from != null)
            {
                from.volume = 0f;
                from.Stop();
                from.clip = null;
            }
            if (to != null) to.volume = target;

            _routine = null;
        }
    }
}
