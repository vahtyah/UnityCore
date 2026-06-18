using System.Collections.Generic;
using UnityEngine;

namespace VahTyah
{
    internal class ItemAnimationPool
    {
        private readonly Queue<RectTransform> _available = new Queue<RectTransform>();

        public int AvailableCount => _available.Count;

        public ItemAnimationPool(Transform parent, int maxSize, GameObject prefab)
        {
            for (int i = 0; i < maxSize; i++)
            {
                var obj = Object.Instantiate(prefab, parent);
                obj.SetActive(false);
                var rt = obj.GetComponent<RectTransform>() ?? obj.AddComponent<RectTransform>();
                _available.Enqueue(rt);
            }
        }

        public bool TryGet(out RectTransform rt)
        {
            if (_available.Count > 0) { rt = _available.Dequeue(); return true; }
            rt = null;
            return false;
        }

        public void Release(RectTransform rt)
        {
            rt.gameObject.SetActive(false);
            _available.Enqueue(rt);
        }
    }
}
