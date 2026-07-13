using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using VahTyah.Core;        // LayerDrawingSystem — vẽ layer nền/header (rounded rect + border)
using VahTyah.Inspector;   // InspectorStyle / InspectorStyleData.GroupStyles — style BoxGroup

namespace VahTyah
{
    /// <summary>
    /// Custom inspector cho <see cref="ModuleConfig"/> — "lắp ráp" module không cần thao tác tay:
    ///  • Add Module ▾: reflection quét mọi <see cref="Module"/> subclass, tạo SUB-ASSET nhúng
    ///    ngay trong ModuleConfig.asset (không sinh file .asset rời), auto vào mảng + auto-sort.
    ///  • Auto-sort theo <see cref="ModuleRequiresAttribute"/> (topo-sort ổn định) → không sắp sai.
    ///  • Doctor: bắt thiếu [CoreModule], trùng type, null entry, order vi phạm, ModuleRequires thiếu target.
    ///  • [CoreModule] → ẩn nút Remove (module bắt buộc).
    ///
    /// Mọi thao tác đổi asset/mảng được hoãn (<see cref="_deferred"/>) rồi chạy SAU khi layout
    /// đóng, tránh ExitGUI giữa các layout scope.
    /// </summary>
    [CustomEditor(typeof(ModuleConfig))]
    public class ModuleConfigEditor : UnityEditor.Editor
    {
        private ModuleConfig _config;
        private SerializedProperty _modulesProp;
        private SerializedProperty _debugProp;

        private readonly Dictionary<UnityEngine.Object, UnityEditor.Editor> _inlineEditors = new();
        private Action _deferred;
        // Cảnh báo gắn theo từng module (key = array index): null / trùng type / thiếu required / sai order.
        private readonly Dictionary<int, List<(string msg, bool error)>> _moduleIssues = new();

        // Style BoxGroup lấy thẳng từ vahtyah.inspector (fallback default nếu chưa có EditorStyleDatabase).
        private InspectorStyleData.GroupStyles _group;
        private InspectorStyleData.GroupStyles Group =>
            _group ??= InspectorStyle.GetStyle()?.groupStyles
                       ?? InspectorStyleData.GroupStyles.CreateDefaultStyles(EditorGUIUtility.isProSkin);

        // Tiêu đề module: đậm + to hơn label field-group để module nổi hơn.
        private GUIStyle _titleStyle;
        private GUIStyle TitleStyle =>
            _titleStyle ??= new GUIStyle(Group.labelStyle) { fontSize = 13 };

        private void OnEnable()
        {
            _config = (ModuleConfig)target;
            _modulesProp = serializedObject.FindProperty(nameof(ModuleConfig.Modules));
            _debugProp = serializedObject.FindProperty(nameof(ModuleConfig.DebugLogs));
            _group = null;      // refresh theo theme khi chọn lại
            _titleStyle = null;
        }

        private void OnDisable()
        {
            foreach (var e in _inlineEditors.Values)
                if (e != null) DestroyImmediate(e);
            _inlineEditors.Clear();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Cảnh báo per-module tính reflection/AssetDatabase — chỉ 1 lần/frame (Layout), Repaint tái dùng.
            if (Event.current.type == EventType.Layout)
                ComputeModuleIssues();

            DrawModulesBox();

            serializedObject.ApplyModifiedProperties();

            // Chạy thao tác đổi asset SAU khi mọi layout scope đã đóng.
            if (_deferred != null)
            {
                var action = _deferred;
                _deferred = null;
                action();
                Repaint();
            }
        }

        // ---------------------------------------------------------------- Actions menu (☰)

        // Gom mọi thao tác cấp config: Add Module, Fix (missing core / absorb), và toggle Debug logs.
        private void ShowActionsMenu()
        {
            var present = PresentTypes();
            var menu = new GenericMenu();

            // Add Module/…  (module đã có → disable)
            foreach (var type in AllModuleTypes().OrderBy(MenuPath, StringComparer.Ordinal))
            {
                var content = new GUIContent("Add Module/" + ShortName(type) + (IsCore(type) ? "  (core)" : ""));
                if (present.Contains(type))
                    menu.AddDisabledItem(content, true);
                else
                {
                    var captured = type;
                    menu.AddItem(content, false, () => AddModule(captured));
                }
            }

            // Fix/…  (chỉ hiện khi có việc cần)
            string configPath = AssetDatabase.GetAssetPath(_config);
            var entries = CurrentEntries();
            var presentTypes = new HashSet<Type>(entries.Where(m => m != null).Select(m => m.GetType()));
            var missingCore = AllModuleTypes().Where(t => IsCore(t) && !presentTypes.Contains(t)).ToList();
            if (missingCore.Count > 0)
                menu.AddItem(new GUIContent($"Fix/Add missing core ({missingCore.Count})"), false,
                    () => { foreach (var t in missingCore) AddModule(t); });
            if (entries.Any(o => IsStandalone(o, configPath)))
                menu.AddItem(new GUIContent("Fix/Absorb standalone → sub-asset"), false, ConfirmAbsorb);

            menu.AddSeparator("");

            // Debug boot logs (toggle, checkmark hiện state)
            menu.AddItem(new GUIContent("Debug boot logs"), _debugProp.boolValue, () =>
            {
                serializedObject.Update();
                _debugProp.boolValue = !_debugProp.boolValue;
                serializedObject.ApplyModifiedProperties();
            });

            menu.ShowAsContext();
        }

        private static string ShortName(Type t)
        {
            var path = MenuPath(t);
            int slash = path.LastIndexOf('/');
            return slash >= 0 ? path.Substring(slash + 1) : path;
        }

        // Tính cảnh báo per-module (gắn icon lên header từng module). Chỉ chạy ở EventType.Layout.
        private void ComputeModuleIssues()
        {
            _moduleIssues.Clear();
            var entries = CurrentEntries();          // theo array index, có thể null
            int n = entries.Count;

            for (int i = 0; i < n; i++)
            {
                if (entries[i] == null)
                {
                    AddModuleIssue(i, "Ô Module trống (null).", false);
                    continue;
                }

                var ti = entries[i].GetType();

                // trùng type
                if (entries.Count(m => m != null && m.GetType() == ti) > 1)
                    AddModuleIssue(i, $"Trùng type {ti.Name} — chỉ nên có 1.", false);

                // requires: thiếu target / sai thứ tự
                var req = ti.GetCustomAttribute<ModuleRequiresAttribute>();
                if (req == null) continue;
                foreach (var t in req.Required)
                {
                    if (t == null) continue;
                    bool anyPresent = false, anyAfter = false;
                    for (int j = 0; j < n; j++)
                    {
                        if (j == i || entries[j] == null || !t.IsAssignableFrom(entries[j].GetType())) continue;
                        anyPresent = true;
                        if (j > i) anyAfter = true;
                    }
                    if (!anyPresent)
                        AddModuleIssue(i, $"[ModuleRequires {t.Name}] nhưng thiếu {t.Name} trong config.", false);
                    else if (anyAfter)
                        AddModuleIssue(i, $"Phải init SAU {t.Name} — hiện đang đứng trước.", true);
                }
            }
        }

        private void AddModuleIssue(int index, string msg, bool error)
        {
            if (!_moduleIssues.TryGetValue(index, out var list))
                _moduleIssues[index] = list = new List<(string, bool)>();
            list.Add((msg, error));
        }

        // ---------------------------------------------------------------- Module list

        // Khung "Modules" ngoài cùng (kiểu Watermelon BeginMenuBoxGroup): header có nút ☰ mở
        // Add menu + nút Sort; content chứa từng module box.
        private void DrawModulesBox()
        {
            var g = Group;
            bool repaint = Event.current.type == EventType.Repaint;

            Rect outer = EditorGUILayout.BeginVertical();
            if (repaint) LayerDrawingSystem.DrawLayers(outer, g.backgroundConfig);

            Rect header = GUILayoutUtility.GetRect(0, g.headerHeight, GUILayout.ExpandWidth(true));
            if (repaint) LayerDrawingSystem.DrawLayers(header, g.headerConfig);
            DrawModulesHeader(header, g);

            GUILayout.Space(g.contentPadding.top);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(g.contentPadding.left);
                using (new EditorGUILayout.VerticalScope())
                {
                    if (_modulesProp.arraySize == 0)
                        EditorGUILayout.HelpBox("Chưa có module. Bấm ☰ để Add.", MessageType.Info);
                    else
                        for (int i = 0; i < _modulesProp.arraySize; i++)
                            DrawModuleBox(i);
                }
                GUILayout.Space(g.contentPadding.right);
            }
            GUILayout.Space(g.contentPadding.bottom);

            EditorGUILayout.EndVertical();
            GUILayout.Space(g.groupSpacing);
        }

        private void DrawModulesHeader(Rect header, InspectorStyleData.GroupStyles g)
        {
            const float MENU = 26f, SORT = 50f, ARROW = 22f, GAP = 4f;
            float pad = g.headerPadding.left;
            float line = EditorGUIUtility.singleLineHeight;
            float cy = header.y + (header.height - line) * 0.5f;

            float x = header.xMax - pad;

            // ☰ menu: Add Module / Fix / Debug logs
            x -= MENU;
            var menu = new GUIContent(EditorGUIUtility.IconContent("_Menu@2x").image, "Actions");
            if (GUI.Button(new Rect(x, cy, MENU, line), menu))
                ShowActionsMenu();
            x -= GAP;

            // Sort
            x -= SORT;
            using (new EditorGUI.DisabledScope(_modulesProp.arraySize < 2))
                if (GUI.Button(new Rect(x, cy, SORT, line), "Sort"))
                    _deferred = AutoSort;

            // Đóng tất cả / Mở tất cả (cạnh Sort)
            using (new EditorGUI.DisabledScope(_modulesProp.arraySize == 0))
            {
                x -= GAP + ARROW;
                if (GUI.Button(new Rect(x, cy, ARROW, line), new GUIContent("▸", "Đóng tất cả")))
                    _deferred = () => SetAllExpanded(false);
                x -= GAP + ARROW;
                if (GUI.Button(new Rect(x, cy, ARROW, line), new GUIContent("▾", "Mở tất cả")))
                    _deferred = () => SetAllExpanded(true);
            }

            GUI.Label(new Rect(header.x + pad, header.y, Mathf.Max(0f, x - header.x - pad - GAP), header.height),
                "Modules", g.labelStyle);
        }

        // Một box kiểu BoxGroup (nền bo góc + header band), header bấm để đóng/mở.
        private void DrawModuleBox(int index)
        {
            var g = Group;
            var element = _modulesProp.GetArrayElementAtIndex(index);
            var obj = element.objectReferenceValue;
            bool core = obj != null && IsCore(obj.GetType());
            bool expanded = obj != null && element.isExpanded;
            bool repaint = Event.current.type == EventType.Repaint;

            // BeginVertical trả full rect lúc Repaint (layout đã tính) → vẽ nền TRƯỚC content.
            Rect boxRect = EditorGUILayout.BeginVertical();
            if (repaint) LayerDrawingSystem.DrawLayers(boxRect, g.backgroundConfig);

            Rect header = GUILayoutUtility.GetRect(0, g.headerHeight, GUILayout.ExpandWidth(true));
            if (repaint)
            {
                LayerDrawingSystem.DrawLayers(header, g.headerConfig);

                // Module MỞ → header phủ cam + viền cam bao cả box (gồm cạnh dưới), giữ bo góc.
                if (expanded)
                {
                    LayerDrawingSystem.DrawLayers(header, OpenHeaderConfig);
                    LayerDrawingSystem.DrawLayers(boxRect, OpenBorderConfig);
                }
            }

            DrawModuleHeader(header, index, element, obj, core, g);

            if (obj != null && element.isExpanded)
            {
                GUILayout.Space(g.contentPadding.top);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(g.contentPadding.left);
                    using (new EditorGUILayout.VerticalScope())
                    {
                        if (_moduleIssues.TryGetValue(index, out var mIssues))
                            foreach (var (msg, error) in mIssues)
                                EditorGUILayout.HelpBox(msg, error ? MessageType.Error : MessageType.Warning);

                        var ed = GetInlineEditor(obj);
                        if (ed != null) ed.OnInspectorGUI();
                    }
                    GUILayout.Space(g.contentPadding.right);
                }
                GUILayout.Space(g.contentPadding.bottom);
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(g.groupSpacing);
        }

        private void DrawModuleHeader(Rect header, int index, SerializedProperty element,
            UnityEngine.Object obj, bool core, InspectorStyleData.GroupStyles g)
        {
            const float KEBAB = 22f, ICON = 20f, GAP = 4f;
            float pad = g.headerPadding.left;
            float line = EditorGUIUtility.singleLineHeight;
            float cy = header.y + (header.height - line) * 0.5f;

            float x = header.xMax - pad;

            // 1 nút ⋮ gộp Move Up / Move Down / Remove (kiểu context menu của Watermelon).
            x -= KEBAB;
            if (GUI.Button(new Rect(x, cy, KEBAB, line), new GUIContent("⋮", "Actions")))
                ShowModuleMenu(index, obj, core);

            // Icon cảnh báo gắn ngay trên module (idiom Required của vahtyah.inspector).
            if (_moduleIssues.TryGetValue(index, out var mIssues) && mIssues.Count > 0)
            {
                bool err = mIssues.Any(z => z.error);
                x -= GAP + ICON;
                var ic = new GUIContent(
                    EditorGUIUtility.IconContent(err ? "console.erroricon.sml" : "console.warnicon.sml").image,
                    string.Join("\n", mIssues.Select(z => z.msg)));
                GUI.Label(new Rect(x, cy, ICON, line), ic);
            }

            // Right-click BẤT KỲ đâu trên header → context menu. Xử lý TRƯỚC nút toggle + Use()
            // để nút vô hình không "nuốt" chuột phải thành toggle (Unity GUI.Button ăn cả right-click).
            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 1 && header.Contains(e.mousePosition))
            {
                ShowModuleMenu(index, obj, core);
                e.Use();
            }

            // Vùng còn lại của header = nút vô hình click TRÁI để đóng/mở.
            var toggleRect = new Rect(header.x, header.y, Mathf.Max(0f, x - header.x - GAP), header.height);
            if (obj != null && GUI.Button(toggleRect, GUIContent.none, GUIStyle.none))
                element.isExpanded = !element.isExpanded;

            string arrow = obj != null ? (element.isExpanded ? "▾  " : "▸  ") : "    ";
            string title = $"{arrow}#{index}  {DisplayName(obj)}" + (core ? "     [core]" : "");
            GUI.Label(new Rect(header.x + pad, header.y, Mathf.Max(0f, toggleRect.width - pad), header.height),
                title, TitleStyle);
        }

        // Tên hiển thị: bỏ prefix "Module_" / "Module" (chỉ hiển thị, KHÔNG đổi tên asset/file).
        private static string DisplayName(UnityEngine.Object obj)
        {
            if (obj == null) return "<null>";
            string n = obj.name;
            if (n.StartsWith("Module_", StringComparison.Ordinal)) return n.Substring(7);
            if (n.StartsWith("Module", StringComparison.Ordinal) && n.Length > 6) return n.Substring(6);
            return n;
        }

        // Context menu 1 module: Move Up / Move Down / Remove (mở từ nút ⋮ hoặc right-click header).
        private void ShowModuleMenu(int index, UnityEngine.Object obj, bool core)
        {
            int count = _modulesProp.arraySize;
            var menu = new GenericMenu();

            if (index > 0)
                menu.AddItem(new GUIContent("Move Up"), false, () => _deferred = () => Move(index, index - 1));
            else
                menu.AddDisabledItem(new GUIContent("Move Up"));

            if (index < count - 1)
                menu.AddItem(new GUIContent("Move Down"), false, () => _deferred = () => Move(index, index + 1));
            else
                menu.AddDisabledItem(new GUIContent("Move Down"));

            menu.AddSeparator("");

            if (obj != null && core)
                menu.AddDisabledItem(new GUIContent("Remove (core)"));
            else if (obj == null)
                menu.AddItem(new GUIContent("Remove"), false, () => _deferred = () => RemoveAt(index));
            else
            {
                var captured = obj;
                menu.AddItem(new GUIContent("Remove"), false, () =>
                {
                    if (EditorUtility.DisplayDialog("Remove module", $"Xoá {captured.name}?", "Remove", "Cancel"))
                        _deferred = () => RemoveAt(index);
                });
            }

            menu.ShowAsContext();
        }

        private UnityEditor.Editor GetInlineEditor(UnityEngine.Object obj)
        {
            if (obj == null) return null;
            if (!_inlineEditors.TryGetValue(obj, out var ed) || ed == null)
            {
                ed = CreateEditor(obj);
                _inlineEditors[obj] = ed;
            }
            return ed;
        }

        // ---------------------------------------------------------------- Mutations (deferred)

        private void AddModule(Type type)
        {
            if (!typeof(Module).IsAssignableFrom(type) || type.IsAbstract) return;

            Undo.RecordObject(_config, "Add Module");

            var module = (Module)CreateInstance(type);
            module.name = DefaultName(type);
            module.hideFlags = HideFlags.None; // hiển thị nested dưới ModuleConfig trong Project view

            AssetDatabase.AddObjectToAsset(module, _config);

            serializedObject.Update();
            int idx = _modulesProp.arraySize;
            _modulesProp.arraySize++;
            _modulesProp.GetArrayElementAtIndex(idx).objectReferenceValue = module;
            serializedObject.ApplyModifiedProperties();

            EditorUtility.SetDirty(_config);
            RebuildSorted();
            AssetDatabase.SaveAssets();
        }

        private void RemoveAt(int index)
        {
            serializedObject.Update();
            if (index < 0 || index >= _modulesProp.arraySize) return;

            var element = _modulesProp.GetArrayElementAtIndex(index);
            var obj = element.objectReferenceValue;

            Undo.RecordObject(_config, "Remove Module");

            element.objectReferenceValue = null;
            _modulesProp.DeleteArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();

            // Chỉ destroy nếu là SUB-ASSET của chính config này (không xoá file .asset rời).
            if (obj != null &&
                AssetDatabase.IsSubAsset(obj) &&
                AssetDatabase.GetAssetPath(obj) == AssetDatabase.GetAssetPath(_config))
            {
                if (_inlineEditors.TryGetValue(obj, out var ed))
                {
                    if (ed != null) DestroyImmediate(ed);
                    _inlineEditors.Remove(obj);
                }
                AssetDatabase.RemoveObjectFromAsset(obj);
                DestroyImmediate(obj, true);
            }

            EditorUtility.SetDirty(_config);
            AssetDatabase.SaveAssets();
        }

        private void Move(int from, int to)
        {
            serializedObject.Update();
            if (to < 0 || to >= _modulesProp.arraySize) return;
            _modulesProp.MoveArrayElement(from, to);
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(_config);
        }

        private void AutoSort()
        {
            RebuildSorted();
            EditorUtility.SetDirty(_config);
            AssetDatabase.SaveAssets();
        }

        // Mở/đóng foldout tất cả module (chỉ đổi isExpanded, không đụng asset).
        private void SetAllExpanded(bool value)
        {
            serializedObject.Update();
            for (int i = 0; i < _modulesProp.arraySize; i++)
            {
                var el = _modulesProp.GetArrayElementAtIndex(i);
                if (el.objectReferenceValue != null) el.isExpanded = value;
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void ConfirmAbsorb()
        {
            if (EditorUtility.DisplayDialog(
                    "Absorb standalone modules",
                    "Gộp các module .asset rời thành sub-asset nhúng trong ModuleConfig?\n\n" +
                    "• Giữ NGUYÊN mọi tham số (dùng CopySerialized).\n" +
                    "• File .asset cũ sẽ bị xoá.\n" +
                    "• Chỉ an toàn vì module không được asset khác tham chiếu.\n\n" +
                    "Hãy chắc project đang trong version control trước khi chạy.",
                    "Absorb", "Cancel"))
                AbsorbStandalone();
        }

        /// <summary>
        /// Gộp mọi module đang là file .asset rời thành sub-asset của config,
        /// bê nguyên tham số cũ (CopySerialized) rồi xoá file cũ. Value-preserving.
        /// </summary>
        private void AbsorbStandalone()
        {
            string configPath = AssetDatabase.GetAssetPath(_config);
            serializedObject.Update();
            Undo.RecordObject(_config, "Absorb Modules");

            var toDelete = new List<string>();

            for (int i = 0; i < _modulesProp.arraySize; i++)
            {
                var el = _modulesProp.GetArrayElementAtIndex(i);
                var old = el.objectReferenceValue as Module;
                if (!IsStandalone(old, configPath)) continue;

                var copy = (Module)CreateInstance(old.GetType());
                EditorUtility.CopySerialized(old, copy); // bê nguyên toàn bộ [SerializeField]
                copy.name = old.name;
                copy.hideFlags = HideFlags.None;

                AssetDatabase.AddObjectToAsset(copy, _config);
                el.objectReferenceValue = copy;

                var p = AssetDatabase.GetAssetPath(old);
                if (!string.IsNullOrEmpty(p)) toDelete.Add(p);
            }

            // Repoint mảng sang sub-asset TRƯỚC khi xoá file cũ (tránh null reference).
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(_config);
            AssetDatabase.SaveAssets();

            foreach (var p in toDelete.Distinct())
                AssetDatabase.DeleteAsset(p);

            // Inline editor cache có thể trỏ object đã huỷ → clear.
            foreach (var e in _inlineEditors.Values) if (e != null) DestroyImmediate(e);
            _inlineEditors.Clear();

            AssetDatabase.SaveAssets();
            RebuildSorted();
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(configPath);
        }

        private static bool IsStandalone(UnityEngine.Object o, string configPath)
        {
            if (o == null || AssetDatabase.IsSubAsset(o)) return false;
            var path = AssetDatabase.GetAssetPath(o);
            return !string.IsNullOrEmpty(path) && path != configPath;
        }

        private void RebuildSorted()
        {
            serializedObject.Update();
            var sorted = ModuleSorter.Sort(CurrentEntries().Where(m => m != null).ToList());

            _modulesProp.ClearArray();
            for (int i = 0; i < sorted.Length; i++)
            {
                _modulesProp.arraySize++;
                _modulesProp.GetArrayElementAtIndex(i).objectReferenceValue = sorted[i];
            }
            serializedObject.ApplyModifiedProperties();
        }

        // ---------------------------------------------------------------- Helpers

        private List<Module> CurrentEntries()
        {
            var list = new List<Module>(_modulesProp.arraySize);
            for (int i = 0; i < _modulesProp.arraySize; i++)
                list.Add(_modulesProp.GetArrayElementAtIndex(i).objectReferenceValue as Module);
            return list;
        }

        private HashSet<Type> PresentTypes()
        {
            var set = new HashSet<Type>();
            foreach (var m in CurrentEntries())
                if (m != null) set.Add(m.GetType());
            return set;
        }

        // ---------------------------------------------------------------- Type discovery cache

        private struct ModuleTypeInfo
        {
            public Type Type;
            public bool IsCore;
            public string MenuPath;
            public string DefaultName;
        }

        // Build 1 lần/domain reload (static reset khi Unity recompile) — KHÔNG quét assembly mỗi frame.
        private static ModuleTypeInfo[] _typeCache;
        private static Dictionary<Type, ModuleTypeInfo> _typeMap;

        private static void EnsureTypeCache()
        {
            if (_typeCache != null) return;

            var list = new List<ModuleTypeInfo>();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }

                foreach (var t in types)
                {
                    if (t == null || !t.IsClass || t.IsAbstract || !t.IsSubclassOf(typeof(Module))) continue;
                    list.Add(new ModuleTypeInfo
                    {
                        Type = t,
                        IsCore = t.GetCustomAttribute<CoreModuleAttribute>() != null,
                        MenuPath = BuildMenuPath(t),
                        DefaultName = BuildDefaultName(t),
                    });
                }
            }

            _typeCache = list.ToArray();
            _typeMap = _typeCache.ToDictionary(x => x.Type);
        }

        private static IEnumerable<Type> AllModuleTypes()
        {
            EnsureTypeCache();
            return _typeCache.Select(x => x.Type);
        }

        private static bool IsCore(Type t)
        {
            EnsureTypeCache();
            return _typeMap.TryGetValue(t, out var info) && info.IsCore;
        }

        private static string MenuPath(Type t)
        {
            EnsureTypeCache();
            return _typeMap.TryGetValue(t, out var info) ? info.MenuPath : ObjectNames.NicifyVariableName(t.Name);
        }

        private static string DefaultName(Type t)
        {
            EnsureTypeCache();
            return _typeMap.TryGetValue(t, out var info) ? info.DefaultName : t.Name;
        }

        private static string BuildMenuPath(Type t)
        {
            var attr = t.GetCustomAttribute<CreateAssetMenuAttribute>();
            if (attr != null && !string.IsNullOrEmpty(attr.menuName))
            {
                var mn = attr.menuName;
                const string prefix = "VahTyah/";
                if (mn.StartsWith(prefix, StringComparison.Ordinal))
                    mn = mn.Substring(prefix.Length);
                return mn;
            }
            return ObjectNames.NicifyVariableName(t.Name);
        }

        private static string BuildDefaultName(Type t)
        {
            var attr = t.GetCustomAttribute<CreateAssetMenuAttribute>();
            if (attr != null && !string.IsNullOrEmpty(attr.fileName))
                return attr.fileName;
            return t.Name;
        }

        private static readonly Color OpenAccent = new Color(0.88f, 0.62f, 0.32f);

        // Module MỞ → phủ màu cam lên header, dùng rounded-rect radius 4 khớp header band
        // (chỉ đổi màu, không đổi geometry). Alpha < 1 để border/nền dưới lộ nhẹ.
        private static LayerConfiguration _openHeaderConfig;
        private static LayerConfiguration OpenHeaderConfig
        {
            get
            {
                if (_openHeaderConfig == null)
                {
                    _openHeaderConfig = new LayerConfiguration(1);
                    _openHeaderConfig.layers[0] =
                        Layer.CreateRoundedRect(new Color(OpenAccent.r, OpenAccent.g, OpenAccent.b, 0.7f), 4f);
                }
                return _openHeaderConfig;
            }
        }

        // Viền cam bao cả box khi mở (border rounded-rect radius 4, khớp viền neutral).
        private static LayerConfiguration _openBorderConfig;
        private static LayerConfiguration OpenBorderConfig
        {
            get
            {
                if (_openBorderConfig == null)
                {
                    _openBorderConfig = new LayerConfiguration(1);
                    _openBorderConfig.layers[0] = Layer.CreateBorder(OpenAccent, 1f, 4f);
                }
                return _openBorderConfig;
            }
        }
    }
}
