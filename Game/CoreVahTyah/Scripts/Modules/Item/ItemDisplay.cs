using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VahTyah.Inspector;

namespace VahTyah
{
    [RequireComponent(typeof(CounterFeedback))]
    public class ItemDisplay : MonoBehaviour
    {
        [BoxGroup("Item")]
        [Required("Item key chưa set → không đọc/nghe được giá trị nào.")]
        [Tooltip("Item key (khớp Key trong ModuleItem).")]
        [SerializeField] private string _itemKey;
        [BoxGroup("Item")]
        [Tooltip("Text hiện số lượng. Bỏ trống → không hiện số.")]
        [SerializeField] private TextMeshProUGUI _valueText;
        [BoxGroup("Item")]
        [Tooltip("Icon dùng làm đích cho coin-fly (TryFind theo key). Bỏ trống → không làm đích bay được.")]
        [SerializeField] private Image _iconImage;

        [BoxGroup("Feedback"), AutoRef]
        [SerializeField] private CounterFeedback _feedback;

        private static readonly List<ItemDisplay> _all = new List<ItemDisplay>();

        private int _lastValue;

        private void Awake()
        {
            if (_feedback == null) _feedback = GetComponent<CounterFeedback>();
        }

        private void OnEnable()
        {
            _all.Add(this);
            this.On<ItemChanged>(e => { if (e.Key == _itemKey) OnChanged(); });
            // Priority âm để chạy TRƯỚC ModuleItem.OnCollect (OnAsync waitFor=true, nếu không sẽ chạy sau cả quãng bay).
            this.On<ItemCollect>(e => { if (e.Key == _itemKey && e.Value > 0) _feedback.RaiseForCollect(); }, -100);
            InitValueAsync().Forget();
        }

        private void OnDisable() => _all.Remove(this);

        // Giá trị đầu đọc qua query ItemGet — nhưng nếu HUD OnEnable chạy TRƯỚC khi ModuleItem.Subscribe
        // (boot chưa xong) thì query chưa có listener → trả 0. Đợi tới khi ItemGet có listener rồi mới đọc.
        private async UniTaskVoid InitValueAsync()
        {
            if (!EventBus.HasListeners<ItemGet>())
                await UniTask.WaitUntil(
                    static () => EventBus.HasListeners<ItemGet>(),
                    cancellationToken: this.GetCancellationTokenOnDestroy());

            _lastValue = GetValue();
            Refresh();
        }

        private void OnChanged()
        {
            int val = GetValue();
            int prev = _lastValue;
            _lastValue = val;
            Refresh();
            _feedback.PlayChange(prev, val);
        }

        private void Refresh()
        {
            if (_valueText != null)
                _valueText.SetText("{0}", GetValue());
        }

        private int GetValue()
        {
            int result = 0;
            EventBus.Publish(new ItemGet { Key = _itemKey, Reply = v => result = v }).Forget();
            return result;
        }

        public static bool TryFind(string key, out Vector3 position)
        {
            foreach (var d in _all)
            {
                if (d._itemKey == key && d._iconImage != null)
                {
                    position = d._iconImage.transform.position;
                    return true;
                }
            }
            position = Vector3.zero;
            return false;
        }

        public static void RefreshAll()
        {
            foreach (var d in _all) d.Refresh();
        }
    }
}
