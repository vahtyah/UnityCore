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
                _controller.InitFallback(_fadeColor, _fadeDuration);
            }

            return UniTask.CompletedTask;
        }

        private bool _bootReady;

        public override void Subscribe()
        {
            /*EventBus.OnAsync<TransitionRequest>(OnTransition);

            // Trong lúc boot, LoadingScreen đã che toàn màn — không cho Transition cover/uncover
            // để tránh fade chồng fade (nhấp nháy) khi load scene game đầu tiên. Chỉ bật sau boot.
            EventBus.On<BootCompleted>(_ => _bootReady = true);

            // Trước load scene → che màn hình (bỏ qua khi đang boot)
            EventBus.OnAsync<SceneLoadRequest>(
                _ => _bootReady ? EventBus.Publish(new TransitionRequest { Cover = true }) : UniTask.CompletedTask, -1000);

            // Sau load scene → mở màn hình (bỏ qua khi đang boot)
            EventBus.OnAsync<SceneLoaded>(
                _ => _bootReady ? EventBus.Publish(new TransitionRequest { Cover = false }) : UniTask.CompletedTask, -50);*/
        }

        private UniTask OnTransition(TransitionRequest e)
        {
            return _controller.PlayAsync(e.Cover, _sprite);
        }
    }
}
