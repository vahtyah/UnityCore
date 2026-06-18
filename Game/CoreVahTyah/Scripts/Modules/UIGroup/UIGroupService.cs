using System.Collections.Generic;
using UnityEngine;

namespace VahTyah
{
    /// <summary>
    /// Quản lý hiện/ẩn các nhóm UI theo UIGroupId. OR semantics: object hiện nếu BẤT KỲ
    /// group nào của nó đang shown.
    /// Public API dùng enum (type-safe); nội bộ key bằng int (cast compile-time) nên
    /// dictionary/hashset không box.
    /// </summary>
    public class UIGroupService
    {
        private readonly Dictionary<int, List<UIGroup>> _members = new Dictionary<int, List<UIGroup>>();
        private readonly HashSet<int> _shown = new HashSet<int>();
        private readonly List<int> _buffer = new List<int>(8);

        public void Add(UIGroup member)
        {
            var groups = member.Groups;
            if (groups == null || groups.Length == 0) return;

            for (int i = 0; i < groups.Length; i++)
            {
                int g = (int)groups[i];
                if (!_members.TryGetValue(g, out var list))
                    _members[g] = list = new List<UIGroup>(4);
                list.Add(member);
            }

            member.SetVisible(IsVisible(member));
        }

        public void Remove(UIGroup member)
        {
            var groups = member.Groups;
            if (groups == null) return;

            for (int i = 0; i < groups.Length; i++)
            {
                if (_members.TryGetValue((int)groups[i], out var list))
                    list.Remove(member);
            }
        }

        public void SetVisible(UIGroupId group, bool visible)
        {
            int g = (int)group;
            if (visible) _shown.Add(g);
            else _shown.Remove(g);

            RefreshGroup(g);
        }

        public void Show(UIGroupId group) => SetVisible(group, true);
        public void Hide(UIGroupId group) => SetVisible(group, false);

        /// <summary>Chỉ hiện group này, tắt tất cả group đang shown khác (chuyển màn full-screen).</summary>
        public void ShowExclusive(UIGroupId group)
        {
            int g = (int)group;

            _buffer.Clear();
            foreach (var s in _shown)
                if (s != g) _buffer.Add(s);

            _shown.Clear();
            _shown.Add(g);

            for (int i = 0; i < _buffer.Count; i++)
                RefreshGroup(_buffer[i]); // refresh group vừa tắt
            RefreshGroup(g);              // refresh group được hiện
        }

        public bool IsShown(UIGroupId group) => _shown.Contains((int)group);

        private void RefreshGroup(int group)
        {
            if (!_members.TryGetValue(group, out var list)) return;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null)
                    list[i].SetVisible(IsVisible(list[i]));
            }
        }

        private bool IsVisible(UIGroup member)
        {
            var groups = member.Groups;
            for (int i = 0; i < groups.Length; i++)
            {
                if (_shown.Contains((int)groups[i]))
                    return true;
            }
            return false;
        }

        /// <summary>Gắn UIGroup bằng code với danh sách group.</summary>
        public UIGroup Attach(GameObject go, params UIGroupId[] groups)
        {
            var comp = go.GetComponent<UIGroup>();
            if (comp == null) comp = go.AddComponent<UIGroup>();
            comp.SetGroups(groups);
            return comp;
        }
    }
}
