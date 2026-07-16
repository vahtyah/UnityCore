using System;
using LitMotion;
using UnityEngine;
using VahTyah;

/// <summary>
/// Fade-only show/hide for a CanvasGroup, driven by LitMotion. SetActive and interactable/blocksRaycasts
/// are handled internally, so callers only use Show / Hide. A lighter sibling of PopupAnimator (no scale).
///
/// Params come from the shared PanelStyleService (ModulePanel) via _style (FadeStyle) — the component holds
/// NO local style fields. Missing service/profile → code default. Only _style lives here.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class FadeAnimator : MonoBehaviour, IPanelAnimator
{
    [Tooltip("Shared fade style from ModulePanel. Missing service/profile → code default.")]
    [SerializeField] private FadeStyleId _style = FadeStyleId.Default;

    // Fallback khi chưa có ModulePanel.
    private static FadeStyle _codeDefault;
    private static FadeStyle CodeDefault => _codeDefault ??= new FadeStyle();

    private CanvasGroup _cg;
    private MotionHandle _alpha;
    private Action _deactivate;

    private CanvasGroup Cg => _cg != null ? _cg : (_cg = GetComponent<CanvasGroup>());

    private FadeStyle ResolveStyle() => PanelStyle.Fade(_style) ?? CodeDefault;

    public void Show()
    {
        CancelMotions();
        gameObject.SetActive(true);

        var cg = Cg;
        cg.interactable = true;
        cg.blocksRaycasts = true;

        var s = ResolveStyle();
        if (s.FadeIn)
        {
            cg.alpha = 0f;
            _alpha = LMotion.Create(0f, 1f, s.FadeInDuration).Bind(cg, static (a, c) => c.alpha = a);
        }
        else
        {
            cg.alpha = 1f;
        }
    }

    public void Hide()
    {
        if (!gameObject.activeSelf) return;

        CancelMotions();
        var cg = Cg;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        var s = ResolveStyle();
        if (s.FadeOut)
        {
            _alpha = LMotion.Create(cg.alpha, 0f, s.FadeOutDuration)
                .WithOnComplete(_deactivate ??= Deactivate)
                .Bind(cg, static (a, c) => c.alpha = a);
        }
        else
        {
            Deactivate();
        }
    }

    private void Deactivate() => gameObject.SetActive(false);

    private void CancelMotions()
    {
        if (_alpha.IsActive()) _alpha.Cancel();
    }

    private void OnDestroy() => CancelMotions();
}
