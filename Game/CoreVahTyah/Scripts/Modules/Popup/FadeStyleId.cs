namespace VahTyah
{
    /// <summary>
    /// Danh mục "feel" fade panel (<c>FadeAnimator</c>). TÙY CHỈNH theo game. Giữ giá trị int ổn định.
    /// Default = 0 → component để mặc định (unset) là Default luôn.
    /// </summary>
    public enum FadeStyleId
    {
        Default = 0,
        FadeInOnly = 1,   // fade vào nhưng đóng tức thì (FadeOut = false)
        // Thêm style của game...
    }
}
