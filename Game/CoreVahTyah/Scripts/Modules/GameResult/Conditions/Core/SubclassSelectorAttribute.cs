using UnityEngine;

/// <summary>
/// Đặt lên field [SerializeReference] (hoặc List phần tử [SerializeReference]) để hiện
/// dropdown chọn concrete class implement interface/base đó ngay trong Inspector.
/// </summary>
public sealed class SubclassSelectorAttribute : PropertyAttribute
{
    public readonly bool IncludeNull;

    public SubclassSelectorAttribute(bool includeNull = true)
    {
        IncludeNull = includeNull;
    }
}
