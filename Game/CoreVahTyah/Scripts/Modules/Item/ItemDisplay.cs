using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VahTyah
{
    public class ItemDisplay : MonoBehaviour
    {
        [SerializeField] private string _itemKey;
        [SerializeField] private TextMeshProUGUI _valueText;
        [SerializeField] private Image _iconImage;

        [Header("Change Animation")]
        [SerializeField] private bool _animateOnChange;
        [SerializeField] private AnimationCurve _increaseAnim = DefaultIncrease();
        [SerializeField] private AnimationCurve _decreaseAnim = DefaultDecrease();

        private static readonly List<ItemDisplay> _all = new List<ItemDisplay>();

        private int _lastValue;
        private Coroutine _animCoroutine;

        private void OnEnable()
        {
            _all.Add(this);
            _lastValue = GetValue();
            Refresh();

            this.On<ItemChanged>(e => { if (e.Key == _itemKey) OnChanged(); });
        }

        private void OnDisable()
        {
            _all.Remove(this);
            if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        }

        private void OnChanged()
        {
            int val = GetValue();
            int prev = _lastValue;
            _lastValue = val;
            Refresh();

            if (_animateOnChange && val != prev)
            {
                var curve = val > prev ? _increaseAnim : _decreaseAnim;
                if (_animCoroutine != null) StopCoroutine(_animCoroutine);
                _animCoroutine = StartCoroutine(ScaleRoutine(curve));
            }
        }

        private void Refresh()
        {
            if (_valueText != null)
                _valueText.SetText("{0}", GetValue());
        }

        private int GetValue()
        {
            int result = 0;
            EventBus.Publish(new ItemGet { Key = _itemKey, Reply = v => result = v });
            return result;
        }

        private IEnumerator ScaleRoutine(AnimationCurve curve)
        {
            if (curve == null || curve.length == 0) yield break;

            float duration = curve.keys[curve.length - 1].time;
            float elapsed = 0f;
            Vector3 original = transform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.localScale = original * curve.Evaluate(elapsed);
                yield return null;
            }

            transform.localScale = original;
            _animCoroutine = null;
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

        private static AnimationCurve DefaultIncrease() => new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(0.1f, 1.1f), new Keyframe(0.3f, 1f));

        private static AnimationCurve DefaultDecrease() => new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(0.1f, 0.9f), new Keyframe(0.4f, 1f));
    }
}
