using System;
using System.Collections.Generic;
using UnityEngine;

namespace VahTyah
{
    public static class Services
    {
        private static readonly Dictionary<Type, object> _registry = new Dictionary<Type, object>();

        public static void Register<T>(T instance) where T : class
        {
            var type = typeof(T);
            if (!_registry.TryAdd(type, instance))
            {
                Debug.LogWarning($"[Services] Overwriting {type.Name}");
                _registry[type] = instance;
                return;
            }
        }

        public static T Get<T>() where T : class
        {
            if (_registry.TryGetValue(typeof(T), out var obj))
                return (T)obj;

            Debug.LogError($"[Services] {typeof(T).Name} not registered.");
            return null;
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (_registry.TryGetValue(typeof(T), out var obj))
            {
                service = (T)obj;
                return true;
            }
            service = null;
            return false;
        }

        public static bool Has<T>() where T : class
            => _registry.ContainsKey(typeof(T));

        public static void Remove<T>() where T : class
            => _registry.Remove(typeof(T));

        public static void Reset() => _registry.Clear();
    }
}
