using UnityEngine;

namespace VahTyah
{
    /// <summary>
    /// Xoay liên tục transform theo vận tốc góc cấu hình sẵn.
    /// </summary>
    public class RotateEffect : MonoBehaviour
    {
        [Tooltip("Độ/giây trên mỗi trục.")]
        [SerializeField] private Vector3 _angularVelocity = new Vector3(0f, 0f, -2f);

        [Tooltip("Self: xoay theo parent. World: xoay theo trục thế giới.")]
        [SerializeField] private Space _space = Space.Self;

        [SerializeField] private bool _useUnscaledTime;

        private void Update()
        {
            float dt = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            transform.Rotate(_angularVelocity * dt, _space);
        }
    }
}
