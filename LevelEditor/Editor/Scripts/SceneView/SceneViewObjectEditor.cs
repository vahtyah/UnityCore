#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// ═══════════════════════════════════════════════════════════════════════════════
//  HƯỚNG DẪN SỬ DỤNG — SceneViewObjectEditor<TEditor, TData>
// ═══════════════════════════════════════════════════════════════════════════════
//
//  Base vẽ floating inspector (kéo được) trong SceneView cho object đang chọn.
//  Lo sẵn: selection tracking, layout/scroll/drag panel, click-blocker chống
//  deselect, Undo, persist vào SceneEditorController.WorkingLevelData.
//
//  TEditor = component trên scene đại diện object (thường kế thừa ObjectEditor<T>).
//  TData   = data authoring (serializable) mà panel sẽ chỉnh.
//
//  ─── BƯỚC 1. Tạo subclass, implement 5 hàm abstract ─────────────────────────────
//
//    public sealed class SceneViewMyEditor : SceneViewObjectEditor<MyEditor, MyData>
//    {
//        // Tiêu đề trên title-bar của panel.
//        protected override string GetPanelTitle(MyEditor e) => $"My #{e.Index}";
//
//        // Tra data tương ứng với editor đang chọn. Tự quyết tra bằng gì
//        // (Index, Floor+Position, id...). Trả null nếu không tìm thấy.
//        protected override MyData FindData(MyEditor e)
//        {
//            var level = SceneEditorController.WorkingLevelData;
//            return level?.levelDetail?.Items?.Find(i => i.Index == e.Index);
//        }
//
//        // Vẽ các field. Trả về y cuối cùng. Dùng GetRowRects + RowH/LabelHeight
//        // để canh dòng. Mỗi khi field đổi: RecordUndo(...) rồi PersistAndReload().
//        protected override float DrawFields(float x, float y, float innerW)
//        {
//            if (ActiveData == null) return y;
//            var (lRect, fRect) = GetRowRects(x, y, innerW);
//            GUI.Label(lRect, "Value", EditorStyles.whiteLabel);
//            int v = EditorGUI.IntField(fRect, ActiveData.Value);
//            if (v != ActiveData.Value)
//            {
//                RecordUndo("Change Value");
//                ActiveData.Value = v;
//                PersistAndReload();
//            }
//            return y + RowH + 4f;
//        }
//
//        // Tổng chiều cao content (phải khớp DrawFields) để panel tự co / bật scroll.
//        protected override float ContentHeight() => PadInner + RowH + 4f + PadInner;
//
//        // Lưu WorkingLevelData xuống asset rồi refresh biểu diễn trên scene.
//        protected override void PersistAndReload()
//        {
//            PersistOnly();
//            ActiveEditor?.GetComponent<MyView>()?.Refresh(ActiveData);
//        }
//    }
//
//  ─── BƯỚC 2. Bật/tắt panel (thường trong PanelNavigator / SceneEditor window) ────
//
//        private readonly SceneViewMyEditor _panel = new();
//        void OnEnable()  => _panel.Enable();     // idempotent, gọi nhiều lần OK
//        void OnDisable() => _panel.Disable();
//        // Sau khi reload level: _panel.RefreshActiveSelection();
//
//  ─── TÙY CHỌN — override thêm khi cần ───────────────────────────────────────────
//
//    • BorderColor                → đổi màu viền phân biệt loại panel.
//    • OnSelected / OnDeselected  → build/clear state theo data (vd ReorderableList).
//    • OnEnabled / OnDisabled     → acquire/release tài nguyên dùng chung (toolbar…).
//    • OnAlwaysDraw(sceneView)    → vẽ overlay mọi frame kể cả khi không chọn gì;
//                                   gọi DrawIndexLabel(...) cho từng object để hiện badge:
//
//        protected override void OnAlwaysDraw(SceneView sv)
//        {
//            Handles.BeginGUI();
//            foreach (var e in Object.FindObjectsByType<MyEditor>(FindObjectsSortMode.None))
//                DrawIndexLabel(sv, e.transform.position, e.Index.ToString(),
//                               selected: ActiveEditor == e);
//            Handles.EndGUI();
//        }
//
//    • BeginSuppressClear/EndSuppressClear → bọc quanh đoạn reselect programmatic
//      để việc bỏ chọn tạm thời không làm panel biến mất.
//
//  ─── LƯU Ý PERSIST ──────────────────────────────────────────────────────────────
//    RecordUndo + PersistOnly gắn cứng vào SceneEditorController.WorkingLevelData.
//    Mọi thay đổi data: RecordUndo("...") TRƯỚC khi gán, PersistAndReload() SAU.
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Base for a draggable floating inspector drawn inside the SceneView for a selected
/// scene object. Handles selection tracking, panel layout/scroll/drag, click-blocking,
/// Undo, and persistence against <see cref="SceneEditorController.WorkingLevelData"/>.
///
/// TEditor = the on-scene component representing the object (e.g. an <see cref="ObjectEditor{T}"/>).
/// TData   = the serialized authoring data the panel edits.
///
/// Subclass must implement <see cref="FindData"/>, <see cref="DrawFields"/>,
/// <see cref="ContentHeight"/>, <see cref="PersistAndReload"/> and <see cref="GetPanelTitle"/>.
/// See the usage guide comment block above the class for a worked example.
/// </summary>
public abstract class SceneViewObjectEditor<TEditor, TData>
    where TEditor : Component
    where TData   : class
{
    protected const float PanelWidth  = 280f;
    protected const float TitleBarH   = 20f;
    protected const float PadOuter    = 10f;
    protected const float PadInner    = 10f;
    protected const float LabelHeight = 18f;
    protected const float RowH        = 22f;

    protected TEditor ActiveEditor { get; private set; }
    protected TData   ActiveData   { get; private set; }

    private Vector2 _scrollPos;
    private Vector2 _panelPos      = new Vector2(-1f, -1f);
    private bool    _isDragging;
    private Vector2 _dragOffset;
    private int     _dragControlID = -1;
    private bool    _suppressClear;
    private bool    _enabled;

    // Idempotent: a PanelNavigator may enable every panel at init AND on becoming visible, so Enable()
    // can be called more than once. Without this guard the SceneView callback would subscribe twice
    // and Disable()'s single -= would leave a stale subscription drawing after the window is hidden.
    public void Enable()
    {
        if (_enabled) return;
        _enabled = true;
        Selection.selectionChanged += OnSelectionChanged;
        SceneView.duringSceneGui   += OnSceneGUI;
        OnEnabled();
    }

    public void Disable()
    {
        if (!_enabled) return;
        _enabled = false;
        Selection.selectionChanged -= OnSelectionChanged;
        SceneView.duringSceneGui   -= OnSceneGUI;
        ClearState();
        OnDisabled();
    }

    /// <summary>Called once when the editor transitions to enabled (after subscribing).</summary>
    protected virtual void OnEnabled() { }

    /// <summary>Called once when the editor transitions to disabled (after unsubscribing).</summary>
    protected virtual void OnDisabled() { }

    /// <summary>Re-resolve <see cref="ActiveData"/> for the current selection (e.g. after a reload).</summary>
    public void RefreshActiveSelection()
    {
        if (ActiveEditor == null)
        {
            ClearState();
            SceneView.RepaintAll();
            return;
        }

        ActiveData = FindData(ActiveEditor);
        OnSelected(ActiveEditor, ActiveData);
        SceneView.RepaintAll();
    }

    private void OnSelectionChanged()
    {
        var go = Selection.activeGameObject;
        TEditor editor = go != null ? GetRootEditor(go) : null;

        if (editor != null)
        {
            if (ActiveEditor == editor) return;

            SetActive(editor);

            // Snap selection up to the editor root so nested colliders don't fragment the selection.
            var rootGO = editor.gameObject;
            if (Selection.activeGameObject != rootGO)
                EditorApplication.delayCall += () =>
                {
                    if (rootGO != null) Selection.activeGameObject = rootGO;
                };
        }
        else
        {
            if (ActiveEditor == null || _suppressClear) return;
            ClearState();
            SceneView.RepaintAll();
        }
    }

    private static TEditor GetRootEditor(GameObject go)
    {
        // Walk up to the outermost TEditor so a click on any child resolves to the object root.
        TEditor found = null;
        Transform t = go.transform;
        while (t != null)
        {
            if (t.TryGetComponent<TEditor>(out var candidate))
                found = candidate;
            t = t.parent;
        }
        return found;
    }

    private void SetActive(TEditor editor)
    {
        ActiveEditor = editor;
        ActiveData   = FindData(editor);
        _scrollPos   = Vector2.zero;
        _isDragging  = false;
        OnSelected(ActiveEditor, ActiveData);
        SceneView.RepaintAll();
    }

    private void ClearState()
    {
        OnDeselected(ActiveEditor, ActiveData);
        ActiveEditor   = null;
        ActiveData     = null;
        _isDragging    = false;
        _dragControlID = -1;
    }

    /// <summary>Suppress the deselect-clears-panel behaviour while programmatically reselecting.</summary>
    protected void BeginSuppressClear() => _suppressClear = true;
    protected void EndSuppressClear()   => _suppressClear = false;

    protected virtual void OnSelected(TEditor editor, TData data) { }
    protected virtual void OnDeselected(TEditor editor, TData data) { }

    protected abstract float  DrawFields(float x, float y, float innerW);
    protected abstract float  ContentHeight();
    protected abstract TData  FindData(TEditor editor);
    protected abstract void   PersistAndReload();
    protected abstract string GetPanelTitle(TEditor editor);
    protected virtual  Color  BorderColor => new Color(0.4f, 0.8f, 0.4f, 0.8f);

    protected void RecordUndo(string actionName)
    {
        var levelData = SceneEditorController.WorkingLevelData;
        if (levelData != null) Undo.RecordObject(levelData, actionName);
    }

    protected void PersistOnly()
    {
        var levelData = SceneEditorController.WorkingLevelData;
        if (levelData == null) return;
        EditorUtility.SetDirty(levelData);
        AssetDatabase.SaveAssetIfDirty(levelData);
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        OnAlwaysDraw(sceneView);

        if (ActiveEditor == null || ActiveData == null) return;

        DrawPanel(sceneView);
        HandleDrag(sceneView);
        RepaintOnInteraction();
    }

    /// <summary>
    /// Called every SceneView repaint regardless of whether an editor is selected.
    /// Override to draw persistent overlays (e.g. index labels via <see cref="DrawIndexLabel"/>).
    /// </summary>
    protected virtual void OnAlwaysDraw(SceneView sceneView) { }

    private void DrawPanel(SceneView sceneView)
    {
        float svH    = sceneView.position.height;
        float contH  = ContentHeight();
        float bodyH  = Mathf.Min(contH + PadInner * 2, svH * 0.6f);
        bool  scroll = contH + PadInner * 2 > bodyH;

        Rect full     = GetPanelRect(sceneView, bodyH);
        Rect titleBar = new Rect(full.x, full.y, full.width, TitleBarH);
        Rect body     = new Rect(full.x, full.y + TitleBarH, full.width, bodyH);

        Handles.BeginGUI();

        Color prev = GUI.color;
        GUI.color  = Color.white;

        EditorGUI.DrawRect(titleBar, new Color(0.12f, 0.12f, 0.12f, 0.97f));
        DrawBorder(new Rect(full.x, full.y, full.width, TitleBarH + bodyH), 1f, BorderColor);
        GUI.Label(new Rect(titleBar.x + 4f,  titleBar.y + 1f, 16f,                  TitleBarH), "≡", EditorStyles.whiteMiniLabel);
        GUI.Label(new Rect(titleBar.x + 22f, titleBar.y + 1f, titleBar.width - 26f, TitleBarH), GetPanelTitle(ActiveEditor), EditorStyles.whiteBoldLabel);
        if (titleBar.Contains(Event.current.mousePosition))
            EditorGUIUtility.AddCursorRect(titleBar, MouseCursor.MoveArrow);

        EditorGUI.DrawRect(body, new Color(0.18f, 0.18f, 0.18f, 0.92f));

        float innerW = PanelWidth - PadInner * 2;
        float y      = body.y + PadInner;

        if (scroll)
        {
            Rect viewRect    = new Rect(body.x + PadInner, y, innerW, bodyH - PadInner * 2);
            Rect contentRect = new Rect(0, 0, innerW - 16f, contH);
            _scrollPos = GUI.BeginScrollView(viewRect, _scrollPos, contentRect);
            DrawFields(0f, 0f, innerW - 16f);
            GUI.EndScrollView();
        }
        else
        {
            DrawFields(body.x + PadInner, y, innerW);
        }

        // Blocker: consume unhandled clicks on the panel so scene-picking doesn't
        // deselect the active object. Placed last so real controls get priority.
        // LMB-down on title bar is exempted so HandleDrag can start a drag.
        var evType    = Event.current.type;
        Rect fullRect = new Rect(full.x, full.y, full.width, TitleBarH + bodyH);
        if ((evType == EventType.MouseDown || evType == EventType.MouseUp ||
             evType == EventType.ScrollWheel) && fullRect.Contains(Event.current.mousePosition))
        {
            bool isTitleBarLmb = evType == EventType.MouseDown
                                 && Event.current.button == 0
                                 && titleBar.Contains(Event.current.mousePosition);
            if (!isTitleBarLmb) Event.current.Use();
        }

        GUI.color = prev;
        Handles.EndGUI();
    }

    private Rect GetPanelRect(SceneView sv, float bodyH)
    {
        if (_panelPos.x < 0f)
            _panelPos = new Vector2(sv.position.width - PanelWidth - PadOuter, PadOuter);

        _panelPos = new Vector2(
            Mathf.Clamp(_panelPos.x, 0f, sv.position.width  - PanelWidth),
            Mathf.Clamp(_panelPos.y, 0f, sv.position.height - TitleBarH));

        return new Rect(_panelPos.x, _panelPos.y, PanelWidth, TitleBarH + bodyH);
    }

    private const int DragHint = 0x5CE3_A170;

    private void HandleDrag(SceneView sv)
    {
        Event e = Event.current;
        _dragControlID = GUIUtility.GetControlID(DragHint, FocusType.Passive);

        float bodyH    = Mathf.Min(ContentHeight() + PadInner * 2, sv.position.height * 0.6f);
        Rect  panel    = GetPanelRect(sv, bodyH);
        Rect  titleBar = new Rect(panel.x, panel.y, panel.width, TitleBarH);

        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0 && titleBar.Contains(e.mousePosition))
                {
                    _isDragging           = true;
                    _dragOffset           = e.mousePosition - new Vector2(panel.x, panel.y);
                    GUIUtility.hotControl = _dragControlID;
                    e.Use();
                }
                break;

            case EventType.MouseDrag:
                if (_isDragging && GUIUtility.hotControl == _dragControlID)
                {
                    _panelPos = e.mousePosition - _dragOffset;
                    SceneView.RepaintAll();
                    e.Use();
                }
                break;

            case EventType.MouseUp:
                if (_isDragging && e.button == 0)
                {
                    _isDragging = false;
                    if (GUIUtility.hotControl == _dragControlID)
                        GUIUtility.hotControl = 0;
                    e.Use();
                }
                break;
        }
    }

    // ── Layout helpers ────────────────────────────────────────────────────────

    protected static (Rect labelRect, Rect fieldRect) GetRowRects(
        float x, float y, float innerW, float labelWidth = 90f)
    {
        return (new Rect(x,              y, labelWidth,          RowH),
                new Rect(x + labelWidth, y, innerW - labelWidth, RowH));
    }

    private static void DrawBorder(Rect r, float t, Color c)
    {
        EditorGUI.DrawRect(new Rect(r.x - t, r.y - t, r.width + t * 2, t), c);
        EditorGUI.DrawRect(new Rect(r.x - t, r.yMax,  r.width + t * 2, t), c);
        EditorGUI.DrawRect(new Rect(r.x - t, r.y,     t, r.height),        c);
        EditorGUI.DrawRect(new Rect(r.xMax,  r.y,     t, r.height),        c);
    }

    private static void RepaintOnInteraction()
    {
        var t = Event.current.type;
        if (t == EventType.MouseMove  || t == EventType.MouseDrag  ||
            t == EventType.MouseDown  || t == EventType.MouseUp    ||
            t == EventType.KeyDown    || t == EventType.KeyUp      ||
            t == EventType.ScrollWheel)
            SceneView.RepaintAll();
    }

    // ── Index-label overlay helper ────────────────────────────────────────────
    // Reusable from OnAlwaysDraw to tag scene objects with their index/id. Call
    // inside a Handles.BeginGUI()/EndGUI() block, once per object.

    private static GUIStyle _indexLabelStyle;

    private static GUIStyle IndexLabelStyle
    {
        get
        {
            if (_indexLabelStyle != null) return _indexLabelStyle;
            _indexLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize  = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = Color.white },
                padding   = new RectOffset(4, 4, 2, 2),
            };
            _indexLabelStyle.normal.background = MakeSolidTexture(new Color(0f, 0f, 0f, 0.65f));
            return _indexLabelStyle;
        }
    }

    private static Texture2D MakeSolidTexture(Color c)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, c);
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// Draw a small badge with <paramref name="text"/> at the object's world position.
    /// Selected badges are tinted. Skips objects behind the camera. Must be called
    /// between Handles.BeginGUI() and Handles.EndGUI().
    /// </summary>
    protected static void DrawIndexLabel(SceneView sceneView, Vector3 world, string text, bool selected)
    {
        Vector3 screenPos = sceneView.camera.WorldToScreenPoint(world);
        if (screenPos.z < 0f) return; // behind camera

        float guiX = screenPos.x;
        float guiY = sceneView.camera.pixelHeight - screenPos.y; // GUI y=0 is top

        const float w = 36f, h = 22f;
        var labelRect = new Rect(guiX - w * 0.5f, guiY - h * 0.5f, w, h);

        Color prevColor = GUI.color;
        GUI.color = selected ? new Color(1f, 0.85f, 0.2f) : Color.white;
        GUI.Label(labelRect, text, IndexLabelStyle);
        GUI.color = prevColor;
    }
}
#endif
