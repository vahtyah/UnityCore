using System;
using System.Collections.Generic;
using UnityEngine;

namespace StandardAssets
{
    /// <summary>
    /// Component "vô hình" tự gắn vào GameObject khi dùng owner.On(...)/owner.OnAsync(...).
    /// Lưu danh sách hành động cleanup và chạy hết khi object bị destroy,
    /// đảm bảo mọi listener của object đó được Off() đúng lúc.
    /// </summary>
    [AddComponentMenu("")] // ẩn khỏi menu Add Component
    internal sealed class SABusCleaner : MonoBehaviour
    {
        private readonly List<Action> _cleanups = new List<Action>(4);

        internal void Register(Action c) => _cleanups.Add(c);

        private void OnDestroy()
        {
            foreach (var cleanup in _cleanups)
                cleanup();
            _cleanups.Clear();
        }
    }
}
