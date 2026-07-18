using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VahTyah
{
    /// <summary>
    /// Service bay sprite về counter dùng chung (coin/heart/gem...). Tra <see cref="CollectProfile"/> theo
    /// <see cref="CollectAnimId"/>; caller truyền prefab + nguồn + đích + callback cộng số. Register bởi
    /// <see cref="ModuleCollectFly"/>. Không state currency — chỉ animation.
    /// </summary>
    public sealed class CollectFlyService
    {
        private readonly CollectFlyRunner _runner;
        private readonly Dictionary<CollectAnimId, CollectProfile> _byId = new Dictionary<CollectAnimId, CollectProfile>();
        private readonly CollectProfile _fallback;

        public CollectFlyService(CollectFlyRunner runner, IReadOnlyList<CollectProfile> profiles)
        {
            _runner = runner;
            if (profiles != null)
                for (int i = 0; i < profiles.Count; i++)
                {
                    var p = profiles[i];
                    if (p == null) continue;
                    if (_byId.ContainsKey(p.Id))
                        Debug.LogWarning($"[CollectFly] Trùng profile Id '{p.Id}' — dùng bản khai báo sau.");
                    _byId[p.Id] = p; // last-wins
                }

            if (!_byId.TryGetValue(CollectAnimId.Default, out _fallback) || _fallback == null)
            {
                _fallback = new CollectProfile();
                Debug.LogWarning("[CollectFly] Thiếu profile 'Default' — dùng default code làm fallback.");
            }
        }

        /// <summary>Profile cho <paramref name="id"/>; thiếu → Default (không bao giờ null).</summary>
        public CollectProfile GetProfile(CollectAnimId id) => _byId.TryGetValue(id, out var p) ? p : _fallback;

        /// <summary>Đăng ký sẵn pool cho prefab theo MaxPoolSize của profile.</summary>
        public void Prewarm(GameObject prefab, CollectAnimId id) => _runner.Prewarm(prefab, GetProfile(id).MaxPoolSize);

        /// <summary>Bay <paramref name="value"/> đơn vị từ <paramref name="start"/> → <paramref name="target"/>;
        /// gọi <paramref name="onPieceLanded"/>(pieceValue) mỗi mảnh đáp. Task hoàn tất khi tất cả đã đáp.</summary>
        public UniTask Fly(GameObject prefab, Vector3 start, Vector3 target, CollectAnimId id, int value, Action<int> onPieceLanded)
            => _runner.Fly(prefab, start, target, GetProfile(id), value, onPieceLanded);
    }
}
