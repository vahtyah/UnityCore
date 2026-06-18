using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VahTyah
{
    /// <summary>
    /// View màn hình loading. Đặt trong boot scene (scene 0), Canvas Sort Order cao.
    /// Nghe BootProgress/BootCompleted từ Bootstrap — KHÔNG ai gọi trực tiếp.
    /// Tự DontDestroyOnLoad để sống xuyên qua lần load game scene rồi fade out.
    /// </summary>
    public class LoadingScreen : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Image _fillImage;
        [SerializeField] private TMP_Text _percentText;
        [SerializeField] private TMP_Text _messageText;

        [Header("Animation")]
        [SerializeField] private float _fadeDuration = 0.5f;
        [Tooltip("Tốc độ thanh bar chạy tới target (đơn vị/giây).")]
        [SerializeField] private float _barSpeed = 2f;
        [Tooltip("Thời gian loading tối thiểu (giây) để tránh nháy nhanh.")]
        [SerializeField] private float _minLoadingTime = 1.5f;

        private float _target;
        private float _shown;
        private float _startTime;
        private bool _completed;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            _startTime = Time.realtimeSinceStartup;
            _shown = _target = 0f;
            ApplyBar(0f);
            if (_canvasGroup != null) _canvasGroup.alpha = 1f;

            this.On<BootProgress>(OnProgress);
            this.On<BootCompleted>(_ => OnCompleted());
        }

        private void Update()
        {
            if (Mathf.Approximately(_shown, _target)) return;
            _shown = Mathf.MoveTowards(_shown, _target, _barSpeed * Time.unscaledDeltaTime);
            ApplyBar(_shown);
        }

        private void OnProgress(BootProgress e)
        {
            _target = Mathf.Clamp01(e.Value);
            if (e.Message != null && _messageText != null)
                _messageText.SetText(e.Message);
        }

        private void OnCompleted()
        {
            if (_completed) return;
            _completed = true;
            StartCoroutine(FinishRoutine());
        }

        private IEnumerator FinishRoutine()
        {
            // đợi đủ thời gian tối thiểu
            float elapsed = Time.realtimeSinceStartup - _startTime;
            if (elapsed < _minLoadingTime)
                yield return new WaitForSecondsRealtime(_minLoadingTime - elapsed);

            // đợi thanh chạy tới target
            while (!Mathf.Approximately(_shown, _target))
                yield return null;

            // fade out
            if (_canvasGroup != null)
            {
                float start = _canvasGroup.alpha;
                float t = 0f;
                while (t < _fadeDuration)
                {
                    t += Time.unscaledDeltaTime;
                    _canvasGroup.alpha = Mathf.Lerp(start, 0f, t / _fadeDuration);
                    yield return null;
                }
                _canvasGroup.alpha = 0f;
            }

            Destroy(gameObject);
        }

        private void ApplyBar(float t)
        {
            if (_fillImage != null) _fillImage.fillAmount = t;
            if (_percentText != null) _percentText.SetText("{0}%", Mathf.RoundToInt(t * 100f));
        }
    }
}
