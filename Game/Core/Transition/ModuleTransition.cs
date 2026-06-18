using System.Threading.Tasks;
using UnityEngine;

namespace StandardAssets
{
    /// <summary>
    /// Module quản lý hiệu ứng chuyển cảnh (transition).
    /// Dùng Animator nếu có prefab, ngược lại fallback fade bằng CanvasGroup.
    /// </summary>
    [CreateAssetMenu(menuName = "SA/Modules/Transition", fileName = "Module_Transition", order = 22)]
    internal sealed class ModuleTransition : SAModule
    {
        [SerializeField]
        private GameObject _transitionPrefab;

        [Header("Fallback (no prefab)")]
        [SerializeField]
        private float _fadeDuration = 0.3f;

        [SerializeField]
        private Sprite _sprite;

        [SerializeField]
        private Color _fadeColor = Color.black;

        private TransitionController _controller;

        public ModuleTransition()
        {
            Priority = -1;
        }

        public override Task InitializeAsync()
        {
            // Tạo controller thường trú để điều khiển transition xuyên suốt các scene
            GameObject val = new GameObject("[SA] TransitionController");
            Object.DontDestroyOnLoad(val);
            _controller = val.AddComponent<TransitionController>();
            if (_transitionPrefab != null)
            {
                GameObject val2 = Object.Instantiate(_transitionPrefab);
                Object.DontDestroyOnLoad(val2);
                Animator componentInChildren = val2.GetComponentInChildren<Animator>();
                _controller.InitializeWithAnimator(componentInChildren);
            }
            else
            {
                _controller.InitializeFallback(_fadeColor, _fadeDuration);
            }
            return Task.CompletedTask;
        }

        public override void Subscribe()
        {
            SATypedBus.OnAsync<Ev.Transition>(OnTransition);
            // Trước khi load scene: che màn hình
            SATypedBus.OnAsync<Ev.SceneLoad>(_ => SATypedBus.Publish(new Ev.Transition { State = true }), -1000);
            // Sau khi load xong: mở màn hình
            SATypedBus.OnAsync<Ev.SceneLoaded>(_ => SATypedBus.Publish(new Ev.Transition { State = false }), -50);
        }

        private Task OnTransition(Ev.Transition e)
        {
            bool coverScreen = e.State;
            return _controller.PlayAsync(coverScreen, _sprite);
        }
    }
}
