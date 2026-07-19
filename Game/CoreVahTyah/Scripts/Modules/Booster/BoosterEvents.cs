using System;
using UnityEngine;

namespace VahTyah
{
    public struct BoosterActivate : IEvent { public string Key; public Action<bool> Reply; }

    public struct BoosterSetFree : IEvent { public string Key; }

    public struct BoosterGetState : IEvent { public string Key; public Action<BoosterState> Reply; }

    public struct BoosterActivated : IEvent { public string Key; }

    public struct BoosterChanged : IEvent { public string Key; }

    public struct BoosterInsufficient : IEvent { public string Key; }

    public struct BoosterShowBuyPanel : IEvent
    {
        public string Key;
        public string CurrencyKey;
        public int Cost;
        public int BuyAmount;
        public int AdAmount;
        public bool ShowAdButton;

        public string Title;
        public string Description;
        public Sprite Icon;
    }

    public struct AdShowRewarded : IEvent { public string Placement; public Action<bool> Reply; }

    public struct BoosterState
    {
        public bool Exists;
        public bool Locked;
        public int UnlockLevel;
        public bool Maxed;
        public bool Free;
        public int Price;
        public bool HasSeparateCost;
        public string CurrencyKey;
    }
}
