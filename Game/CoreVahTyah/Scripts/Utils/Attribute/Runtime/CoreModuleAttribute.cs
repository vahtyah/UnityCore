using System;

namespace VahTyah
{
    /// <summary>
    /// Đánh dấu Module là bắt buộc: tự thêm vào ModuleConfig khi mở editor, không cho xoá.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class CoreModuleAttribute : Attribute { }
}
