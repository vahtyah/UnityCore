using System.Collections.Generic;
using UnityEngine;

namespace StandardAssets
{
    /// <summary>
    /// Pool đơn giản các RectTransform dùng cho animation item.
    /// Khởi tạo sẵn maxSize instance (ẩn) và tái sử dụng qua TryGet/Release.
    /// </summary>
    internal class ItemAnimationPool
    {
        private readonly Queue<RectTransform> _available = new Queue<RectTransform>();

        /// <summary>Số instance đang rảnh trong pool.</summary>
        public int AvailableCount => _available.Count;

        public ItemAnimationPool(Transform parent, int maxSize, GameObject prefab)
        {
            for (int i = 0; i < maxSize; i++)
            {
                GameObject obj = Object.Instantiate(prefab, parent);
                obj.SetActive(false);
                RectTransform item = obj.GetComponent<RectTransform>() ?? obj.AddComponent<RectTransform>();
                _available.Enqueue(item);
            }
        }

        /// <summary>Lấy một instance rảnh. Trả về false nếu pool đã cạn.</summary>
        public bool TryGet(out RectTransform rt)
        {
            if (_available.Count > 0)
            {
                rt = _available.Dequeue();
                return true;
            }
            rt = null;
            return false;
        }

        /// <summary>Trả instance về pool và ẩn nó đi.</summary>
        public void Release(RectTransform rt)
        {
            rt.gameObject.SetActive(false);
            _available.Enqueue(rt);
        }
    }
}
