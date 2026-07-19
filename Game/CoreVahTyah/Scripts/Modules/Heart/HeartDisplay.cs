using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VahTyah.Inspector;

namespace VahTyah
{
    /// <summary>
    /// Hiển thị số tim + timer (hồi tiếp / vô hạn) trên HUD. Nghe HeartChanged/HeartInfinityChanged để cập nhật;
    /// tự refresh timer mỗi giây vì đếm ngược đổi liên tục. Bump scale + nâng sorting uỷ cho CounterFeedback.
    /// Gắn lên cụm Heart, wire _countText + _timerText.
    /// </summary>
    [RequireComponent(typeof(CounterFeedback))]
    public class HeartDisplay : MonoBehaviour
    {
        [BoxGroup("Display")]
        [Tooltip("Số tim, vd \"3\". Bỏ trống → không hiện số.")]
        [SerializeField] private TextMeshProUGUI _countText;
        [BoxGroup("Display")]
        [Tooltip("Timer: \"2m 30s\" / \"Full\" / \"∞ 12m\". Bỏ trống → không hiện timer.")]
        [SerializeField] private TextMeshProUGUI _timerText;
        [BoxGroup("Display")]
        [Tooltip("Icon trái tim — đích cho tim bay tới (HeartCollect). Thiếu → HeartCollect cộng thẳng.")]
        [SerializeField] private Image _iconImage;

        [BoxGroup("Timer")]
        [Tooltip("Chu kỳ refresh timer (giây).")]
        [SerializeField] private float _refreshInterval = 1f;

        [BoxGroup("Feedback"), AutoRef]
        [Required("Chưa gán → PlayChange/RaiseForCollect gây NRE (RequireComponent đã đảm bảo có sẵn trên GameObject).")]
        [SerializeField] private CounterFeedback _feedback;

        private static readonly List<HeartDisplay> _all = new List<HeartDisplay>();

        private float _timer;
        private int _lastCount;

        private void Awake()
        {
            if (_feedback == null) _feedback = GetComponent<CounterFeedback>();
        }

        private void OnEnable()
        {
            _all.Add(this);
            this.On<HeartChanged>(_ => OnCountChanged());
            this.On<HeartInfinityChanged>(_ => ApplyDisplay(QueryInfinity()));
            // Priority âm để nâng sorting trước khi tim bắt đầu bay (ModuleHeart xử lý HeartCollect).
            this.On<HeartCollect>(e => { if (e.Value > 0) _feedback.RaiseForCollect(); }, -100);
            InitAsync().Forget();
        }

        private void OnDisable() => _all.Remove(this);

        /// <summary>Vị trí world của icon tim (đích bay cho HeartCollect). False nếu không có display/icon.</summary>
        public static bool TryFind(out Vector3 position)
        {
            foreach (var d in _all)
            {
                if (d._iconImage != null)
                {
                    position = d._iconImage.transform.position;
                    return true;
                }
            }
            position = Vector3.zero;
            return false;
        }

        // Đợi ModuleHeart Subscribe xong rồi mới đọc lần đầu — tránh HUD OnEnable chạy TRƯỚC khi boot xong
        // → query chưa có listener → hiển thị 0. Không bump ở lần đầu.
        private async UniTaskVoid InitAsync()
        {
            if (!EventBus.HasListeners<HeartGet>())
                await UniTask.WaitUntil(
                    static () => EventBus.HasListeners<HeartGet>(),
                    cancellationToken: this.GetCancellationTokenOnDestroy());

            _lastCount = GetCount();
            ApplyDisplay(QueryInfinity());
        }

        private void Update()
        {
            _timer += Time.unscaledDeltaTime;   // unscaled: timer chạy kể cả khi game pause (Time.timeScale=0)
            if (_timer >= _refreshInterval)
            {
                _timer = 0f;
                ApplyDisplay(QueryInfinity());   // tick mỗi giây: đếm ngược + bắt lúc infinity hết
            }
        }

        // HeartChanged: số tim đổi → render lại + bump nếu không infinity (infinity thì count hiện "∞", số không đổi trên màn).
        private void OnCountChanged()
        {
            int val = GetCount();
            int prev = _lastCount;
            _lastCount = val;

            bool infinity = QueryInfinity();
            ApplyDisplay(infinity);

            // Chỉ bump khi số THẬT SỰ đổi trên màn. Ngược lại (infinity, hoặc clamp ở max → val==prev) không bump,
            // nhưng vẫn phải Settle để hạ sorting nếu RaiseForCollect đã nâng lúc bắt đầu collect.
            if (!infinity && val != prev)
                _feedback.PlayChange(prev, val);
            else
                _feedback.Settle();
        }

        // Render count + timer theo trạng thái infinity. KHÔNG bump ở đây (bump chỉ ở OnCountChanged).
        private void ApplyDisplay(bool infinity)
        {
            if (_countText != null)
                _countText.SetText(infinity ? "∞" : GetCount().ToString());

            if (_timerText != null)
            {
                string text = string.Empty;
                if (infinity)
                    EventBus.Publish(new HeartGetInfinityTimer { Reply = v => text = v }).Forget();   // giờ THÔ, không ∞
                else
                    EventBus.Publish(new HeartGetTimer { Reply = v => text = v }).Forget();            // "Full" / đếm ngược
                _timerText.SetText(text);
            }
        }

        private bool QueryInfinity()
        {
            bool v = false;
            EventBus.Publish(new HeartIsInfinity { Reply = x => v = x }).Forget();
            return v;
        }

        private int GetCount()
        {
            int hearts = 0;
            EventBus.Publish(new HeartGet { Reply = v => hearts = v }).Forget();
            return hearts;
        }
    }
}
