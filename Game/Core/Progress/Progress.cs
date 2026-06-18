using System.Collections.Generic;
using UnityEngine;

namespace StandardAssets
{
    /// <summary>
    /// Registry tĩnh của các <see cref="AdvancedFillManager"/> theo khoá (string từ enum).
    /// Cho phép cập nhật mọi thanh fill cùng khoá bằng <see cref="Fill{T}"/> và phát event Progress.*.
    /// </summary>
    public static class Progress
    {
        private static readonly Dictionary<string, List<AdvancedFillManager>> _registry = new Dictionary<string, List<AdvancedFillManager>>();

        internal static void Register(string key, AdvancedFillManager mgr)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }
            if (!_registry.TryGetValue(key, out var value))
            {
                value = _registry[key] = new List<AdvancedFillManager>();
            }
            if (!value.Contains(mgr))
            {
                value.Add(mgr);
            }
        }

        internal static void Unregister(string key, AdvancedFillManager mgr)
        {
            if (!string.IsNullOrEmpty(key) && _registry.TryGetValue(key, out var value))
            {
                value.Remove(mgr);
            }
        }

        /// <summary>Cập nhật tiến trình cho mọi fill cùng khoá và phát event Progress.OnChanged/OnComplete.</summary>
        public static void Fill<T>(T key, int current, int target, bool direct = false)
        {
            if (target <= 0 || !_registry.TryGetValue(key.ToString(), out var value) || value.Count == 0)
            {
                return;
            }
            float ratio = Mathf.Clamp01((float)current / target);
            foreach (AdvancedFillManager item in value)
            {
                item.SetCounts(current, target);
                item.UpdateProgress(ratio, direct);
            }
            SATypedBus.Publish(new Ev.ProgressOnChanged
            {
                Key = key,
                Current = current,
                Target = target,
                Direct = direct
            });
            if (Mathf.Approximately(ratio, 1f))
            {
                SATypedBus.Publish(new Ev.ProgressOnComplete { Key = key });
            }
        }

        public static void FillDirect<T>(T key, int current, int target)
        {
            Fill(key, current, target, direct: true);
        }
    }
}
