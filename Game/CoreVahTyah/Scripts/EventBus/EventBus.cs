using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VahTyah
{
    public static class EventBus
    {
        private abstract class Channel { }

        private sealed class Channel<T> : Channel where T : struct, IEvent
        {
            public struct Entry
            {
                public int Priority;
                public bool WaitFor;
                public Action<T> Sync;
                public Func<T, UniTask> Async;
            }

            public readonly List<Entry> Entries = new List<Entry>(4);

            public void Insert(Entry e)
            {
                int i;
                for (i = 0; i < Entries.Count && Entries[i].Priority <= e.Priority; i++) { }
                Entries.Insert(i, e);
            }
        }

        private static readonly Dictionary<Type, Channel> _channels = new Dictionary<Type, Channel>();

        private static class SnapshotPool<T> where T : struct, IEvent
        {
            public static readonly Stack<List<Channel<T>.Entry>> Pool = new Stack<List<Channel<T>.Entry>>(4);
        }

        public static object On<T>(Action<T> handler, int priority = 0) where T : struct, IEvent
        {
            Chan<T>().Insert(new Channel<T>.Entry { Priority = priority, WaitFor = true, Sync = handler });
            return handler;
        }

        public static object OnAsync<T>(Func<T, UniTask> handler, int priority = 0, bool waitFor = true) where T : struct, IEvent
        {
            Chan<T>().Insert(new Channel<T>.Entry { Priority = priority, WaitFor = waitFor, Async = handler });
            return handler;
        }

        public static void Off<T>(object tag) where T : struct, IEvent
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

        public static UniTask Publish<T>(in T evt) where T : struct, IEvent
        {
            if (!_channels.TryGetValue(typeof(T), out var c))
                return UniTask.CompletedTask;

            var entries = ((Channel<T>)c).Entries;
            if (entries.Count == 0)
                return UniTask.CompletedTask;

            var snapshot = SnapshotPool<T>.Pool.Count > 0
                ? SnapshotPool<T>.Pool.Pop()
                : new List<Channel<T>.Entry>(8);
            snapshot.AddRange(entries);

            return Dispatch(evt, snapshot);
        }

        private static async UniTask Dispatch<T>(T evt, List<Channel<T>.Entry> snapshot) where T : struct, IEvent
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
                            if (e.WaitFor)
                            {
                                await e.Async(evt);
                            }
                            else
                            {
                                e.Async(evt).Forget(
                                    ex => Debug.LogError($"[EventBus] {typeof(T).Name} async error: {ex.Message}"));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[EventBus] {typeof(T).Name}: {ex.Message}");
                    }
                }
            }
            finally
            {
                snapshot.Clear();
                SnapshotPool<T>.Pool.Push(snapshot);
            }
        }

        /// <summary>Chờ event T xảy ra lần kế tiếp (one-shot). Tự gỡ listener sau khi nhận.</summary>
        public static UniTask<T> WaitFor<T>() where T : struct, IEvent
        {
            var tcs = new UniTaskCompletionSource<T>();
            Action<T> handler = null;
            handler = e =>
            {
                Off<T>(handler);
                tcs.TrySetResult(e);
            };
            On<T>(handler);
            return tcs.Task;
        }

        public static bool HasListeners<T>() where T : struct, IEvent
            => _channels.TryGetValue(typeof(T), out var c) && ((Channel<T>)c).Entries.Count > 0;

        public static void Reset() => _channels.Clear();

        private static Channel<T> Chan<T>() where T : struct, IEvent
        {
            if (!_channels.TryGetValue(typeof(T), out var c))
                c = _channels[typeof(T)] = new Channel<T>();
            return (Channel<T>)c;
        }
    }

    public static class EventBusExtensions
    {
        public static void On<T>(this MonoBehaviour owner, Action<T> handler, int priority = 0) where T : struct, IEvent
        {
            object tag = EventBus.On(handler, priority);
            Cleaner(owner).Register(() => EventBus.Off<T>(tag));
        }

        public static void OnAsync<T>(this MonoBehaviour owner, Func<T, UniTask> handler, int priority = 0, bool waitFor = true) where T : struct, IEvent
        {
            object tag = EventBus.OnAsync(handler, priority, waitFor);
            Cleaner(owner).Register(() => EventBus.Off<T>(tag));
        }

        private static BusCleaner Cleaner(MonoBehaviour o)
            => o.GetComponent<BusCleaner>() ?? o.gameObject.AddComponent<BusCleaner>();
    }
}
