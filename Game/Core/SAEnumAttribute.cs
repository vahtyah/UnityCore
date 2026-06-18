using System;

namespace StandardAssets
{
    /// <summary>
    /// Đánh dấu một enum thuộc một "Category" (vd: "UIGroup"), để hệ thống
    /// gom các enum cùng loại lại — dùng kèm SAEnumFilter/SAEnumRef chọn enum động trong Inspector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Enum, Inherited = false)]
    public sealed class SAEnumAttribute : Attribute
    {
        public string Category { get; }

        public SAEnumAttribute(string category) => Category = category;
    }
}
