using System;
using LitMotion;
using UnityEngine;

/// <summary>
/// Reusable fade-only show/hide for a CanvasGroup, driven by LitMotion. SetActive and the
/// interactable/blocksRaycasts states are handled internally, so callers only use Show / Hide.
/// A lighter sibling of <see cref="PopupView"/> for panels that need a plain fade (no scale pop).
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class FadeView : MonoBehaviour, IPanelView
{
    [SerializeField] private bool fadeIn = true;
    [SerializeField] private float _fadeInDuration = 0.2f;
    [SerializeField] private bool fadeOut = true;
    [SerializeField] private float _fadeOutDuration = 0.15f;

    private CanvasGroup _cg;
    private MotionHandle _alpha;
    private Action _deactivate;

    private CanvasGroup Cg => _cg != null ? _cg : (_cg = GetComponent<CanvasGroup>());

    public void Show()
    {
        CancelMotions();
        gameObject.SetActive(true);

        var cg = Cg;
        cg.interactable = true;
        cg.blocksRaycasts = true;

        if (fadeIn)
        {
            cg.alpha = 0f;
            _alpha = LMotion.Create(0f, 1f, _fadeInDuration).Bind(cg, static (a, c) => c.alpha = a);
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

        if (fadeOut)
        {
            _alpha = LMotion.Create(cg.alpha, 0f, _fadeOutDuration)
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
