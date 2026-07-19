using LitMotion;
using UnityEngine;
using VahTyah;

/// <summary>
/// Reusable popup open/close animation driven by LitMotion. Open = scale-over-time curve
/// (default balloon-blast: 0.8 -> 1.1 overshoot -> 1.0) + short CanvasGroup fade; close = scale + fade out.
/// SetActive is handled internally, so callers only use Show / Hide.
///
/// All timing/curve params come from the shared PanelStyleService (ModulePanel) via _style (PopupStyle) —
/// the component holds NO local style fields. Missing service/profile → code default (balloon-blast feel).
/// Only _panel (per-instance scene ref) and _style live here.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class PopupAnimator : PanelAnimator
{
    [Tooltip("Shared popup style from ModulePanel. Missing service/profile → code default.")]
    [SerializeField] private PopupStyleId _style = PopupStyleId.Default;

    [Tooltip("Inner panel that pops in/out. Leave empty to animate alpha only.")]
    [SerializeField] private RectTransform _panel;

    // Fallback khi chưa có ModulePanel (field initializer dựng sẵn curve balloon-blast + defaults).
    private static PopupStyle _codeDefault;
    private static PopupStyle CodeDefault => _codeDefault ??= new PopupStyle();

    private CanvasGroup _cg;
    private MotionHandle _alpha;
    private MotionHandle _scale;
    private AnimationCurve _activeCurve;   // static lambda đọc field này

    private void Awake() => _cg = GetComponent<CanvasGroup>();

    private PopupStyle ResolveStyle() => PanelStyle.Popup(_style) ?? CodeDefault;

    public override void Show()
    {
        CancelMotions();
        gameObject.SetActive(true);

        var style = ResolveStyle();
        _activeCurve = (style.ScaleCurve != null && style.ScaleCurve.length > 0)
            ? style.ScaleCurve : CodeDefault.ScaleCurve;

        _cg.alpha = 0f;
        _cg.interactable = true;
        _cg.blocksRaycasts = true;
        _alpha = LMotion.Create(0f, 1f, style.FadeInDuration).Bind(_cg, static (a, cg) => cg.alpha = a);

        if (_panel != null)
        {
            float dur = CurveDuration(_activeCurve);
            _panel.localScale = Vector3.one * _activeCurve.Evaluate(0f);
            _scale = LMotion.Create(0f, dur, dur)
                .Bind(this, static (t, self) => self._panel.localScale = Vector3.one * self._activeCurve.Evaluate(t));
        }
    }

    public override void Hide()
    {
        var style = ResolveStyle();

        CancelMotions();
        _cg.interactable = false;
        _cg.blocksRaycasts = false;

        if (!style.HasCloseAnimation)
        {
            gameObject.SetActive(false);
            return;
        }

        _alpha = LMotion.Create(_cg.alpha, 0f, style.CloseDuration)
            .WithOnComplete(() => gameObject.SetActive(false))
            .Bind(_cg, static (a, cg) => cg.alpha = a);

        if (_panel != null)
            _scale = LMotion.Create(_panel.localScale.x, style.CloseScale, style.CloseDuration)
                .WithEase(style.CloseEase)
                .Bind(this, static (s, self) => self._panel.localScale = Vector3.one * s);
    }

    private static float CurveDuration(AnimationCurve c)
    {
        int n = c != null ? c.length : 0;
        return n > 0 ? Mathf.Max(0.01f, c[n - 1].time) : 0.2f;
    }

    private void CancelMotions()
    {
        if (_alpha.IsActive()) _alpha.Cancel();
        if (_scale.IsActive()) _scale.Cancel();
    }

    private void OnDestroy() => CancelMotions();
}
