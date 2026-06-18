using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StandardAssets
{
    /// <summary>
    /// Component UI hiển thị số lượng và icon của một item. Tự đăng ký vào danh sách
    /// tĩnh để các module khác tìm vị trí (target bay), tự cập nhật khi nhận message
    /// "Item.Add"/"Item.GetPending" và có thể phát animation phình/co khi giá trị đổi.
    /// </summary>
    public class ItemDisplay : MonoBehaviour
    {
        [SAEnumFilter("Item")]
        public SAEnumRef itemKey;

        [SerializeField]
        private TextMeshProUGUI _valueText;

        [SerializeField]
        private Image _iconImage;

        [Header("Change Animation")]
        [SerializeField]
        private bool _changeAnimation;

        [ShowIf("_changeAnimation")]
        [Tooltip("Transform to animate. Defaults to this GameObject's transform when left empty.")]
        [SerializeField]
        private Transform _animationTarget;

        [ShowIf("_changeAnimation")]
        [Tooltip("Scale curve played when the value increases (x = seconds, y = scale).")]
        [SerializeField]
        private AnimationCurve _onIncreaseAnimation = new AnimationCurve(new Keyframe[3]
        {
            new Keyframe(0f, 1f, 1f, 1f),
            new Keyframe(0.1f, 1.1f, 0f, 0f),
            new Keyframe(0.3f, 1f, -0.5f, -0.5f)
        });

        [ShowIf("_changeAnimation")]
        [Tooltip("Scale curve played when the value decreases (x = seconds, y = scale).")]
        [SerializeField]
        private AnimationCurve _onDecreaseAnimation = new AnimationCurve(new Keyframe[3]
        {
            new Keyframe(0f, 1f, -1f, -1f),
            new Keyframe(0.1f, 0.9f, 0f, 0f),
            new Keyframe(0.4f, 1f, 0.3333334f, 0.3333334f)
        });

        // Tất cả ItemDisplay đang active, dùng cho RefreshAll và TryFind.
        private static readonly List<ItemDisplay> _all = new List<ItemDisplay>();

        private Vector3 _originalScale;
        private int _lastKnownValue;
        private int _animGeneration;
        private Coroutine _activeCoroutine;

        private void Awake()
        {
            _originalScale = ((_animationTarget != null) ? _animationTarget : transform).localScale;
        }

        private void OnEnable()
        {
            _all.Add(this);
            _lastKnownValue = GetCurrentValue();
            UpdateDisplay();
            this.On<Ev.ItemAdd>(e => OnItemChanged(e.Key));
            this.On<Ev.ItemGetPending>(e => OnItemChanged(e.Key));
        }

        private void OnDisable()
        {
            _all.Remove(this);
            StopAnimation();
        }

        /// <summary>Làm mới hiển thị cho mọi ItemDisplay đang active.</summary>
        public static void RefreshAll()
        {
            foreach (ItemDisplay item in _all)
            {
                item.UpdateDisplay();
            }
        }

        private void OnItemChanged(string key)
        {
            // Bỏ qua nếu message không phải cho item key này.
            if (key != itemKey.GetEnum()?.ToString())
            {
                return;
            }
            int currentValue = GetCurrentValue();
            int previous = _lastKnownValue;
            _lastKnownValue = currentValue;
            UpdateDisplay();
            if (_changeAnimation && currentValue != previous)
            {
                PlayAnimation((currentValue > previous) ? _onIncreaseAnimation : _onDecreaseAnimation);
            }
        }

        private void UpdateDisplay()
        {
            UpdateValue();
            UpdateIcon();
        }

        private void UpdateValue()
        {
            if (_valueText == null)
            {
                return;
            }
            int value = 0;
            if (ModuleItem.saveDataItem != null && ModuleItem.saveDataItem.TryGet(itemKey.GetEnum()?.ToString(), out var entry))
            {
                value = entry.current;
            }
            _valueText.SetText("{0}", (float)value);
        }

        private void UpdateIcon()
        {
            if (_iconImage == null || ModuleItem.Instance == null)
            {
                return;
            }
            foreach (ModuleItem.ItemDefinition item in ModuleItem.Instance.items)
            {
                if (item.name != itemKey.GetEnum()?.ToString())
                {
                    continue;
                }
                _iconImage.sprite = item.icon;
                _iconImage.enabled = item.icon != null;
                break;
            }
        }

        private void PlayAnimation(AnimationCurve curve)
        {
            if (curve == null || curve.length == 0)
            {
                return;
            }
            Transform target = (_animationTarget != null) ? _animationTarget : transform;
            StopAnimation();
            _activeCoroutine = StartCoroutine(AnimateScale(target, curve, ++_animGeneration));
        }

        private IEnumerator AnimateScale(Transform target, AnimationCurve curve, int gen)
        {
            float duration = curve.keys[curve.length - 1].time;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                target.localScale = _originalScale * curve.Evaluate(elapsed);
                yield return null;
            }
            target.localScale = _originalScale * curve.Evaluate(duration);
            // Chỉ dọn nếu vẫn là thế hệ animation hiện tại (tránh ghi đè coroutine mới).
            if (_animGeneration == gen)
            {
                _activeCoroutine = null;
            }
        }

        private void StopAnimation()
        {
            if (_activeCoroutine != null)
            {
                StopCoroutine(_activeCoroutine);
                _activeCoroutine = null;
                _animGeneration++;
            }
        }

        /// <summary>Tìm vị trí thế giới của icon ứng với <paramref name="key"/> (dùng làm target bay).</summary>
        public static bool TryFind(string key, out Vector3 position)
        {
            foreach (ItemDisplay item in _all)
            {
                if (item.itemKey.GetEnum()?.ToString() != key || item._iconImage == null)
                {
                    continue;
                }
                position = item._iconImage.transform.position;
                return true;
            }
            position = Vector3.zero;
            return false;
        }

        private int GetCurrentValue()
        {
            if (ModuleItem.saveDataItem == null)
            {
                return 0;
            }
            return ModuleItem.saveDataItem.TryGet(itemKey.GetEnum()?.ToString(), out var entry) ? entry.current : 0;
        }
    }
}
