namespace VahTyah
{
    /// <summary>
    /// Danh mục profile animation cho item. TÙY CHỈNH theo game. Giữ giá trị int ổn định.
    /// Default = 0 → ItemDefinition để mặc định (unset) là Default luôn; cũng là fallback khi thiếu id.
    /// </summary>
    public enum ItemAnimationId
    {
        Default = 0,
        CoinFly = 1,
        BoosterPop = 2,
        // Thêm profile của game...
    }
}
