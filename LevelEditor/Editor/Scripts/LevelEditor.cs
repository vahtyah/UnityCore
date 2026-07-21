using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using VahTyah.LevelEditor;

public class LevelEditor : LevelEditorBase
{
    private PanelNavigator _panelNavigator;

    protected override void OnEnable()
    {
        base.OnEnable();
        var panels = new Dictionary<string, IEditorPanel>()
        {
            { "Default", new DefaultPanel() },
        };
        _panelNavigator = new PanelNavigator(panels);
    }

    private void OnBecameVisible()
    {
        _panelNavigator?.OnEnable();
    }

    private void OnBecameInvisible()
    {
        _panelNavigator?.Cleanup();
    }

    public override void OpenLevel(Object levelObject, int index)
    {
        if (levelObject == null)
        {
            Debug.LogError("Level object is null. Cannot open level.");
            return;
        }

        var levelData = levelObject as LevelData;
        if (levelData == null)
        {
#if UNITY_EDITOR
            Debug.LogError("Invalid level object");
#endif
        }

        SceneEditorController.Instance.LoadLevel(levelData, index);
    }

    protected override void DrawContent()
    {
        const float buttonBarHeight = 36f;
        const float buttonBarPadding = 6f;

        float xStart = ResizableSidebar.TotalSize + 12f;
        float areaWidth = position.width - ResizableSidebar.CurrentWidth - 22f;

        // Reserve room at the bottom for the Apply / Discard bar
        var panelArea = new Rect(xStart, 6f, areaWidth,
            position.height - buttonBarHeight - buttonBarPadding * 2f);
        _panelNavigator.Draw(panelArea);

        // ── Apply / Discard bar ───────────────────────────────────────────────
        float barY = position.height - buttonBarHeight - buttonBarPadding;
        float btnWidth = (areaWidth - buttonBarPadding) / 2f;

        // Apply button (green/teal)
        Color savedBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.25f, 0.65f, 0.25f);
        if (GUI.Button(new Rect(xStart, barY, btnWidth, buttonBarHeight), "✔  Apply"))
            OnApply();

        // Discard button (red)
        GUI.backgroundColor = new Color(0.75f, 0.22f, 0.22f);
        if (GUI.Button(new Rect(xStart + btnWidth + buttonBarPadding, barY, btnWidth, buttonBarHeight), "✖  Discard"))
            OnDiscard();

        GUI.backgroundColor = savedBg;
    }

    private static void OnApply() => SceneEditorControllerBase.Instance.ApplyLevel();

    private static void OnDiscard() => SceneEditorControllerBase.Instance.DiscardLevel();

    protected override void OnDisable()
    {
        base.OnDisable();
        _panelNavigator.Cleanup();
    }
}