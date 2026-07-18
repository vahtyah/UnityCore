namespace VahTyah
{
    /// <summary>
    /// Danh mục profile animation "bay về counter" dùng chung (coin/heart/gem/booster...). TÙY CHỈNH theo game.
    /// Giữ int ổn định. Default = 0 → caller để mặc định là Default; cũng là fallback khi thiếu id.
    /// </summary>
    public enum CollectAnimId
    {
        Default = 0,
        Coin = 1,
        Heart = 2,
        BoosterPop = 3,
        // Thêm profile của game...
    }
}
