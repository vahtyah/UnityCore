using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VahTyah
{
    [CreateAssetMenu(menuName = "VahTyah/Modules/Transition", fileName = "Module_Transition")]
    public sealed class ModuleTransition : Module
    {
        [SerializeField] private GameObject _transitionPrefab;

        [Header("Fallback (không có prefab)")]
        [SerializeField] private float _fadeDuration = 0.3f;
        [Tooltip("Giữ màn đen bao lâu (giây) TRƯỚC khi vén (uncover). 0 = không giữ.")]
        [SerializeField] private float _holdDuration = 0f;
        [SerializeField] private Sprite _sprite;
        [SerializeField] private Color _fadeColor = Color.black;

        private TransitionController _controller;

        public override UniTask InitializeAsync(Transform holder)
        {
            var go = new GameObject("[Transition]");
            go.transform.SetParent(holder);

            _controller = go.AddComponent<TransitionController>();

            if (_transitionPrefab != null)
            {
                var instance = Object.Instantiate(_transitionPrefab);
                Object.DontDestroyOnLoad(instance);
                _controller.InitWithAnimator(instance.GetComponentInChildren<Animator>());
            }
            else
            {
                _controller.InitFallback(_fadeColor, _fadeDuration, _holdDuration);
            }

            return UniTask.CompletedTask;
        }

        public override void Subscribe()
        {
            EventBus.OnAsync<TransitionRequest>(OnTransition);
            
            EventBus.OnAsync<LevelLoadRequest>(
                _ => EventBus.Publish(new TransitionRequest { Cover = true }), -1000);

            EventBus.OnAsync<LevelLoadRequest>(
                _ => EventBus.Publish(new TransitionRequest { Cover = false }), 1000);
        }

        private UniTask OnTransition(TransitionRequest e)
        {
            return _controller.PlayAsync(e.Cover, _sprite);
        }
    }
}
