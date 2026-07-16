namespace VahTyah
{
    /// <summary>
    /// Danh mục "feel" nhấn cho UI bấm được (button / toggle / tile...). TÙY CHỈNH theo game.
    /// Giữ giá trị int ổn định. Default = 0 → component để mặc định (unset) là Default luôn.
    /// </summary>
    public enum InteractableStyleId
    {
        Default = 0,
        Button = 1,
        Toggle = 2,
        Icon = 3,
        // Thêm style của game...
    }
}
