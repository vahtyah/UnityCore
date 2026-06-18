using System;
using System.Collections.Generic;

namespace VahTyah
{
    [Serializable]
    public class ItemSaveData
    {
        [Serializable]
        public class Entry
        {
            public string Key;
            public int Current;
            public int Pending;
        }

        public List<Entry> Items = new List<Entry>();

        public bool TryGet(string key, out Entry entry)
        {
            foreach (var e in Items)
            {
                if (e.Key == key) { entry = e; return true; }
            }
            entry = null;
            return false;
        }

        public Entry GetOrCreate(string key)
        {
            if (TryGet(key, out var entry))
                return entry;

            var e = new Entry { Key = key };
            Items.Add(e);
            return e;
        }
    }
}
