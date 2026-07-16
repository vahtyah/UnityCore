/// <summary>
/// Component diễn animation show/hide cho một panel (tự xử lý SetActive + interactable/raycast bên trong).
/// Chỉ là hành vi trình diễn — không giữ/đại diện dữ liệu. Cài bởi PopupAnimator, FadeAnimator.
/// </summary>
public interface IPanelAnimator
{
    public void Show();
    public void Hide();
}
