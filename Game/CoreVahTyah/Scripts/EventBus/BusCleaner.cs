using System;
using System.Collections.Generic;
using UnityEngine;

namespace VahTyah
{
    [AddComponentMenu("")]
    internal sealed class BusCleaner : MonoBehaviour
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
