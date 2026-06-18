using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace StandardAssets
{
    /// <summary>
    /// Đánh dấu một struct là "sự kiện định kiểu" gửi qua <see cref="SATypedBus"/>.
    /// Nên khai báo struct readonly với field định kiểu rõ ràng. Với event hỏi-đáp
    /// (request/response), thêm field delegate (vd. Action&lt;int&gt; Reply) — delegate là
    /// reference type nên KHÔNG gây boxing.
    /// </summary>
    public interface ISAEvent { }

    /// <summary>
    /// Event bus ĐỊNH KIỂU, ZERO-BOXING — thay thế hoàn toàn SABus (string + dictionary).
    ///
    /// Giữ NGUYÊN ngữ nghĩa của SABus:
    ///   - Priority: số nhỏ chạy trước (ổn định theo thứ tự đăng ký khi bằng nhau).
    ///   - Sync + Async lẫn lộn trên cùng một event, dispatch theo đúng thứ tự priority.
    ///   - Publish trả về Task: handler sync chạy NGAY (inline) trước khi có await đầu tiên,
    ///     nên pattern request/response (đọc kết quả ngay sau Publish) vẫn đúng.
    ///   - Async handler: WaitFor=true thì await; false thì fire-and-forget (log nếu lỗi).
    ///
    /// Khác biệt với SABus: payload là struct đi qua generic T, KHÔNG ép sang object → hết boxing.
    /// </summary>
    public static class SATypedBus
    {
        // Container không generic để chứa trong Dictionary<Type, Channel> đồng nhất.
        private abstract class Channel { }

        private sealed class Channel<T> : Channel where T : struct, ISAEvent
        {
            public struct Entry
            {
                public int Priority;
                public bool WaitFor;
                public Action<T> Sync;
                public Func<T, Task> Async;
            }

            public readonly List<Entry> Entries = new List<Entry>(4);

            // Chèn theo priority (ổn định): giữ thứ tự đăng ký với cùng priority.
            public void Insert(Entry e)
            {
                int i;
                for (i = 0; i < Entries.Count && Entries[i].Priority <= e.Priority; i++) { }
                Entries.Insert(i, e);
            }
        }

        private static readonly Dictionary<Type, Channel> _channels = new Dictionary<Type, Channel>();

        // Pool snapshot để tránh cấp phát rác mỗi lần dispatch (giống SABus).
        private static class SnapshotPool<T> where T : struct, ISAEvent
        {
            public static readonly Stack<List<Channel<T>.Entry>> Pool = new Stack<List<Channel<T>.Entry>>(4);
        }

        // ---- Đăng ký ----

        /// <summary>
        /// Đăng ký handler đồng bộ. Trả về CHÍNH handler làm tag để Off() — không cấp phát object thừa.
        /// </summary>
        public static object On<T>(Action<T> handler, int priority = 0) where T : struct, ISAEvent
        {
            Chan<T>().Insert(new Channel<T>.Entry { Priority = priority, WaitFor = true, Sync = handler });
            return handler;
        }

        /// <summary>
        /// Đăng ký handler bất đồng bộ (waitFor=false để fire-and-forget). Trả về chính handler làm tag.
        /// </summary>
        public static object OnAsync<T>(Func<T, Task> handler, int priority = 0, bool waitFor = true) where T : struct, ISAEvent
        {
            Chan<T>().Insert(new Channel<T>.Entry { Priority = priority, WaitFor = waitFor, Async = handler });
            return handler;
        }

        /// <summary>Huỷ đăng ký: tag chính là delegate handler đã trả về từ On/OnAsync.</summary>
        public static void Off<T>(object tag) where T : struct, ISAEvent
        {
            if (!_channels.TryGetValue(typeof(T), out var c)) return;
            var list = ((Channel<T>)c).Entries;
            for (int i = 0; i < list.Count; i++)
            {
                if ((object)list[i].Sync == tag || (object)list[i].Async == tag)
                {
                    list.RemoveAt(i);
                    break;
                }
            }
        }

        // ---- Phát ----

        /// <summary>
        /// Phát event. Handler sync chạy inline (đồng bộ) theo thứ tự priority,
        /// nên kết quả request/response đã sẵn sàng ngay khi hàm này trả về (kể cả khi không await).
        /// Trả về Task để await chuỗi async handler nếu cần.
        /// </summary>
        public static Task Publish<T>(in T evt) where T : struct, ISAEvent
        {
            if (!_channels.TryGetValue(typeof(T), out var c))
                return Task.CompletedTask;

            var entries = ((Channel<T>)c).Entries;
            if (entries.Count == 0)
                return Task.CompletedTask;

            // Snapshot để handler có thể tự thêm/bớt trong lúc dispatch.
            var snapshot = SnapshotPool<T>.Pool.Count > 0 ? SnapshotPool<T>.Pool.Pop() : new List<Channel<T>.Entry>(8);
            snapshot.AddRange(entries);

            // Chạy phần sync inline trước; nếu không có async cần await thì kết thúc đồng bộ.
            return Dispatch(evt, snapshot);
        }

        private static async Task Dispatch<T>(T evt, List<Channel<T>.Entry> snapshot) where T : struct, ISAEvent
        {
            try
            {
                for (int i = 0; i < snapshot.Count; i++)
                {
                    var e = snapshot[i];
                    try
                    {
                        if (e.Sync != null)
                        {
                            e.Sync(evt);
                        }
                        else if (e.Async != null)
                        {
                            Task task = e.Async(evt);
                            if (e.WaitFor)
                            {
                                await task;
                            }
                            else
                            {
                                task.ContinueWith(
                                    t => Debug.LogError($"[SATypedBus] {typeof(T).Name} async error: {t.Exception?.InnerException?.Message}"),
                                    TaskContinuationOptions.OnlyOnFaulted);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[SATypedBus] {typeof(T).Name}: {ex.Message}");
                    }
                }
            }
            finally
            {
                snapshot.Clear();
                SnapshotPool<T>.Pool.Push(snapshot);
            }
        }

        /// <summary>Có handler nào cho event T không (tránh dựng payload nặng nếu không cần).</summary>
        public static bool HasListeners<T>() where T : struct, ISAEvent
            => _channels.TryGetValue(typeof(T), out var c) && ((Channel<T>)c).Entries.Count > 0;

        /// <summary>Xoá toàn bộ — gọi khi reset domain / chơi lại từ đầu.</summary>
        public static void ResetState() => _channels.Clear();

        private static Channel<T> Chan<T>() where T : struct, ISAEvent
        {
            if (!_channels.TryGetValue(typeof(T), out var c))
                c = _channels[typeof(T)] = new Channel<T>();
            return (Channel<T>)c;
        }
    }

    /// <summary>
    /// Extension cho MonoBehaviour: nghe SATypedBus và TỰ ĐỘNG huỷ khi object destroy
    /// (tái dùng <see cref="SABusCleaner"/>) — tránh leak / gọi handler trên object đã chết.
    /// </summary>
    public static class SATypedBusExtensions
    {
        public static void On<T>(this MonoBehaviour owner, Action<T> handler, int priority = 0) where T : struct, ISAEvent
        {
            object tag = SATypedBus.On(handler, priority);
            Cleaner(owner).Register(() => SATypedBus.Off<T>(tag));
        }

        public static void OnAsync<T>(this MonoBehaviour owner, Func<T, Task> handler, int priority = 0, bool waitFor = true) where T : struct, ISAEvent
        {
            object tag = SATypedBus.OnAsync(handler, priority, waitFor);
            Cleaner(owner).Register(() => SATypedBus.Off<T>(tag));
        }

        private static SABusCleaner Cleaner(MonoBehaviour o)
            => o.GetComponent<SABusCleaner>() ?? o.gameObject.AddComponent<SABusCleaner>();
    }
}
