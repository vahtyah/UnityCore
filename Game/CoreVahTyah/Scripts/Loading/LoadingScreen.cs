using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace VahTyah
{
    public class LoadingScreen : MonoBehaviour
    {
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

        [SerializeField] private float waitIntroDuration = 0.1f;
        

        [Header("Animation")]
        [Tooltip("Thời gian loading tối thiểu (giây) trước khi fade — tránh chớp màn khi boot quá nhanh. 0 = không giữ.")]
        [SerializeField] private float _minLoadingTime = 1.5f;
        [Tooltip("Hằng số smoothing cho fill đoạn cuối (_introTarget → 100%), ease-out kiểu lerp. " +
                 "Cao = tới 100% nhanh; ~6–10 cho cảm giác búng nhanh, thấp thì settle chậm.")]
        [Min(0.01f)] [SerializeField] private float _fillSpeed = 8f;
        [Tooltip("Chiều rộng khi đầy. 0 = tự đọc từ _fillRect lúc Awake.")]
        [SerializeField] private float _fullWidth = 0f;

        private const float MaxStep = 1f / 30f;

        private GameObject _root;

        private void Awake()
        {
            _root = transform.root.gameObject;
            DontDestroyOnLoad(_root);

            this.On<TransitionRequest>(OnTransitionRequest);

            if (_introCurve == null || _introCurve.length == 0)
                _introCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            if (_fullWidth <= 0f && _fillRect != null) _fullWidth = _fillRect.rect.width;
            ApplyBar(0f);

            RunAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private void OnTransitionRequest(TransitionRequest obj)
        {
            if(!obj.Cover) Destroy(_root);
        }

        private async UniTaskVoid RunAsync(CancellationToken ct)
        {
            float startTime = Time.realtimeSinceStartup;

            float elapsed = 0f;
            while (elapsed < _introDuration)
            {
                elapsed += Mathf.Min(Time.unscaledDeltaTime, MaxStep);
                ApplyBar(_introTarget * _introCurve.Evaluate(Mathf.Clamp01(elapsed / _introDuration)));
                await UniTask.Yield(ct);
            }
            ApplyBar(_introTarget);

            await UniTask.WaitForSeconds(waitIntroDuration, cancellationToken: ct);

            float remain = _minLoadingTime - (Time.realtimeSinceStartup - startTime);
            if (remain > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(remain), DelayType.Realtime, cancellationToken: ct);

            float shown = _introTarget;
            while (shown < 0.999f)
            {
                float k = 1f - Mathf.Exp(-_fillSpeed * Mathf.Min(Time.unscaledDeltaTime, MaxStep));
                shown = Mathf.Lerp(shown, 1f, k);
                ApplyBar(shown);
                await UniTask.Yield(ct);
            }
            ApplyBar(1f);

            
            EventBus.Publish(new BootIntroCompleted()).Forget();
        }

        private void ApplyBar(float t)
        {
            if (_fillRect != null)
                _fillRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _fullWidth * t);
            if (_percentText != null) _percentText.SetText("{0}%", Mathf.RoundToInt(t * 100f));
        }
    }
}
