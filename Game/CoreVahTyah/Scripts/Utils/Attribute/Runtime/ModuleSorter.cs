using System;
using System.Collections.Generic;
using System.Reflection;

namespace VahTyah
{
    /// <summary>
    /// Topo-sort ổn định cho danh sách Module theo ràng buộc <see cref="ModuleRequiresAttribute"/>.
    /// "Ổn định" = giữ nguyên thứ tự hiện tại giữa các module KHÔNG ràng buộc lẫn nhau
    /// (tie-break theo index gốc). Dùng bởi ModuleConfig editor để auto-sort; là pure C#
    /// (không phụ thuộc UnityEditor) nên có thể gọi ở runtime như safety-net nếu cần.
    /// </summary>
    public static class ModuleSorter
    {
        public static Module[] Sort(IReadOnlyList<Module> modules)
            => Sort(modules, out _, out _);

        /// <param name="cycleMembers">Các module nằm trong vòng lặp phụ thuộc (không sắp được).</param>
        /// <param name="missing">Cặp (module, type) khi ModuleRequires trỏ tới module không có trong list.</param>
        public static Module[] Sort(
            IReadOnlyList<Module> modules,
            out List<Module> cycleMembers,
            out List<KeyValuePair<Module, Type>> missing)
        {
            cycleMembers = new List<Module>();
            missing = new List<KeyValuePair<Module, Type>>();

            int n = modules?.Count ?? 0;
            var result = new List<Module>(n);
            if (n == 0) return result.ToArray();

            // Chỉ giữ node non-null, theo thứ tự gốc.
            var nodes = new List<int>();
            for (int i = 0; i < n; i++)
                if (modules[i] != null) nodes.Add(i);

            // deps[i] = tập index phải đứng TRƯỚC i.
            var deps = new Dictionary<int, HashSet<int>>();
            foreach (int i in nodes) deps[i] = new HashSet<int>();

            foreach (int i in nodes)
            {
                var requires = modules[i].GetType().GetCustomAttribute<ModuleRequiresAttribute>();
                if (requires == null) continue;

                foreach (Type t in requires.Required)
                {
                    if (t == null) continue;

                    bool found = false;
                    foreach (int j in nodes)
                    {
                        if (j == i) continue;
                        if (t.IsAssignableFrom(modules[j].GetType()))
                        {
                            deps[i].Add(j);
                            found = true;
                        }
                    }

                    if (!found)
                        missing.Add(new KeyValuePair<Module, Type>(modules[i], t));
                }
            }

            // Kahn ổn định: mỗi vòng chọn node "sẵn sàng" (mọi dep đã xử lý) có index gốc nhỏ nhất.
            var remaining = new List<int>(nodes); // đã ở thứ tự index tăng dần
            var done = new HashSet<int>();

            while (remaining.Count > 0)
            {
                int pick = -1;
                foreach (int i in remaining)
                {
                    bool ready = true;
                    foreach (int d in deps[i])
                    {
                        if (!done.Contains(d)) { ready = false; break; }
                    }
                    if (ready) { pick = i; break; }
                }

                if (pick == -1)
                {
                    // Còn vòng lặp: xả phần còn lại theo thứ tự gốc, đánh dấu cycle.
                    foreach (int i in remaining)
                    {
                        cycleMembers.Add(modules[i]);
                        result.Add(modules[i]);
                    }
                    break;
                }

                result.Add(modules[pick]);
                done.Add(pick);
                remaining.Remove(pick);
            }

            return result.ToArray();
        }
    }
}
