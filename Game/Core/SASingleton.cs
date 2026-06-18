using UnityEngine;

namespace StandardAssets
{
    /// <summary>
    /// Singleton MonoBehaviour bền qua các scene (DontDestroyOnLoad).
    /// Bản trùng sẽ tự huỷ. Lớp con override OnInitialize() để chạy khởi tạo một lần.
    /// </summary>
    public abstract class SASingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this as T;
            DontDestroyOnLoad(gameObject);
            OnInitialize();
        }

        protected virtual void OnInitialize() { }
    }
}
