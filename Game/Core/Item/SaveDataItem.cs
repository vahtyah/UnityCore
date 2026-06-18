using System;
using System.Collections.Generic;

namespace StandardAssets
{
    /// <summary>
    /// Dữ liệu lưu trữ số lượng item của người chơi. Mỗi entry gồm current
    /// (đã sở hữu) và pending (đã cấp nhưng chưa "thu" vào ví, ví dụ đang bay).
    /// </summary>
    [Serializable]
    internal class SaveDataItem
    {
        [Serializable]
        public class ItemEntry
        {
            public string key;
            public int current;
            public int pending;
        }

        public List<ItemEntry> items = new List<ItemEntry>();

        /// <summary>Tìm entry theo key.</summary>
        public bool TryGet(string key, out ItemEntry entry)
        {
            foreach (ItemEntry item in items)
            {
                if (item.key == key)
                {
                    entry = item;
                    return true;
                }
            }
            entry = null;
            return false;
        }

        /// <summary>Lấy entry theo key, tạo mới nếu chưa có.</summary>
        public ItemEntry GetOrCreate(string key)
        {
            if (TryGet(key, out var entry))
            {
                return entry;
            }
            ItemEntry newEntry = new ItemEntry { key = key };
            items.Add(newEntry);
            return newEntry;
        }
    }
}
