using System;
using UnityEngine;

namespace StandardAssets
{
    /// <summary>
    /// Cơ sở cho các attribute "hiện/ẩn field trong Inspector theo điều kiện".
    /// Conditions thường là: tên field điều kiện + (các) giá trị để so sánh.
    /// (Phần vẽ Inspector nằm trong assembly Editor tương ứng.)
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public abstract class ConditionalFieldAttribute : PropertyAttribute
    {
        public readonly object[] Conditions;

        protected ConditionalFieldAttribute(object[] conditions) => Conditions = conditions;
    }

    /// <summary>Chỉ hiện field khi điều kiện ĐÚNG.</summary>
    public sealed class ShowIfAttribute : ConditionalFieldAttribute
    {
        public ShowIfAttribute(params object[] conditions) : base(conditions) { }
    }

    /// <summary>Ẩn field khi điều kiện ĐÚNG.</summary>
    public sealed class HideIfAttribute : ConditionalFieldAttribute
    {
        public HideIfAttribute(params object[] conditions) : base(conditions) { }
    }
}
