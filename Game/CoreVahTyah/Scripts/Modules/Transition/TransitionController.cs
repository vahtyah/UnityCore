using System.Threading;
using Cysharp.Threading.Tasks;
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
        private float _holdDuration;
        private Color _baseColor;

        public void InitWithAnimator(Animator animator)
        {
            _animator = animator;
        }

        public void InitFallback(Color color, float duration, float holdDuration)
        {
            _baseColor = color;
            _fadeDuration = duration;
            _holdDuration = holdDuration;

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
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

        public UniTask PlayAsync(bool cover, Sprite sprite)
        {
            var ct = this.GetCancellationTokenOnDestroy();
            return _animator != null
                ? AnimatorRoutine(cover, ct)
                : FadeRoutine(cover, sprite, ct);
        }

        private async UniTask AnimatorRoutine(bool cover, CancellationToken ct)
        {
            _animator.SetBool("transition", cover);
            await UniTask.Yield(ct);

            await UniTask.WaitWhile(() => _animator.IsInTransition(0), cancellationToken: ct);
            await UniTask.WaitWhile(() => _animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f, cancellationToken: ct);
        }

        private async UniTask FadeRoutine(bool cover, Sprite sprite, CancellationToken ct)
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
                await UniTask.Yield(ct);
            }

            _canvasGroup.alpha = to;
            _canvasGroup.blocksRaycasts = cover;
            
            if (cover && _holdDuration > 0f)
                await UniTask.WaitForSeconds(_holdDuration, ignoreTimeScale: true, cancellationToken: ct); // giữ đen trước khi vén
        }
    }
}
