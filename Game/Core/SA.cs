using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace StandardAssets
{
    /// <summary>
    /// Facade tiện dụng của framework: tập hợp toàn bộ API "một dòng" mà game gọi.
    /// Bên dưới mọi hàm chỉ phát event định kiểu qua SATypedBus — game KHÔNG cần biết
    /// module nào xử lý. Đây là điểm vào chính cho gameplay code.
    /// </summary>
    public static class SA
    {
        // ---------------- Scene / Transition ----------------

        public static Task LoadScene(int index)
        {
            return SATypedBus.Publish(new Ev.SceneLoad { Index = index });
        }

        public static Task Transition(bool cover)
        {
            return SATypedBus.Publish(new Ev.Transition { State = cover });
        }

        // ---------------- Level ----------------

        public static void LevelStarted(Dictionary<string, object> data = null)
        {
            SATypedBus.Publish(new Ev.LevelStarted { Extra = data });
        }

        /// <summary>
        /// Kết thúc level. completed=true -> "Level.Completed", ngược lại -> "Level.Failed".
        /// Đính kèm cờ showScreen (có hiện màn win/lose không) và delay trước khi hiện.
        /// </summary>
        public static void LevelEnded(bool completed, Dictionary<string, object> data = null, bool showScreen = true, float delay = 0f)
        {
            if (completed)
                SATypedBus.Publish(new Ev.LevelCompleted { Completed = true, ShowScreen = showScreen, ShowDelay = delay, Extra = data });
            else
                SATypedBus.Publish(new Ev.LevelFailed { Completed = false, ShowScreen = showScreen, ShowDelay = delay, Extra = data });
        }

        public static int CurrentLevel()
        {
            int result = 0;
            SATypedBus.Publish(new Ev.LevelGet { Reply = v => result = v });
            return result;
        }

        public static int CurrentLevelIndex()
        {
            int result = 0;
            SATypedBus.Publish(new Ev.LevelGetIndex { Reply = v => result = v });
            return result;
        }

        public static int CurrentLevelTries()
        {
            int result = 0;
            SATypedBus.Publish(new Ev.LevelGetTries { Reply = v => result = v });
            return result;
        }

        public static void SetLevel(int level)
            => SATypedBus.Publish(new Ev.LevelSet { Level = level });

        public static bool IsInSequence(bool includeLast = false)
        {
            bool result = false;
            SATypedBus.Publish(new Ev.LevelIsInSequence { IncludeLast = includeLast, Reply = v => result = v });
            return result;
        }

        // ---------------- UI Group ----------------

        public static void SetUIGroupVisible<T>(T group, bool visible) where T : Enum
        {
            SAEnumRef enumRef = new SAEnumRef
            {
                typeName = group.GetType().AssemblyQualifiedName,
                value = Convert.ToInt32(group)
            };
            SATypedBus.Publish(new Ev.UISetGroupVisible { Group = enumRef, Visible = visible });
        }

        public static void AttachUIGroup<T>(GameObject go, T group) where T : Enum
            => SAUIGroupManager.Attach(go, group);

        public static void DetachUIGroup<T>(GameObject go, T group) where T : Enum
            => SAUIGroupManager.Detach(go, group);

        // ---------------- Analytics ----------------

        public static void TrackEvent(string eventName, Dictionary<string, object> data = null)
        {
            SATypedBus.Publish(new Ev.AnalyticsSendEvent { EventName = eventName, Extra = data });
        }

        // ---------------- Save / Load ----------------

        public static void Save<T>(T value)
        {
            SATypedBus.Publish(new Ev.SaveDataSave { Key = typeof(T).Name, Value = value });
        }

        public static void Save<T>(string key, T value)
        {
            SATypedBus.Publish(new Ev.SaveDataSave { Key = key, Value = value });
        }

        public static T Load<T>() where T : class, new()
        {
            object result = null;
            SATypedBus.Publish(new Ev.SaveDataLoad { Key = typeof(T).Name, Type = typeof(T), Reply = v => result = v });
            return (result as T) ?? new T();
        }

        public static T Load<T>(string key) where T : class, new()
        {
            object result = null;
            SATypedBus.Publish(new Ev.SaveDataLoad { Key = key, Type = typeof(T), Reply = v => result = v });
            return (result as T) ?? new T();
        }

        // ---------------- Ads ----------------

        public static void ShowInterstitial(string placement, Action afterInterstitial = null)
        {
            if (afterInterstitial != null)
            {
                object tag = null;
                tag = SATypedBus.On<Ev.AdsInterstitialShown>(_ =>
                {
                    afterInterstitial();
                    SATypedBus.Off<Ev.AdsInterstitialShown>(tag);
                });
            }
            SATypedBus.Publish(new Ev.AdsShowInterstitial { Placement = placement });
        }

        public static void ShowRewarded(string placement, Action<bool> reward)
        {
            SATypedBus.Publish(new Ev.AdsShowRewarded { Placement = placement, OnRewarded = reward });
        }

        public static void ShowBanner() => SATypedBus.Publish(new Ev.AdsShowBanner());
        public static void HideBanner() => SATypedBus.Publish(new Ev.AdsHideBanner());

        // ---------------- IAP ----------------

        public static void Purchase(string productId, string placement)
            => SATypedBus.Publish(new Ev.IapPurchase { Placement = placement, ProductId = productId });

        // ---------------- Remote Config ----------------

        public static string GetRemoteConfig(string key)
        {
            string result = null;
            SATypedBus.Publish(new Ev.RemoteConfigGet { Key = key, Reply = value => result = value });
            return result;
        }

        // ---------------- Item / Currency ----------------

        public static void AddItem<T>(T key, int value, bool pending = false) where T : Enum
            => SATypedBus.Publish(new Ev.ItemAdd { Key = key.ToString(), Value = value, Pending = pending });

        public static int GetItem<T>(T key, bool pending = false) where T : Enum
        {
            int value = 0;
            SATypedBus.Publish(new Ev.ItemGet { Key = key.ToString(), Pending = pending, Reply = v => value = v });
            return value;
        }

        public static Task GetPendingItems<T>(T key, Transform from = null) where T : Enum
        {
            int item = GetItem(key, pending: true);
            if (item <= 0) return Task.CompletedTask;

            return SATypedBus.Publish(new Ev.ItemAnimationPlay { Key = key.ToString(), From = from, Value = item });
        }

        public static void GetItemIcon(string key, UnityAction<object> callback)
            => SATypedBus.Publish(new Ev.ItemGetIcon { Key = key, Reply = v => callback?.Invoke(v) });

        // ---------------- Booster ----------------

        public static void ActivateBooster(string key, UnityAction<bool> callback = null)
            => SATypedBus.Publish(new Ev.BoosterCmdActivate { Key = key, Reply = v => callback?.Invoke(v) });

        public static void GetBoosterPrice(string key, UnityAction<int> callback)
            => SATypedBus.Publish(new Ev.BoosterCmdGetPrice { Key = key, Reply = v => callback?.Invoke(v) });

        // ---------------- Particle / Sound / Haptic ----------------

        public static void PlayParticle<T>(T type, Vector3 position) where T : Enum
            => SATypedBus.Publish(new Ev.ParticlePlay { Type = Convert.ToInt32(type), Position = position, PositionIsScreenOffset = false });

        public static void PlayParticle<T>(T type, Vector2 position) where T : Enum
            => SATypedBus.Publish(new Ev.ParticlePlay { Type = Convert.ToInt32(type), Position = new Vector3(position.x, position.y, 0f), PositionIsScreenOffset = true });

        public static void Sound<T>(T audioType, float volume = 1f, float pitch = 1f) where T : Enum
            => SATypedBus.Publish(new Ev.SoundPlay { Type = Convert.ToInt32(audioType), Volume = volume, Pitch = pitch });

        public static void Haptic(params HapticType[] types)
        {
            SATypedBus.Publish(new Ev.HapticPlay { Types = types, Force = false });
        }

        public static void Haptic(bool force, params HapticType[] types)
        {
            SATypedBus.Publish(new Ev.HapticPlay { Types = types, Force = force });
        }

        // ---------------- Notification ----------------

        public static void NotificationSchedule(string id, string title, string body, DateTime triggerTime)
        {
            SATypedBus.Publish(new Ev.NotificationSchedule { Id = id, Title = title, Body = body, TriggerTime = triggerTime });
        }

        public static void NotificationSchedule(string id, string title, string body, TimeSpan delay)
            => NotificationSchedule(id, title, body, DateTime.Now + delay);

        public static void NotificationCancel(string id)
            => SATypedBus.Publish(new Ev.NotificationCancel { Id = id });

        public static void NotificationCancelAll()
            => SATypedBus.Publish(new Ev.NotificationCancelAll());

        // ---------------- Helper ----------------

        /// <summary>Format TimeSpan ngắn gọn: "2d 3h", "5h 10m", "3m 20s", "15s".</summary>
        public static string FormatTime(TimeSpan time)
        {
            if (time.TotalDays >= 1.0)    return $"{(int)time.TotalDays}d {time.Hours}h";
            if (time.TotalHours >= 1.0)   return $"{(int)time.TotalHours}h {time.Minutes}m";
            if (time.TotalMinutes >= 1.0) return $"{time.Minutes}m {time.Seconds}s";
            return $"{time.Seconds}s";
        }
    }
}
