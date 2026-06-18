using System;
using System.Collections;
using UnityEngine;

namespace StandardAssets
{
    /// <summary>
    /// Phát hiệu ứng popup (scale theo curve) cho GameObject khi bật hoặc khi gọi thủ công.
    /// </summary>
    public class AnimationPopup : MonoBehaviour
    {
        [Tooltip("Automatically play the popup animation when this GameObject is enabled.")]
        [SerializeField]
        private bool playOnEnable = true;

        [Tooltip("Run the animation in real time, unaffected by Time.timeScale.")]
        [SerializeField]
        private bool ignoreTimeScale = false;

        [Tooltip("When enabled, use the curve below instead of the module's default curve.")]
        [SerializeField]
        private bool overrideCurve = false;

        [ShowIf(new object[] { "overrideCurve" })]
        [Tooltip("Custom curve (x = seconds, y = scale). Active only when Override Curve is enabled.")]
        [SerializeField]
        private AnimationCurve animationCurve = new AnimationCurve();

        private Vector3 _originalScale;
        private Coroutine _activeCoroutine;

        public bool PlayOnEnable => playOnEnable;

        private void Awake()
        {
            _originalScale = transform.localScale;
        }

        private void OnEnable()
        {
            if (playOnEnable)
            {
                PlayPopup();
            }
        }

        private void OnDisable()
        {
            StopAnimation();
        }

        private void OnDestroy()
        {
            StopAnimation();
        }

        [ContextMenu("▶ Play Popup")]
        public void PlayPopup()
        {
            PlayPopup(null);
        }

        public void PlayPopup(Action onDone, bool reverse = false)
        {
            AnimationCurve val = ResolveCurve();
            if (val == null || val.length == 0)
            {
                Debug.LogWarning("[AnimationPopup] No curve available. Assign a default in ModuleAnimationPopup or enable Override Curve. (" + name + ")", this);
                return;
            }
            StopAnimation();
            _activeCoroutine = StartCoroutine(PlayRoutine(val, reverse, onDone));
        }

        [ContextMenu("⏹ Reset Scale")]
        public void ResetScale()
        {
            StopAnimation();
            AnimationCurve val = ResolveCurve();
            if (val != null && val.length > 0)
            {
                transform.localScale = _originalScale * val.Evaluate(val.keys[val.length - 1].time);
            }
        }

        private IEnumerator PlayRoutine(AnimationCurve curve, bool reverse, Action onDone)
        {
            float duration = curve.keys[curve.length - 1].time;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += (ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime);
                float t = (reverse ? (duration - elapsed) : elapsed);
                transform.localScale = _originalScale * curve.Evaluate(t);
                yield return null;
            }
            float endT = (reverse ? 0f : duration);
            transform.localScale = _originalScale * curve.Evaluate(endT);
            _activeCoroutine = null;
            onDone?.Invoke();
        }

        private void StopAnimation()
        {
            if (_activeCoroutine != null)
            {
                StopCoroutine(_activeCoroutine);
                _activeCoroutine = null;
            }
        }

        private AnimationCurve ResolveCurve()
        {
            if (overrideCurve && animationCurve != null && animationCurve.length > 0)
            {
                return animationCurve;
            }
            return (ModuleAnimationPopup.Instance != null) ? ModuleAnimationPopup.Instance.DefaultCurve : null;
        }
    }
}
