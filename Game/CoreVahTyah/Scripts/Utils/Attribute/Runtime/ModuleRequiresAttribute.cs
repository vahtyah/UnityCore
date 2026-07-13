using System;

namespace VahTyah
{
    /// <summary>
    /// Khai báo phụ thuộc thứ tự boot: module gắn attribute này CẦN các module type liệt kê
    /// init TRƯỚC nó. ModuleConfig editor topo-sort mảng Modules theo các cạnh này → không cần
    /// sắp tay, và không thể sắp sai (Doctor cảnh báo nếu order vi phạm). Type truyền vào là kiểu
    /// Module (vd <c>typeof(ModuleSave)</c>); khớp cả subclass (IsAssignableFrom).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ModuleRequiresAttribute : Attribute
    {
        public Type[] Required { get; }

        public ModuleRequiresAttribute(params Type[] required)
            => Required = required ?? Array.Empty<Type>();
    }
}
