using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VahTyah.Inspector;

namespace VahTyah
{
    public class BoosterBuyPanel : MonoBehaviour
    {
        [BoxGroup("Animator"), AutoRef] [SerializeField]
        private PanelAnimator animator;

        [BoxGroup("Button")] [SerializeField] private Button buyButton;
        [BoxGroup("Button")] [SerializeField] private Button adButton;
        [BoxGroup("Button")] [SerializeField] private Button[] closeButtons;

        [BoxGroup("Content")] [SerializeField] private TextMeshProUGUI title;
        [BoxGroup("Content")] [SerializeField] private TextMeshProUGUI description;
        [BoxGroup("Content")] [SerializeField] private Image icon;
        [BoxGroup("Content")] [SerializeField] private TextMeshProUGUI amountLabel;
        [BoxGroup("Content")] [SerializeField] private TextMeshProUGUI costLabel;
        [BoxGroup("Content")] [SerializeField] private TextMeshProUGUI adAmountLabel;
        [BoxGroup("Content")] [SerializeField] private GameObject adButtonRoot;

        [BoxGroup("Options")] [SerializeField]
        private Transform _flyFrom;

        [BoxGroup("Options")] [SerializeField] private bool _prefixX = true;

        private string _boosterKey;
        private string _currencyKey;
        private int _cost;
        private int _buyAmount;
        private int _adAmount;

        private void Awake()
        {
            this.On<BoosterShowBuyPanel>(OnShow);
            RegisterButtons();
        }

        private void RegisterButtons()
        {
            if (buyButton != null) buyButton.onClick.AddListener(Buy);
            if (adButton != null) adButton.onClick.AddListener(WatchAd);
            if(closeButtons != null && closeButtons.Length > 0)
            {
                foreach (var closeButton in closeButtons)
                {
                    closeButton.onClick.AddListener(Hide);
                }
            }
        }

        private void UnregisterButtons()
        {
            if (buyButton != null) buyButton.onClick.RemoveListener(Buy);
            if (adButton != null) adButton.onClick.RemoveListener(WatchAd);
            if(closeButtons != null && closeButtons.Length > 0)
            {
                foreach (var closeButton in closeButtons)
                {
                    closeButton.onClick.RemoveListener(Hide);
                }
            }
        }

        private void OnShow(BoosterShowBuyPanel e)
        {
            _boosterKey = e.Key;
            _currencyKey = e.CurrencyKey;
            _cost = e.Cost;
            _buyAmount = e.BuyAmount;
            _adAmount = e.AdAmount;

            if (title != null) title.SetText(e.Title ?? string.Empty);
            if (description != null) description.SetText(e.Description ?? string.Empty);
            if (icon != null)
            {
                icon.sprite = e.Icon;
                icon.enabled = e.Icon != null;
            }

            if (amountLabel != null) amountLabel.SetText(Format(e.BuyAmount));
            if (costLabel != null) costLabel.SetText("{0}", e.Cost);

            if (adButtonRoot != null) adButtonRoot.SetActive(e.ShowAdButton);
            if (adAmountLabel != null)
            {
                adAmountLabel.gameObject.SetActive(e.ShowAdButton);
                if (e.ShowAdButton) adAmountLabel.SetText(Format(e.AdAmount));
            }

            Show();
        }

        public void Buy()
        {
            if (string.IsNullOrEmpty(_boosterKey)) return;

            bool ok = false;
            EventBus.Publish(new ItemTrySpend { Key = _currencyKey, Value = _cost, Reply = r => ok = r }).Forget();
            if (!ok) return;

            EventBus.Publish(new ItemCollect { Key = _boosterKey, Value = _buyAmount, From = _flyFrom }).Forget();
            EventBus.Publish(new BoosterChanged { Key = _boosterKey }).Forget();
            Hide();
        }

        public void WatchAd()
        {
            if (string.IsNullOrEmpty(_boosterKey)) return;

            EventBus.Publish(new AdShowRewarded
            {
                Placement = "booster",
                Reply = success =>
                {
                    if (!success) return;
                    EventBus.Publish(new ItemCollect { Key = _boosterKey, Value = _adAmount, From = _flyFrom })
                        .Forget();
                    EventBus.Publish(new BoosterChanged { Key = _boosterKey }).Forget();
                    Hide();
                }
            }).Forget();
        }

        private void Show()
        {
            if (animator != null) animator.Show();
            else gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (animator != null) animator.Hide();
            else gameObject.SetActive(false);
        }

        private string Format(int n) => _prefixX ? $"x{n}" : $"{n}x";

        private void OnDestroy()
        {
            UnregisterButtons();
        }
    }
}
