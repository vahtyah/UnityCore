using UnityEngine;

public interface IPanelAnimator
{
    public void Show();
    public void Hide();
}

public class PanelAnimator : MonoBehaviour, IPanelAnimator
{
    public virtual void Show() { }

    public virtual void Hide() { }
}
