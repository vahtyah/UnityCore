using System;
using System.Collections.Generic;

namespace VahTyah
{
    [Serializable]
    public class ItemSaveData : ISaveData
    {
        [Serializable]
        public class Entry
        {
            public string Key;
            public int Current;
            public int Pending;
        }

        public List<Entry> Items = new List<Entry>();

        // Cache tra cứu O(1). JsonUtility không serialize Dictionary nên đánh dấu NonSerialized
        // và dựng lại từ List sau khi load (OnAfterLoad).
        [NonSerialized] private Dictionary<string, Entry> _index;

        public int Version => 1;

        public void OnAfterLoad() => RebuildIndex();
        public void OnBeforeSave() { }

        private void RebuildIndex()
        {
            _index = new Dictionary<string, Entry>(Items.Count);
            foreach (var e in Items)
                _index[e.Key] = e;
        }

        public bool TryGet(string key, out Entry entry)
        {
            if (_index == null) RebuildIndex();
            return _index.TryGetValue(key, out entry);
        }

        public Entry GetOrCreate(string key)
        {
            if (TryGet(key, out var entry))
                return entry;

            var e = new Entry { Key = key };
            Items.Add(e);
            _index[key] = e;
            return e;
        }
    }
}
