using UnityEngine;

namespace StandardAssets
{
    /// <summary>
    /// Xoay liên tục transform theo vận tốc góc cấu hình sẵn.
    /// </summary>
    public class RotateEffect : MonoBehaviour
    {
        [Tooltip("Degrees per second on each axis.")]
        [SerializeField]
        private Vector3 _angularVelocity = new Vector3(0f, 0f, -2f);

        [Tooltip("Local space rotates relative to parent; World space rotates in world axes.")]
        [SerializeField]
        private Space _space = Space.Self;

        private void Update()
        {
            transform.Rotate(_angularVelocity * Time.deltaTime, _space);
        }
    }
}
