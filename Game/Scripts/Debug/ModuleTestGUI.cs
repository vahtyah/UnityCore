using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VahTyah;

/// Debug panel để test các Module. Mỗi module là 1 tab riêng; thanh chuyển tab nằm ở đáy màn hình.
/// Gắn component vào 1 GameObject trong scene Game rồi Play.
/// Thêm module mới: thêm tên vào _tabs và 1 nhánh trong DrawActiveTab() + hàm Draw<Feature>().
[AddComponentMenu("VahTyah/Debug/Module Test GUI")]
public class ModuleTestGUI : MonoBehaviour
{
    [Header("Layout")]
    [Tooltip("Chiều rộng tham chiếu để scale GUI theo màn hình (px). 1080 = hợp cho phone dọc.")]
    [SerializeField] private float _referenceWidth = 1080f;
    [SerializeField] private bool _visible = true;

    private readonly string[] _tabs = { "Level", "Haptic", "Heart", "Item", "Sound", "Music" };
    private int _tab;

    // Level
    private int _setLevelInput = 1;
    private bool _showScreenOnComplete = true;
    private int _current, _index, _tries;
    private bool _levelActive;
    private bool _loadingDone;

    // Item
    private string _itemKey = "coin";

    // Haptic
    private bool _hapticForce;
    private bool _hapticSeeded;
    private int _lightMs = 35, _lightAmp = 200;
    private int _medMs = 45, _medAmp = 230;
    private int _heavyMs = 60, _heavyAmp = 255;

    // Style
    private bool _stylesReady;
    private GUIStyle _label, _sub, _title, _button, _btnOn, _tabOn, _tabOff, _field, _panel, _bar;
    private Vector2 _scroll;

    private const float Margin = 14f;
    private const float BarHeight = 150f;

    private void Awake() => this.On<TransitionRequest>(e => { if (!e.Cover) _loadingDone = true; });

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
        float h = Screen.height / scale;

        if (!_visible)
        {
            if (GUI.Button(new Rect(Margin, h - 110f, 210f, 92f), "▸ Test", _tabOn)) _visible = true;
            GUI.matrix = prev;
            return;
        }

        var barRect = new Rect(Margin, h - BarHeight - Margin, w - Margin * 2f, BarHeight);
        var contentRect = new Rect(Margin, Margin, w - Margin * 2f, barRect.y - Margin * 2f);

        DrawContent(contentRect);
        DrawTabBar(barRect);

        GUI.matrix = prev;
    }

    private void DrawContent(Rect rect)
    {
        GUI.Box(rect, GUIContent.none, _panel);
        var inner = new Rect(rect.x + 22f, rect.y + 20f, rect.width - 44f, rect.height - 40f);
        GUILayout.BeginArea(inner);

        GUILayout.BeginHorizontal();
        GUILayout.Label(_tabs[_tab], _title);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("✕", _button, GUILayout.Width(84f), GUILayout.Height(72f))) _visible = false;
        GUILayout.EndHorizontal();

        GUILayout.Space(8f);
        _scroll = GUILayout.BeginScrollView(_scroll);
        DrawActiveTab();
        GUILayout.EndScrollView();

        GUILayout.EndArea();
    }

    private void DrawTabBar(Rect rect)
    {
        GUI.Box(rect, GUIContent.none, _bar);
        var inner = new Rect(rect.x + 16f, rect.y + 20f, rect.width - 32f, rect.height - 40f);
        GUILayout.BeginArea(inner);
        GUILayout.BeginHorizontal();
        for (int i = 0; i < _tabs.Length; i++)
        {
            if (GUILayout.Button(_tabs[i], i == _tab ? _tabOn : _tabOff, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
                _tab = i;
        }
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    private void DrawActiveTab()
    {
        switch (_tab)
        {
            case 0: DrawLevel(); break;
            case 1: DrawHaptic(); break;
            case 2: DrawHeart(); break;
            case 3: DrawItem(); break;
            case 4: DrawSound(); break;
            case 5: DrawMusic(); break;
        }
    }

    // ── Level ─────────────────────────────────────────────
    private void DrawLevel()
    {
        if (!_levelActive)
        {
            GUILayout.Label("ModuleLevel chưa active.\nChạy qua boot scene (Load) để module Subscribe.", _label);
            return;
        }

        GUILayout.Label($"Current: {_current}     Index: {_index}     Tries: {_tries}", _label);
        GUILayout.Space(8f);

        if (Btn("Request Level")) EventBus.Publish(new LevelLoadRequest()).Forget();
        if (Btn("Level Started")) EventBus.Publish(new LevelStarted()).Forget();

        Toggle("ShowScreen", ref _showScreenOnComplete);
        if (Btn("Level Completed")) EventBus.Publish(new LevelCompleted { ShowScreen = _showScreenOnComplete }).Forget();
        if (Btn("Level Failed")) EventBus.Publish(new LevelFailed { ShowScreen = _showScreenOnComplete }).Forget();

        GUILayout.Space(8f);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Set Level", _label, GUILayout.Width(200f));
        _setLevelInput = IntField(_setLevelInput, 1, 9999);
        if (GUILayout.Button("Set", _button, GUILayout.Width(150f), GUILayout.Height(72f)))
            EventBus.Publish(new LevelSet { Level = _setLevelInput }).Forget();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("− 1", _button, GUILayout.Height(84f))) EventBus.Publish(new LevelSet { Level = Mathf.Max(1, _current - 1) }).Forget();
        if (GUILayout.Button("+ 1", _button, GUILayout.Height(84f))) EventBus.Publish(new LevelSet { Level = _current + 1 }).Forget();
        GUILayout.EndHorizontal();
    }

    // ── Haptic ────────────────────────────────────────────
    private void DrawHaptic()
    {
        if (!Services.TryGet<HapticService>(out var haptic))
        {
            GUILayout.Label("HapticService chưa register.\nChạy qua boot scene để ModuleHaptic init.", _label);
            return;
        }

#if UNITY_EDITOR
        GUILayout.Label("Editor = no-op. Build lên device Android mới rung + tune có tác dụng.", _sub);
#endif

        if (Services.TryGet<SettingsService>(out var settings))
        {
            if (GUILayout.Button(settings.Haptics ? "Haptic: ON" : "Haptic: OFF",
                    settings.Haptics ? _btnOn : _button, GUILayout.Height(84f)))
                settings.SetHaptics(!settings.Haptics);
        }
        else
        {
            GUILayout.Label("SettingsService chưa register → coi như luôn bật.", _sub);
        }

        Toggle("Force (bỏ qua cooldown)", ref _hapticForce);

        if (!_hapticSeeded)
        {
            if (haptic.TryGetOneShot(HapticType.Light, out var l)) { _lightMs = l.DurationMs; _lightAmp = l.Amplitude; }
            if (haptic.TryGetOneShot(HapticType.Medium, out var m)) { _medMs = m.DurationMs; _medAmp = m.Amplitude; }
            if (haptic.TryGetOneShot(HapticType.Heavy, out var h)) { _heavyMs = h.DurationMs; _heavyAmp = h.Amplitude; }
            _hapticSeeded = true;
        }

        GUILayout.Space(6f);
        GUILayout.Label("Tune  [Duration ms]  [Amplitude 1-255]  → Play", _sub);
        HapticRow(haptic, "Light", HapticType.Light, ref _lightMs, ref _lightAmp);
        HapticRow(haptic, "Medium", HapticType.Medium, ref _medMs, ref _medAmp);
        HapticRow(haptic, "Heavy", HapticType.Heavy, ref _heavyMs, ref _heavyAmp);

        GUILayout.Space(6f);
        GUILayout.Label("Pattern (waveform cố định)", _sub);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Success", _button, GUILayout.Height(84f))) Haptic.Play(HapticType.Success, _hapticForce);
        if (GUILayout.Button("Warning", _button, GUILayout.Height(84f))) Haptic.Play(HapticType.Warning, _hapticForce);
        if (GUILayout.Button("Failure", _button, GUILayout.Height(84f))) Haptic.Play(HapticType.Failure, _hapticForce);
        GUILayout.EndHorizontal();

        if (Btn("Sequence  (Light → Medium → Heavy)"))
            Haptic.PlaySequence(_hapticForce, HapticType.Light, HapticType.Medium, HapticType.Heavy).Forget();
    }

    private void HapticRow(HapticService haptic, string label, HapticType type, ref int ms, ref int amp)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, _label, GUILayout.Width(140f));
        ms = IntField(ms, 1, 5000);
        amp = IntField(amp, 1, 255);
        if (GUILayout.Button("Play", _btnOn, GUILayout.Width(150f), GUILayout.Height(72f)))
        {
            haptic.SetOneShot(type, new HapticOneShot { DurationMs = ms, Amplitude = amp });
            Haptic.Play(type, _hapticForce);
        }
        GUILayout.EndHorizontal();
    }

    // ── Heart ─────────────────────────────────────────────
    private void DrawHeart()
    {
        if (!EventBus.HasListeners<HeartGet>())
        {
            GUILayout.Label("ModuleHeart chưa active.\nChạy qua boot scene để module Subscribe.", _label);
            return;
        }

        int hearts = 0;
        bool full = false, inf = false;
        string timer = "", infTimer = "";
        EventBus.Publish(new HeartGet { Reply = v => hearts = v }).Forget();
        EventBus.Publish(new HeartIsFull { Reply = v => full = v }).Forget();
        EventBus.Publish(new HeartIsInfinity { Reply = v => inf = v }).Forget();
        EventBus.Publish(new HeartGetTimer { Reply = v => timer = v }).Forget();
        EventBus.Publish(new HeartGetInfinityTimer { Reply = v => infTimer = v }).Forget();

        GUILayout.Label($"Hearts: {hearts}{(full ? "   (full)" : "")}{(inf ? "   ∞" : "")}", _label);
        GUILayout.Label(inf ? $"Infinity còn: {infTimer}" : $"Hồi tim: {timer}", _sub);
        GUILayout.Space(8f);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("+1", _button, GUILayout.Height(84f))) EventBus.Publish(new HeartAdd { Value = 1 }).Forget();
        if (GUILayout.Button("−1  (Use)", _button, GUILayout.Height(84f))) EventBus.Publish(new HeartUse { Value = 1, Reply = _ => { } }).Forget();
        GUILayout.EndHorizontal();

        if (Btn("+5")) EventBus.Publish(new HeartAdd { Value = 5 }).Forget();
        if (Btn("Infinity  +30 phút")) EventBus.Publish(new HeartAddInfinity { Minutes = 30f }).Forget();
    }

    // ── Item ──────────────────────────────────────────────
    private void DrawItem()
    {
        if (!EventBus.HasListeners<ItemGet>())
        {
            GUILayout.Label("ModuleItem chưa active.\nChạy qua boot scene để module Subscribe.", _label);
            return;
        }

        int amount = 0;
        EventBus.Publish(new ItemGet { Key = _itemKey, Reply = v => amount = v }).Forget();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Key", _label, GUILayout.Width(120f));
        _itemKey = GUILayout.TextField(_itemKey, _field, GUILayout.Height(72f));
        GUILayout.EndHorizontal();

        GUILayout.Label($"Amount: {amount}", _label);
        GUILayout.Space(8f);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("+10", _button, GUILayout.Height(84f))) EventBus.Publish(new ItemAdd { Key = _itemKey, Value = 10 }).Forget();
        if (GUILayout.Button("+100", _button, GUILayout.Height(84f))) EventBus.Publish(new ItemAdd { Key = _itemKey, Value = 100 }).Forget();
        if (GUILayout.Button("−10", _button, GUILayout.Height(84f))) EventBus.Publish(new ItemAdd { Key = _itemKey, Value = -10 }).Forget();
        GUILayout.EndHorizontal();

        GUILayout.Space(8f);
        GUILayout.Label("Coin fly (từ giữa màn hình → ItemDisplay):", _sub);
        GUILayout.BeginHorizontal();
        // ItemCollect = add pending + animate + commit trong 1 event; From=null → bay từ giữa màn hình.
        if (GUILayout.Button("Bay +10", _btnOn, GUILayout.Height(84f))) EventBus.Publish(new ItemCollect { Key = _itemKey, From = null, Value = 10 }).Forget();
        if (GUILayout.Button("Bay +30", _btnOn, GUILayout.Height(84f))) EventBus.Publish(new ItemCollect { Key = _itemKey, From = null, Value = 30 }).Forget();
        if (GUILayout.Button("Bay +100", _btnOn, GUILayout.Height(84f))) EventBus.Publish(new ItemCollect { Key = _itemKey, From = null, Value = 100 }).Forget();
        GUILayout.EndHorizontal();
    }

    // ── Sound ─────────────────────────────────────────────
    private void DrawSound()
    {
        if (!Services.Has<SoundService>())
        {
            GUILayout.Label("SoundService chưa register.\nChạy qua boot scene để ModuleSound init.", _label);
            return;
        }

        GUILayout.Label("Bấm phát SFX:", _sub);
        EnumButtons<SoundId>(2, id => Sound.Play(id));
    }

    // ── Music ─────────────────────────────────────────────
    private void DrawMusic()
    {
        if (!Services.Has<MusicService>())
        {
            GUILayout.Label("MusicService chưa register.\nChạy qua boot scene để ModuleMusic init.", _label);
            return;
        }

        GUILayout.Label("Track:", _sub);
        EnumButtons<MusicId>(2, id => Music.Play(id));

        GUILayout.Space(6f);
        if (Btn("Stop")) Music.Stop();

        GUILayout.Space(6f);
        float vol = Services.TryGet<SettingsService>(out var s) ? s.MusicVolume : 1f;
        GUILayout.Label($"Volume: {Mathf.RoundToInt(vol * 100f)}%", _sub);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("0%", _button, GUILayout.Height(80f))) Music.SetVolume(0f);
        if (GUILayout.Button("25%", _button, GUILayout.Height(80f))) Music.SetVolume(0.25f);
        if (GUILayout.Button("50%", _button, GUILayout.Height(80f))) Music.SetVolume(0.5f);
        if (GUILayout.Button("75%", _button, GUILayout.Height(80f))) Music.SetVolume(0.75f);
        if (GUILayout.Button("100%", _button, GUILayout.Height(80f))) Music.SetVolume(1f);
        GUILayout.EndHorizontal();
    }

    // ── Widgets ───────────────────────────────────────────
    private bool Btn(string text) => GUILayout.Button(text, _button, GUILayout.Height(84f));

    private void EnumButtons<TEnum>(int perRow, Action<TEnum> onClick) where TEnum : Enum
    {
        int col = 0;
        bool open = false;
        foreach (TEnum v in Enum.GetValues(typeof(TEnum)))
        {
            if (v.ToString() == "None") continue;
            if (col == 0) { GUILayout.BeginHorizontal(); open = true; }
            if (GUILayout.Button(v.ToString(), _button, GUILayout.Height(84f))) onClick(v);
            if (++col == perRow) { GUILayout.EndHorizontal(); open = false; col = 0; }
        }
        if (open) GUILayout.EndHorizontal();
    }

    private bool Toggle(string text, ref bool value)
    {
        bool clicked = GUILayout.Button((value ? "◉  " : "○  ") + text, value ? _btnOn : _button, GUILayout.Height(76f));
        if (clicked) value = !value;
        return clicked;
    }

    private int IntField(int value, int min, int max)
    {
        string s = GUILayout.TextField(value.ToString(), _field, GUILayout.Width(130f), GUILayout.Height(72f));
        return int.TryParse(s, out int v) ? Mathf.Clamp(v, min, max) : value;
    }

    // ── Style setup ───────────────────────────────────────
    private void EnsureStyles()
    {
        if (_stylesReady) return;
        _stylesReady = true;

        Color text = new Color(0.93f, 0.94f, 0.97f);
        Color dim = new Color(0.62f, 0.65f, 0.72f);
        Color accent = new Color(0.29f, 0.56f, 0.96f);
        Color green = new Color(0.24f, 0.62f, 0.38f);
        Color greenHi = new Color(0.30f, 0.72f, 0.46f);
        Color btn = new Color(0.20f, 0.22f, 0.28f);
        Color btnHi = new Color(0.27f, 0.31f, 0.40f);
        Color tabOff = new Color(0.14f, 0.15f, 0.19f);

        _panel = Solid(24, new Color(0.10f, 0.11f, 0.14f, 0.97f));
        _bar = Solid(24, new Color(0.07f, 0.08f, 0.11f, 0.98f));

        _title = Text(44, text, FontStyle.Bold);
        _title.normal.textColor = accent;
        _label = Text(29, text, FontStyle.Normal);
        _label.wordWrap = true;
        _sub = Text(26, dim, FontStyle.Bold);
        _sub.wordWrap = true;

        _button = Rounded(14, btn, btnHi, text, 30, FontStyle.Normal);
        _btnOn = Rounded(14, green, greenHi, Color.white, 30, FontStyle.Bold);
        _tabOn = Rounded(18, accent, accent, Color.white, 28, FontStyle.Bold);
        _tabOff = Rounded(18, tabOff, btnHi, dim, 28, FontStyle.Normal);
        _tabOn.padding = _tabOff.padding = new RectOffset(6, 6, 10, 10);
        _tabOn.margin = _tabOff.margin = new RectOffset(5, 5, 4, 4);

        _field = new GUIStyle(GUI.skin.textField)
        {
            fontSize = 30,
            alignment = TextAnchor.MiddleCenter,
            border = new RectOffset(10, 10, 10, 10),
            padding = new RectOffset(10, 10, 6, 6),
            margin = new RectOffset(6, 6, 6, 6)
        };
        var fieldTex = MakeRound(10, new Color(0.05f, 0.05f, 0.07f));
        _field.normal.background = fieldTex;
        _field.focused.background = fieldTex;
        _field.normal.textColor = text;
        _field.focused.textColor = Color.white;
    }

    private static GUIStyle Text(int size, Color col, FontStyle fs)
    {
        var s = new GUIStyle { fontSize = size, fontStyle = fs };
        s.normal.textColor = col;
        return s;
    }

    private static GUIStyle Solid(int radius, Color fill)
    {
        var s = new GUIStyle { border = new RectOffset(radius, radius, radius, radius) };
        s.normal.background = MakeRound(radius, fill);
        return s;
    }

    private static GUIStyle Rounded(int radius, Color fill, Color hover, Color textCol, int size, FontStyle fs)
    {
        var s = new GUIStyle
        {
            border = new RectOffset(radius, radius, radius, radius),
            padding = new RectOffset(18, 18, 12, 12),
            margin = new RectOffset(6, 6, 6, 6),
            alignment = TextAnchor.MiddleCenter,
            fontSize = size,
            fontStyle = fs,
            wordWrap = false
        };
        s.normal.background = MakeRound(radius, fill);
        s.normal.textColor = textCol;
        s.hover.background = MakeRound(radius, hover);
        s.hover.textColor = textCol;
        s.active.background = s.hover.background;
        s.active.textColor = textCol;
        return s;
    }

    // Texture bo góc (9-slice qua RectOffset border). AA nhẹ ở viền cong.
    private static Texture2D MakeRound(int radius, Color col)
    {
        int size = radius * 2 + 6;
        var t = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        var px = new Color[size * size];
        int hi = size - 1 - radius;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            int cx = Mathf.Clamp(x, radius, hi);
            int cy = Mathf.Clamp(y, radius, hi);
            float dx = x - cx, dy = y - cy;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            var c = col;
            c.a *= Mathf.Clamp01(radius - d + 0.5f);
            px[y * size + x] = c;
        }
        t.SetPixels(px);
        t.Apply();
        return t;
    }
}
