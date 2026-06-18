using System.Threading.Tasks;
using UnityEngine;

namespace StandardAssets
{
    /// <summary>
    /// Lớp cơ sở cho mọi "module" của framework (Ads, IAP, Save, Sound, ...).
    /// Module là ScriptableObject -> cấu hình ngay trong project, kéo vào SAModuleConfig.
    /// Vòng đời do SAManager điều phối: InitializeAsync() rồi Subscribe().
    /// </summary>
    public abstract class SAModule : ScriptableObject
    {
        [Tooltip("Lower = runs first. Equal priority = parallel.")]
        public int Priority = 0;

        /// <summary>Khởi tạo bất đồng bộ (load SDK, kết nối dịch vụ...).</summary>
        public virtual Task InitializeAsync() => Task.CompletedTask;

        /// <summary>Đăng ký lắng nghe các event trên SATypedBus.</summary>
        public virtual void Subscribe() { }

        /// <summary>Huỷ đăng ký (nếu cần).</summary>
        public virtual void Unsubscribe() { }

        // Tiện ích lưu/đọc dữ liệu qua SATypedBus (module lưu không cần biết hệ thống save cụ thể).
        protected static void Save<T>(T value)
        {
            SATypedBus.Publish(new Ev.SaveDataSave { Key = typeof(T).Name, Value = value });
        }

        protected static T Load<T>() where T : class, new()
        {
            object result = null;
            SATypedBus.Publish(new Ev.SaveDataLoad { Key = typeof(T).Name, Type = typeof(T), Reply = v => result = v });
            return (result as T) ?? new T();
        }
    }
}
