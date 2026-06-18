using System.Threading.Tasks;
using UnityEngine;

namespace VahTyah
{
    [CreateAssetMenu(menuName = "VahTyah/Modules/Transition", fileName = "Module_Transition")]
    public sealed class ModuleTransition : Module
    {
        [SerializeField] private GameObject _transitionPrefab;

        [Header("Fallback (không có prefab)")]
        [SerializeField] private float _fadeDuration = 0.3f;
        [SerializeField] private Sprite _sprite;
        [SerializeField] private Color _fadeColor = Color.black;

        private TransitionController _controller;

        public override Task InitializeAsync(Transform holder)
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
                _controller.InitFallback(_fadeColor, _fadeDuration);
            }

            return Task.CompletedTask;
        }

        public override void Subscribe()
        {
            EventBus.OnAsync<TransitionRequest>(OnTransition);

            // Trước load scene → che màn hình
            EventBus.OnAsync<SceneLoadRequest>(
                _ => EventBus.Publish(new TransitionRequest { Cover = true }), -1000);

            // Sau load scene → mở màn hình
            EventBus.OnAsync<SceneLoaded>(
                _ => EventBus.Publish(new TransitionRequest { Cover = false }), -50);
        }

        private Task OnTransition(TransitionRequest e)
        {
            return _controller.PlayAsync(e.Cover, _sprite);
        }
    }
}
