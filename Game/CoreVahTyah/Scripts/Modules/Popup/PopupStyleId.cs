namespace VahTyah
{
    /// <summary>
    /// Danh mục "feel" popup. TÙY CHỈNH theo game. Giữ giá trị int ổn định.
    /// Default = 0 → component để mặc định (unset) là Default luôn.
    /// </summary>
    public enum PopupStyleId
    {
        Default = 0,
        Dialog = 1,
        Toast = 2,
        Snap = 3,      // pop-in như Default nhưng đóng tức thì (HasCloseAnimation = false)
        // Thêm style của game...
    }
}
