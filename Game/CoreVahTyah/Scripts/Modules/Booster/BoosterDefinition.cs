using System;
using System.Collections.Generic;
using UnityEngine;

namespace VahTyah
{
    [Serializable]
    public class BoosterDefinition
    {
        [Tooltip("Booster item key (must match a Key in ModuleItem).")]
        public string Key;

        [Tooltip("Item spent on use (e.g. \"coin\"). Leave empty = spend the booster item itself (Key).")]
        public string CurrencyItem;

        [Tooltip("Price per use, in order. E.g. [100,120,150]. When Cap == false, the last value repeats forever.")]
        public List<int> Prices = new List<int>();

        [Tooltip("Cap uses per level play to the number of Prices entries. E.g. [1,1,1] = at most 3 uses/level.")]
        public bool Cap;

        [Min(1)]
        [Tooltip("Minimum level required to unlock (1-based). <= 1 = always unlocked.")]
        public int UnlockLevel = 1;

        [Header("When resources are insufficient")]
        [Tooltip("true = open Shop (UIGroupId.Shop). false = show the top-up buy panel (BoosterShowBuyPanel).")]
        public bool OpenShop;

        [Tooltip("Item spent when pressing Buy in the panel. Leave empty = use CurrencyItem.")]
        public string BuyPanelCurrency;

        [Tooltip("Fixed price charged when pressing Buy.")]
        public int BuyPanelPrice;

        [Tooltip("Number of boosters granted when buying with currency.")]
        public int BuyAmount = 1;

        [Tooltip("Show the Watch-Ad button in the panel (only when a ModuleAds subscribes to AdShowRewarded).")]
        public bool ShowAdButton = true;

        [Tooltip("Number of boosters granted when watching a rewarded ad.")]
        public int AdAmount = 1;

        [Header("Display (buy panel)")]
        public string Title;
        [TextArea] public string Description;
        public Sprite Icon;

        public string CurrencyKey => string.IsNullOrEmpty(CurrencyItem) ? Key : CurrencyItem;
        public string BuyPanelCurrencyKey => string.IsNullOrEmpty(BuyPanelCurrency) ? CurrencyKey : BuyPanelCurrency;
        public bool HasSeparateCost => CurrencyKey != Key;

        public bool IsLockedAtLevel(int level) => level < UnlockLevel;

        public int CalculatePrice(int usesThisLevel)
        {
            if (Prices == null || Prices.Count == 0) return 0;
            return Prices[Mathf.Min(usesThisLevel, Prices.Count - 1)];
        }
    }
}
