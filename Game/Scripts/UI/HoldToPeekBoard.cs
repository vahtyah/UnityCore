using LitMotion;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class HoldToPeekBoard : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Tooltip("Nhóm sẽ mờ đi khi giữ. Để trống = tự tìm CanvasGroup ở parent.")]
    [SerializeField] private CanvasGroup _target;

    [Tooltip("Alpha của popup khi đang giữ (0 = ẩn hẳn, board hiện rõ).")]
    [Range(0f, 1f)]
    [SerializeField] private float _peekAlpha = 0f;

    [Tooltip("Thời gian fade popup mờ đi khi bắt đầu giữ.")]
    [SerializeField] private float _fadeOutDuration = 0.15f;

    [Tooltip("Thời gian fade popup hiện lại khi thả.")]
    [SerializeField] private float _fadeInDuration = 0.12f;

    private MotionHandle _fade;
    private bool _peeking;

    private void Awake()
    {
        if (_target == null) _target = GetComponentInParent<CanvasGroup>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_target == null) return;

        // Chỉ peek khi bấm trúng thẳng background này, không phải event bubble từ element con.
        if (eventData.pointerCurrentRaycast.gameObject != gameObject) return;

        _peeking = true;
        CancelFade();
        _fade = LMotion.Create(_target.alpha, _peekAlpha, _fadeOutDuration)
            .Bind(_target, static (a, cg) => cg.alpha = a);
    }

    public void OnPointerUp(PointerEventData eventData) => Restore();

    private void Restore()
    {
        if (_target == null || !_peeking) return;

        _peeking = false;
        CancelFade();
        _fade = LMotion.Create(_target.alpha, 1f, _fadeInDuration)
            .Bind(_target, static (a, cg) => cg.alpha = a);
    }

    private void CancelFade()
    {
        if (_fade.IsActive()) _fade.Cancel();
    }

    private void OnDisable()
    {
        CancelFade();
        if (_peeking && _target != null) _target.alpha = 1f;
        _peeking = false;
    }

    private void OnDestroy() => CancelFade();
}
