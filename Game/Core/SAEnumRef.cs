using System;

namespace StandardAssets
{
    /// <summary>
    /// Tham chiếu tới một giá trị enum mà KHÔNG cố định kiểu lúc biên dịch:
    /// lưu tên kiểu (typeName) + giá trị (value), serialize được trong Inspector.
    /// Dùng để chọn "nhóm UI" hay enum tuỳ biến mà không hardcode kiểu cụ thể.
    /// </summary>
    [Serializable]
    public struct SAEnumRef
    {
        public string typeName;
        public int value;

        public bool IsSet => !string.IsNullOrEmpty(typeName);

        /// <summary>Dựng lại giá trị Enum thực từ typeName + value.</summary>
        public Enum GetEnum()
        {
            if (!IsSet) return null;
            Type type = Type.GetType(typeName);
            return type == null ? null : (Enum)Enum.ToObject(type, value);
        }

        public T GetEnum<T>() where T : Enum => (T)GetEnum();
    }
}
