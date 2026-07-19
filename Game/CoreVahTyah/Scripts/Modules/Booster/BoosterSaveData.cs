using System;
using System.Collections.Generic;

namespace VahTyah
{
    [Serializable]
    public class BoosterSaveData : ISaveData
    {
        [Serializable]
        public class Entry
        {
            public string Key;
            public int UsesThisLevel;
        }

        public List<Entry> Entries = new List<Entry>();

        [NonSerialized] private Dictionary<string, Entry> _index;

        public int Version => 1;
        public void OnAfterLoad() => RebuildIndex();
        public void OnBeforeSave() { }

        private void RebuildIndex()
        {
            _index = new Dictionary<string, Entry>(Entries.Count);
            foreach (var e in Entries)
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
            Entries.Add(e);
            _index[key] = e;
            return e;
        }
    }
}
