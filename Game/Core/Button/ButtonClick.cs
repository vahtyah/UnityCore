using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace StandardAssets
{
    /// <summary>
    /// Nút bấm với hiệu ứng scale khi nhấn/thả, kèm âm thanh, haptic và particle tùy chọn.
    /// </summary>
    [DisallowMultipleComponent]
    public class ButtonClick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [Tooltip("Transform that gets scaled. Auto-assigned to the first child on Awake so the RectTransform hitbox is unaffected. Override manually if needed.")]
        [SerializeField]
        private Transform _visual;

        [Tooltip("Function will be called on hold (PointerDown) instead of click")]
        public bool callOnHold;

        public UnityEvent onClick;

        [Header("Sound")]
        public bool sound;

        [ShowIf(new object[] { "sound" })]
        [SAEnumFilter("Sound")]
        public SAEnumRef soundType;

        [Header("Particle")]
        public bool particle;

        [ShowIf(new object[] { "particle" })]
        [SAEnumFilter("Particle")]
        public SAEnumRef particleType;

        [Header("Settings")]
        public bool customSettings;

        [ShowIf(new object[] { "customSettings" })]
        [SerializeField]
        private float pressDuration = 0.1f;

        [ShowIf(new object[] { "customSettings" })]
        [SerializeField]
        private float releaseDuration = 0.15f;

        [ShowIf(new object[] { "customSettings" })]
        [SerializeField]
        private float pressedScale = 0.9f;

        [ShowIf(new object[] { "customSettings" })]
        [SerializeField]
        private AnimationCurve pressEase = ButtonCurves.OutQuad();

        [ShowIf(new object[] { "customSettings" })]
        [SerializeField]
        private AnimationCurve releaseEase = ButtonCurves.OutBack();

        [ShowIf(new object[] { "customSettings" })]
        [SerializeField]
        private float bounceScale = 1.1f;

        [ShowIf(new object[] { "customSettings" })]
        [SerializeField]
        private float bounceUpDuration = 0.1f;

        [ShowIf(new object[] { "customSettings" })]
        [SerializeField]
        private float bounceDownDuration = 0.15f;

        [ShowIf(new object[] { "customSettings" })]
        [SerializeField]
        private AnimationCurve bounceUpEase = ButtonCurves.OutQuad();

        [ShowIf(new object[] { "customSettings" })]
        [SerializeField]
        private AnimationCurve bounceDownEase = ButtonCurves.OutBack();

        private Transform _target;
        private Vector3 _originalScale;
        private Coroutine _scaleCoroutine;

        // Các thuộc tính dưới đây lấy giá trị custom của riêng nút, hoặc fallback về cấu hình module.
        private float PressDuration => customSettings ? pressDuration : (ModuleButton.Instance?.PressDuration ?? pressDuration);
        private float ReleaseDuration => customSettings ? releaseDuration : (ModuleButton.Instance?.ReleaseDuration ?? releaseDuration);
        private float PressedScale => customSettings ? pressedScale : (ModuleButton.Instance?.PressedScale ?? pressedScale);
        private AnimationCurve PressEase => customSettings ? pressEase : (ModuleButton.Instance?.PressEase ?? pressEase);
        private AnimationCurve ReleaseEase => customSettings ? releaseEase : (ModuleButton.Instance?.ReleaseEase ?? releaseEase);
        private float BounceScale => customSettings ? bounceScale : (ModuleButton.Instance?.BounceScale ?? bounceScale);
        private float BounceUpDuration => customSettings ? bounceUpDuration : (ModuleButton.Instance?.BounceUpDuration ?? bounceUpDuration);
        private float BounceDownDuration => customSettings ? bounceDownDuration : (ModuleButton.Instance?.BounceDownDuration ?? bounceDownDuration);
        private AnimationCurve BounceUpEase => customSettings ? bounceUpEase : (ModuleButton.Instance?.BounceUpEase ?? bounceUpEase);
        private AnimationCurve BounceDownEase => customSettings ? bounceDownEase : (ModuleButton.Instance?.BounceDownEase ?? bounceDownEase);

        private void Awake()
        {
            // Nếu chưa gán visual thì lấy child đầu tiên để scale, tránh ảnh hưởng hitbox.
            if (_visual == null && transform.childCount > 0)
            {
                _visual = transform.GetChild(0);
            }
            _target = (_visual != null) ? _visual : transform;
        }

        private void Start()
        {
            _originalScale = _target.localScale;
        }

        private void OnEnable()
        {
            if (_originalScale != Vector3.zero)
            {
                _target.localScale = _originalScale;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            ScaleTo(_originalScale * PressedScale, PressDuration, PressEase);
            if (callOnHold)
            {
                CallFunction();
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ScaleTo(_originalScale, ReleaseDuration, ReleaseEase);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!callOnHold)
            {
                ScaleBounce();
                CallFunction();
            }
        }

        private void ScaleTo(Vector3 target, float duration, AnimationCurve curve)
        {
            if (_scaleCoroutine != null)
            {
                StopCoroutine(_scaleCoroutine);
            }
            _scaleCoroutine = StartCoroutine(ScaleRoutine(_target.localScale, target, duration, curve));
        }

        private void ScaleBounce()
        {
            if (_scaleCoroutine != null)
            {
                StopCoroutine(_scaleCoroutine);
            }
            _scaleCoroutine = StartCoroutine(BounceRoutine());
        }

        private IEnumerator ScaleRoutine(Vector3 from, Vector3 to, float duration, AnimationCurve curve)
        {
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / Mathf.Max(duration, 0.001f))
            {
                _target.localScale = Vector3.LerpUnclamped(from, to, curve.Evaluate(t));
                yield return null;
            }
            _target.localScale = to;
        }

        private IEnumerator BounceRoutine()
        {
            Vector3 start = _target.localScale;
            Vector3 up = _originalScale * BounceScale;
            for (float t2 = 0f; t2 < 1f; t2 += Time.unscaledDeltaTime / Mathf.Max(BounceUpDuration, 0.001f))
            {
                _target.localScale = Vector3.LerpUnclamped(start, up, BounceUpEase.Evaluate(t2));
                yield return null;
            }
            _target.localScale = up;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / Mathf.Max(BounceDownDuration, 0.001f))
            {
                _target.localScale = Vector3.LerpUnclamped(up, _originalScale, BounceDownEase.Evaluate(t));
                yield return null;
            }
            _target.localScale = _originalScale;
        }

        private void CallFunction()
        {
            onClick.Invoke();

            // Gửi haptic mặc định.
            SATypedBus.Publish(new Ev.HapticPlay { Types = new HapticType[1], Force = false });

            if (sound && soundType.IsSet)
            {
                SATypedBus.Publish(new Ev.SoundPlay { Type = Convert.ToInt32(soundType.GetEnum()), Volume = 1f, Pitch = 1f });
            }

            if (particle && particleType.IsSet)
            {
                // Quy đổi vị trí chuột về tọa độ tâm màn hình.
                Vector2 val = (Vector2)Input.mousePosition - new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                SATypedBus.Publish(new Ev.ParticlePlay { Type = Convert.ToInt32(particleType.GetEnum()), Position = new Vector3(val.x, val.y, 0f), PositionIsScreenOffset = true });
            }
        }
    }
}
