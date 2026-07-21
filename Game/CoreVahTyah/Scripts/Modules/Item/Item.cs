using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VahTyah
{
    /// <summary>Shortcut tĩnh cho tài nguyên (coin/gem/...). Bọc các event của <see cref="ModuleItem"/>
    /// (đi qua EventBus, không phải Service). Query đọc được ngay vì handler chạy sync.</summary>
    public static class Item
    {
        /// <summary>Số lượng hiện có. <paramref name="pending"/>=true → trừ phần đang bay (hiển thị mượt).</summary>
        public static int Get(string key, bool pending = false)
        {
            int v = 0;
            EventBus.Publish(new ItemGet { Key = key, Pending = pending, Reply = r => v = r }).Forget();
            return v;
        }

        /// <summary>Tiêu nguyên tử: đủ → trừ + trả true; thiếu → không trừ + trả false.</summary>
        public static bool TrySpend(string key, int value)
        {
            bool ok = false;
            EventBus.Publish(new ItemTrySpend { Key = key, Value = value, Reply = r => ok = r }).Forget();
            return ok;
        }

        /// <summary>Cộng thẳng. <paramref name="pending"/>=true → cộng vào hàng chờ (chờ bay/commit).</summary>
        public static void Add(string key, int value, bool pending = false)
            => EventBus.Publish(new ItemAdd { Key = key, Value = value, Pending = pending }).Forget();

        /// <summary>Combo an toàn: add pending + bay + commit trong 1 nhịp (không desync). Dùng khi thu item có sẵn.</summary>
        public static void Collect(string key, Transform from, int value)
            => EventBus.Publish(new ItemCollect { Key = key, From = from, Value = value }).Forget();

        /// <summary>Bay pending vào counter rồi commit. <paramref name="value"/>&lt;=0 → bay TẤT CẢ pending chưa bay của key.</summary>
        public static void FlyPending(string key, Transform from = null, int value = 0)
            => EventBus.Publish(new ItemFlyPending { Key = key, From = from, Value = value }).Forget();
    }
}
