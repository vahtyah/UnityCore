using System;
using System.Collections.Generic;
using UnityEngine;

namespace StandardAssets
{
    /// <summary>
    /// Catalog toàn bộ event định kiểu thay cho các chuỗi event của SABus cũ.
    /// Mỗi struct = một event. Field = payload. Field delegate (Action&lt;...&gt;) = reply/callback
    /// (delegate là reference type nên KHÔNG gây boxing).
    ///
    /// Quy ước tên: gộp theo domain, bỏ dấu chấm. Vd "Level.Get" -> Ev.LevelGet.
    /// Dùng: SATypedBus.Publish(new Ev.LevelGet { Reply = v => x = v });
    ///       SATypedBus.On&lt;Ev.LevelGet&gt;(e => e.Reply?.Invoke(Current));
    /// </summary>
    public static class Ev
    {
        // ---------------- App / lifecycle ----------------
        public struct AppReady : ISAEvent { }       // "SA.AppReady" (được await)
        public struct AppPaused : ISAEvent { }
        public struct AppResumed : ISAEvent { }
        public struct AppQuitting : ISAEvent { }

        // ---------------- Scene / Transition (async) ----------------
        public struct SceneLoad : ISAEvent { public int Index; }      // async
        public struct SceneLoaded : ISAEvent { public int Index; }    // async + sync mixed
        public struct SceneUnloading : ISAEvent { }                   // async
        public struct Transition : ISAEvent { public bool State; }    // async + sync mixed

        // ---------------- Level ----------------
        // Payload động (Extra) giữ tương thích facade; subscriber hiện tại không đọc Extra.
        public struct LevelStarted : ISAEvent { public Dictionary<string, object> Extra; }
        public struct LevelCompleted : ISAEvent { public bool Completed; public bool ShowScreen; public float ShowDelay; public Dictionary<string, object> Extra; }
        public struct LevelFailed : ISAEvent { public bool Completed; public bool ShowScreen; public float ShowDelay; public Dictionary<string, object> Extra; }
        public struct LevelSet : ISAEvent { public int Level; }
        public struct LevelOnChanged : ISAEvent { public int Level; }
        public struct LevelGet : ISAEvent { public Action<int> Reply; }
        public struct LevelGetIndex : ISAEvent { public Action<int> Reply; }
        public struct LevelGetTries : ISAEvent { public Action<int> Reply; }
        public struct LevelIsInSequence : ISAEvent { public bool IncludeLast; public Action<bool> Reply; }

        // ---------------- Save / RemoteConfig ----------------
        public struct SaveDataSave : ISAEvent { public string Key; public object Value; public Action Reply; } // Reply = báo hoàn tất (tuỳ chọn)
        public struct SaveDataLoad : ISAEvent { public string Key; public Type Type; public Action<object> Reply; }
        public struct RemoteConfigGet : ISAEvent { public string Key; public Action<string> Reply; }

        // ---------------- Item ----------------
        public struct ItemAdd : ISAEvent { public string Key; public int Value; public bool Pending; }
        public struct ItemGet : ISAEvent { public string Key; public bool Pending; public Action<int> Reply; }
        public struct ItemGetPending : ISAEvent { public string Key; public int Value; }
        public struct ItemGetData : ISAEvent { public string Key; public Action<object> Reply; } // reply: ModuleItem.ItemDefinition (boxed qua object)
        public struct ItemGetIcon : ISAEvent { public string Key; public Action<object> Reply; }
        public struct ItemOnChanged : ISAEvent { public string Key; }
        public struct ItemShowDisplay : ISAEvent { }
        public struct ItemHideDisplay : ISAEvent { }
        public struct ItemAnimationPlay : ISAEvent { public string Key; public Transform From; public int Value; } // async

        // ---------------- Heart ----------------
        public struct HeartAdd : ISAEvent { public int Value; public bool Direct; public bool ResetTimestamp; }
        public struct HeartUse : ISAEvent { public int Value; public Action<bool> Reply; }
        public struct HeartGet : ISAEvent { public bool WithSaved; public Action<int> Reply; }
        public struct HeartCommitSaved : ISAEvent { public int Value; }   // "Heart.CommitSaved" + "Heart.GetSaved" dùng chung handler
        public struct HeartGetSaved : ISAEvent { public int Value; }
        public struct HeartIsFull : ISAEvent { public Action<bool> Reply; }
        public struct HeartIsInfinity : ISAEvent { public Action<bool> Reply; }
        public struct HeartGetTimerDisplay : ISAEvent { public Action<string> Reply; }
        public struct HeartGetInfinityTimerDisplay : ISAEvent { public Action<string> Reply; }
        public struct HeartGetNextSeconds : ISAEvent { public Action<float> Reply; }
        public struct HeartAddInfinity : ISAEvent { public float Minutes; public bool Direct; }
        public struct HeartGetInfinity : ISAEvent { public bool WithSaved; public Action<long> Reply; }
        public struct HeartCommitSavedInfinity : ISAEvent { public float Minutes; } // + "Heart.GetInfinitySaved" dùng chung handler
        public struct HeartGetInfinitySaved : ISAEvent { public float Minutes; }

        // ---------------- Feature ----------------
        public struct FeatureCmdRefresh : ISAEvent { }
        public struct FeatureCmdConsumePendingUnlock : ISAEvent { public Action<bool> Reply; }
        public struct FeatureOnState : ISAEvent
        {
            public bool Active; public int Index; public float PMin; public float PMax;
            public bool Unlocked; public Sprite Sprite; public Sprite SpriteDark; public string ConditionText;
        }
        public struct FeatureOnUnlocked : ISAEvent { public int Index; }
        public struct FeatureOnPendingUnlock : ISAEvent { public int Index; }

        // ---------------- Progress (không có subscriber SABus trong Test; phát cho listener ngoài) ----------------
        public struct ProgressOnChanged : ISAEvent { public object Key; public int Current; public int Target; public bool Direct; }
        public struct ProgressOnComplete : ISAEvent { public object Key; }

        // ---------------- Particle / Sound / Haptic ----------------
        // PositionIsScreenOffset=true tương đương publisher cũ gửi Vector2 (offset từ tâm màn hình -> UI);
        // false tương đương gửi Vector3 (world position). Thay cho check 'is Vector2' của SABus cũ.
        public struct ParticlePlay : ISAEvent { public int Type; public Vector3 Position; public bool PositionIsScreenOffset; public Transform Transform; }
        public struct SoundPlay : ISAEvent { public int Type; public float Volume; public float Pitch; }
        public struct HapticPlay : ISAEvent { public HapticType[] Types; public bool Force; } // async

        // ---------------- UI ----------------
        public struct UISetGroupVisible : ISAEvent { public SAEnumRef Group; public bool Visible; }

        // ---------------- Analytics (không có subscriber trong Test) ----------------
        public struct AnalyticsSendEvent : ISAEvent { public string EventName; public Dictionary<string, object> Extra; }

        // ---------------- Ads (subscriber từ SDK module ngoài; chỉ publisher/one-shot trong Test) ----------------
        public struct AdsShowInterstitial : ISAEvent { public string Placement; }
        public struct AdsInterstitialShown : ISAEvent { }
        public struct AdsShowRewarded : ISAEvent { public string Placement; public Action<bool> OnRewarded; }
        public struct AdsShowBanner : ISAEvent { }
        public struct AdsHideBanner : ISAEvent { }

        // ---------------- IAP ----------------
        public struct IapPurchase : ISAEvent { public string Placement; public string ProductId; }

        // ---------------- Booster (publisher trong Test; subscriber từ SDK module ngoài) ----------------
        public struct BoosterCmdActivate : ISAEvent { public string Key; public Action<bool> Reply; }
        public struct BoosterCmdGetPrice : ISAEvent { public string Key; public Action<int> Reply; }

        // ---------------- Consent (subscriber trong Test; publisher từ SDK ngoài) ----------------
        public struct ConsentUMPGranted : ISAEvent { public bool? Value; public Action<bool> Reply; }
        public struct ConsentATTGranted : ISAEvent { public bool? Value; public Action<bool> Reply; }

        // ---------------- Notification ----------------
        public struct NotificationSchedule : ISAEvent { public string Id; public string Title; public string Body; public DateTime TriggerTime; public float DelaySeconds; }
        public struct NotificationCancel : ISAEvent { public string Id; }
        public struct NotificationCancelAll : ISAEvent { }
    }
}
