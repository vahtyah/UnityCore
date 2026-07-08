using Cysharp.Threading.Tasks;
using UnityEngine;
using VahTyah;

/// Debug panel để test các Module qua EventBus ngay trong scene Game.
/// Gắn component này vào 1 GameObject bất kỳ trong scene Game rồi Play.
/// Thêm test cho module khác: viết một Draw<Feature>Section() mới và gọi trong DrawSections().
[AddComponentMenu("VahTyah/Debug/Module Test GUI")]
public class ModuleTestGUI : MonoBehaviour
{
    [Header("Layout")]
    [Tooltip("Chiều rộng tham chiếu để scale GUI theo màn hình (px). 1080 = hợp cho phone dọc.")]
    [SerializeField] private float _referenceWidth = 1080f;
    [SerializeField] private bool _visible = true;

    private int _setLevelInput = 1;
    private bool _showScreenOnComplete = true;

    private int _current;
    private int _index;
    private int _tries;
    private bool _levelActive;
    private bool _loadingDone;

    private GUIStyle _label, _header, _button, _field;
    private Vector2 _scroll;

    private void Awake()
    {
        this.On<TransitionRequest>(OnTransitionRequest);
    }

    private void OnTransitionRequest(TransitionRequest obj)
    {
        if (!obj.Cover) _loadingDone = true;
    }

    private void Update()
    {
        if (!_loadingDone) return;

        _levelActive = EventBus.HasListeners<LevelGet>();
        if (!_levelActive) return;

        EventBus.Publish(new LevelGet { Reply = v => _current = v }).Forget();
        EventBus.Publish(new LevelGetIndex { Reply = v => _index = v }).Forget();
        EventBus.Publish(new LevelGetTries { Reply = v => _tries = v }).Forget();
    }

    private void OnGUI()
    {
        if (!_loadingDone) return;

        EnsureStyles();

        float scale = Mathf.Max(0.5f, Screen.width / _referenceWidth);
        Matrix4x4 prev = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

        float w = Screen.width / scale;

        if (!_visible)
        {
            if (GUI.Button(new Rect(10, 10, 220, 70), "▸ Test", _button)) _visible = true;
            GUI.matrix = prev;
            return;
        }

        float panelW = Mathf.Min(560f, w - 20f);
        GUILayout.BeginArea(new Rect(10, 10, panelW, Screen.height / scale - 20f), GUI.skin.box);
        _scroll = GUILayout.BeginScrollView(_scroll);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Module Test", _header);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("✕", _button, GUILayout.Width(70))) _visible = false;
        GUILayout.EndHorizontal();

        DrawSections();

        GUILayout.EndScrollView();
        GUILayout.EndArea();

        GUI.matrix = prev;
    }

    // Thêm section mới cho module khác ở đây.
    private void DrawSections()
    {
        DrawLevelSection();
    }

    private void DrawLevelSection()
    {
        GUILayout.Space(8);
        GUILayout.Label("── Level ──", _header);

        if (!_levelActive)
        {
            GUILayout.Label("ModuleLevel chưa active.\nChạy qua boot scene (Load) để module Subscribe.", _label);
            return;
        }

        GUILayout.Label($"Current: {_current}    Index: {_index}    Tries: {_tries}", _label);

        if (Button("Request Level  (LevelLoadRequest)"))
            EventBus.Publish(new LevelLoadRequest()).Forget();

        if (Button("Level Started  (LevelStarted)"))
            EventBus.Publish(new LevelStarted()).Forget();

        _showScreenOnComplete = GUILayout.Toggle(_showScreenOnComplete, " ShowScreen", _label, GUILayout.Height(70));

        if (Button("Level Completed  (LevelCompleted)"))
            EventBus.Publish(new LevelCompleted { ShowScreen = _showScreenOnComplete }).Forget();

        if (Button("Level Failed  (LevelFailed)"))
            EventBus.Publish(new LevelFailed { ShowScreen = _showScreenOnComplete }).Forget();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Set Level:", _label, GUILayout.Width(150));
        string s = GUILayout.TextField(_setLevelInput.ToString(), _field, GUILayout.Width(130));
        if (int.TryParse(s, out int parsed)) _setLevelInput = Mathf.Max(1, parsed);
        if (GUILayout.Button("Set", _button, GUILayout.Width(120)))
            EventBus.Publish(new LevelSet { Level = _setLevelInput }).Forget();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("− 1", _button)) EventBus.Publish(new LevelSet { Level = Mathf.Max(1, _current - 1) }).Forget();
        if (GUILayout.Button("+ 1", _button)) EventBus.Publish(new LevelSet { Level = _current + 1 }).Forget();
        GUILayout.EndHorizontal();
    }

    private bool Button(string text) => GUILayout.Button(text, _button, GUILayout.Height(80));

    private void EnsureStyles()
    {
        if (_label != null) return;

        _label = new GUIStyle(GUI.skin.label) { fontSize = 30, wordWrap = true };
        _header = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold };
        _button = new GUIStyle(GUI.skin.button) { fontSize = 30 };
        _field = new GUIStyle(GUI.skin.textField) { fontSize = 30, alignment = TextAnchor.MiddleCenter };
    }
}
