using LitMotion;
using UnityEngine;

/// <summary>
/// Reusable popup open/close animation. The open "pop" is driven by a scale-over-time AnimationCurve
/// whose default matches balloon-blast's sa.animationpopup: 0.8 -> 1.1 overshoot -> 1.0 settle. A short
/// CanvasGroup fade runs alongside. SetActive is handled internally, so callers only use Show / Hide.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class PopupView : MonoBehaviour, IPanelView
{
    [Tooltip("Inner panel that pops in/out. Leave empty to animate alpha only.")]
    [SerializeField] private RectTransform _panel;

    [Tooltip("Scale over time on open. Default = balloon-blast curve (0.8 -> 1.1 overshoot -> 1.0). " +
             "Curve length (last key time) is the open duration.")]
    [SerializeField] private AnimationCurve _scaleCurve = new AnimationCurve(
        new Keyframe(0f, 0.8f),
        new Keyframe(0.125f, 1.1f),
        new Keyframe(0.2f, 1f),
        new Keyframe(0.35f, 1f));

    [SerializeField] private bool hasCloseAnimation = true;
    
    [SerializeField] private float _fadeInDuration = 0.12f;
    [SerializeField] private float _closeDuration = 0.15f;
    [SerializeField] private float _closeScale = 0.8f;

    private CanvasGroup _cg;
    private MotionHandle _alpha;
    private MotionHandle _scale;

    private void Awake() => _cg = GetComponent<CanvasGroup>();

    public void Show()
    {
        CancelMotions();
        gameObject.SetActive(true);

        _cg.alpha = 0f;
        _cg.interactable = true;
        _cg.blocksRaycasts = true;
        _alpha = LMotion.Create(0f, 1f, _fadeInDuration).Bind(_cg, static (a, cg) => cg.alpha = a);

        if (_panel != null)
        {
            float dur = ScaleDuration();
            _panel.localScale = Vector3.one * _scaleCurve.Evaluate(0f);
            _scale = LMotion.Create(0f, dur, dur)
                .Bind(this, static (t, self) => self._panel.localScale = Vector3.one * self._scaleCurve.Evaluate(t));
        }
    }

    public void Hide()
    {
        CancelMotions();
        _cg.interactable = false;
        _cg.blocksRaycasts = false;

        if (!hasCloseAnimation)
        {
            gameObject.SetActive(false);
            return;
        }

        _alpha = LMotion.Create(_cg.alpha, 0f, _closeDuration)
            .WithOnComplete(() => gameObject.SetActive(false))
            .Bind(_cg, static (a, cg) => cg.alpha = a);

        if (_panel != null)
            _scale = LMotion.Create(_panel.localScale.x, _closeScale, _closeDuration)
                .WithEase(Ease.InBack)
                .Bind(this, static (s, self) => self._panel.localScale = Vector3.one * s);
    }

    private float ScaleDuration()
    {
        int n = _scaleCurve.length;
        return n > 0 ? Mathf.Max(0.01f, _scaleCurve[n - 1].time) : 0.2f;
    }

    private void CancelMotions()
    {
        if (_alpha.IsActive()) _alpha.Cancel();
        if (_scale.IsActive()) _scale.Cancel();
    }

    private void OnDestroy() => CancelMotions();
}
