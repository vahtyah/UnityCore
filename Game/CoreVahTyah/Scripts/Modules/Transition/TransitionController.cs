using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace VahTyah
{
    public class TransitionController : MonoBehaviour
    {
        private Animator _animator;
        private CanvasGroup _canvasGroup;
        private Image _image;
        private float _fadeDuration;
        private Color _baseColor;

        public void InitWithAnimator(Animator animator)
        {
            _animator = animator;
        }

        public void InitFallback(Color color, float duration)
        {
            _baseColor = color;
            _fadeDuration = duration;

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
            gameObject.AddComponent<CanvasScaler>();
            gameObject.AddComponent<GraphicRaycaster>();

            var imgObj = new GameObject("TransitionImage");
            imgObj.transform.SetParent(transform, false);

            var rt = imgObj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            _image = imgObj.AddComponent<Image>();
            _image.color = new Color(color.r, color.g, color.b, 1f);
            _image.raycastTarget = true;

            _canvasGroup = imgObj.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }

        public Task PlayAsync(bool cover, Sprite sprite)
        {
            var tcs = new TaskCompletionSource<bool>();

            if (_animator != null)
                StartCoroutine(AnimatorRoutine(cover, tcs));
            else
                StartCoroutine(FadeRoutine(cover, sprite, tcs));

            return tcs.Task;
        }

        private IEnumerator AnimatorRoutine(bool cover, TaskCompletionSource<bool> tcs)
        {
            _animator.SetBool("transition", cover);
            yield return null;

            while (_animator.IsInTransition(0))
                yield return null;

            while (_animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
                yield return null;

            tcs.SetResult(true);
        }

        private IEnumerator FadeRoutine(bool cover, Sprite sprite, TaskCompletionSource<bool> tcs)
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

            float from = cover ? 0f : 1f;
            float to = cover ? 1f : 0f;
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
            _canvasGroup.blocksRaycasts = cover;
            tcs.SetResult(true);
        }
    }
}
