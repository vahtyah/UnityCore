using UnityEngine;

namespace VahTyah
{
    public class HeartTicker : MonoBehaviour
    {
        private ModuleHeart _module;
        private float _timer;

        internal void Initialize(ModuleHeart module)
        {
            _module = module;
        }

        private void Update()
        {
            if (_module.IsFull() || _module.IsInfinity())
                return;

            _timer += Time.unscaledDeltaTime;
            if (_timer >= 1f)
            {
                _timer = 0f;
                _module.TryRegenerate();
            }
        }
    }
}
