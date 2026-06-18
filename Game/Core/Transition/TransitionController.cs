using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace StandardAssets
{
    /// <summary>
    /// Component thường trú thực thi transition: chạy Animator hoặc fade CanvasGroup.
    /// </summary>
    public class TransitionController : MonoBehaviour
    {
        private Animator _animator;

        private CanvasGroup _canvasGroup;

        private Image _image;

        private float _fadeDuration;

        private Color _baseColor;

        public void InitializeWithAnimator(Animator animator)
        {
            _animator = animator;
        }

        public void InitializeFallback(Color color, float fadeDuration)
        {
            // Dựng Canvas + Image full màn để fade khi không có prefab/animator
            _baseColor = color;
            _fadeDuration = fadeDuration;
            Canvas val = gameObject.AddComponent<Canvas>();
            val.renderMode = RenderMode.ScreenSpaceOverlay;
            val.sortingOrder = 9999;
            gameObject.AddComponent<CanvasScaler>();
            gameObject.AddComponent<GraphicRaycaster>();
            GameObject val2 = new GameObject("TransitionImage");
            val2.transform.SetParent(transform, false);
            RectTransform val3 = val2.AddComponent<RectTransform>();
            val3.anchorMin = Vector2.zero;
            val3.anchorMax = Vector2.one;
            val3.sizeDelta = Vector2.zero;
            _image = val2.AddComponent<Image>();
            _image.color = new Color(color.r, color.g, color.b, 1f);
            _image.raycastTarget = true;
            _canvasGroup = val2.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }

        public Task PlayAsync(bool coverScreen, Sprite sprite)
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            if (_animator != null)
            {
                StartCoroutine(AnimatorCoroutine(coverScreen, tcs));
            }
            else
            {
                StartCoroutine(FadeCoroutine(coverScreen, sprite, tcs));
            }
            return tcs.Task;
        }

        private IEnumerator AnimatorCoroutine(bool coverScreen, TaskCompletionSource<bool> tcs)
        {
            _animator.SetBool("transition", coverScreen);
            yield return null;
            while (_animator.IsInTransition(0))
            {
                yield return null;
            }
            // Chờ state hiện tại chạy xong (normalizedTime >= 1)
            while (_animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
            {
                yield return null;
            }
            tcs.SetResult(true);
        }

        private IEnumerator FadeCoroutine(bool coverScreen, Sprite sprite, TaskCompletionSource<bool> tcs)
        {
            if (sprite != null)
            {
                _image.sprite = sprite;
                _image.color = Color.white;
            }
            else
            {
                _image.sprite = null;
                _image.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, 1f);
            }
            float from = coverScreen ? 0f : 1f;
            float to = coverScreen ? 1f : 0f;
            _canvasGroup.alpha = from;
            _canvasGroup.blocksRaycasts = true;
            float elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / _fadeDuration);
                yield return null;
            }
            _canvasGroup.alpha = to;
            _canvasGroup.blocksRaycasts = coverScreen;
            tcs.SetResult(true);
        }
    }
}
