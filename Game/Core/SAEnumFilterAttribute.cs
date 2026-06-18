using UnityEngine;

namespace StandardAssets
{
    /// <summary>
    /// Gắn lên field kiểu SAEnumRef để Inspector chỉ cho chọn các enum
    /// thuộc đúng Category chỉ định (vd: [SAEnumFilter("UIGroup")]).
    /// </summary>
    public sealed class SAEnumFilterAttribute : PropertyAttribute
    {
        public string Category { get; }

        public SAEnumFilterAttribute(string category) => Category = category;
    }
}
