using System.Collections;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace VahTyah
{
    public class LoadingScreen : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [Tooltip("RectTransform của thanh fill (Image Sliced). Bar chạy bằng cách đổi WIDTH — anchor ngang cố định, không stretch.")]
        [SerializeField] private RectTransform _fillRect;
        [SerializeField] private TMP_Text _percentText;

        [Header("Intro (0 → Intro Target)")]
        [Tooltip("Thời gian (giây) để bar tự chạy từ 0 tới Intro Target — độc lập với tiến độ init module. " +
                 "Bootstrap chờ đoạn này xong rồi mới load scene.")]
        [Min(0.01f)] [SerializeField] private float _introDuration = 1.5f;
        [Tooltip("Mốc kết thúc intro (0..1). Bar giữ ở đây trong lúc load scene, rồi fill nốt tới 100% khi boot xong. " +
                 "Mặc định 0.85.")]
        [Range(0f, 1f)] [SerializeField] private float _introTarget = 0.85f;
        [Tooltip("Đường cong tiến độ intro. X = thời gian 0..1, Y = tiến độ 0..1. " +
                 "Đường thẳng = linear; EaseInOut = mượt hai đầu; kéo cong Y để đổi cảm giác (ease-out, overshoot...).")]
        [SerializeField] private AnimationCurve _introCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Animation")]
        [SerializeField] private float _fadeDuration = 0.5f;
        [Tooltip("Thời gian loading tối thiểu (giây) trước khi fade — tránh chớp màn khi boot quá nhanh. 0 = không giữ.")]
        [SerializeField] private float _minLoadingTime = 1.5f;
        [Tooltip("Hằng số smoothing cho fill đoạn cuối (_introTarget → 100%), ease-out kiểu lerp. " +
                 "Cao = tới 100% nhanh; ~6–10 cho cảm giác búng nhanh, thấp thì settle chậm.")]
        [Min(0.01f)] [SerializeField] private float _fillSpeed = 8f;
        [Tooltip("Chiều rộng khi đầy. 0 = tự đọc từ _fillRect lúc Awake.")]
        [SerializeField] private float _fullWidth = 0f;

        private const float MaxStep = 1f / 30f;

        private float _shown;
        private float _startTime;
        private float _introElapsed;
        private bool _completed;
        private bool _introDone;
        private GameObject _root;

        private void Awake()
        {
            _root = transform.root.gameObject;
            DontDestroyOnLoad(_root);

            if (_introCurve == null || _introCurve.length == 0)
                _introCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

            _startTime = Time.realtimeSinceStartup;
            if (_fullWidth <= 0f && _fillRect != null) _fullWidth = _fillRect.rect.width;
            _shown = 0f;
            ApplyBar(0f);
            if (_canvasGroup != null) _canvasGroup.alpha = 1f;

            this.On<BootCompleted>(_ => OnCompleted());
        }

        private void Update()
        {
            if (_introDone) return;

            _introElapsed += Mathf.Min(Time.unscaledDeltaTime, MaxStep);
            float t = Mathf.Clamp01(_introElapsed / _introDuration);
            _shown = _introTarget * _introCurve.Evaluate(t);

            if (t >= 1f)
            {
                _shown = _introTarget;
                _introDone = true;
                EventBus.Publish(new BootIntroReady()).Forget();
            }

            ApplyBar(_shown);
        }

        private void OnCompleted()
        {
            if (_completed) return;
            _completed = true;
            StartCoroutine(FinishRoutine());
        }

        private IEnumerator FinishRoutine()
        {
            _introDone = true;

            float elapsed = Time.realtimeSinceStartup - _startTime;
            if (elapsed < _minLoadingTime)
                yield return new WaitForSecondsRealtime(_minLoadingTime - elapsed);

            while (_shown < 0.999f)
            {
                float k = 1f - Mathf.Exp(-_fillSpeed * Mathf.Min(Time.unscaledDeltaTime, MaxStep));
                _shown = Mathf.Lerp(_shown, 1f, k);
                ApplyBar(_shown);
                yield return null;
            }
            _shown = 1f;
            ApplyBar(1f);

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

            Destroy(_root);
        }

        private void ApplyBar(float t)
        {
            if (_fillRect != null)
                _fillRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _fullWidth * t);
            if (_percentText != null) _percentText.SetText("{0}%", Mathf.RoundToInt(t * 100f));
        }
    }
}
