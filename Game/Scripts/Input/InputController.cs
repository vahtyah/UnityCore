using System;
using UnityEngine;
using UnityEngine.EventSystems;

public interface IClickable
{
    bool IsClickable { get; set; }
    bool CanClick();
    void OnClicked();
    void OnClickFailed();
}

public class InputController : MonoBehaviour
{
    private static bool isActive = true;

    [SerializeField] private LayerMask clickableLayerMask = ~0;

    private Camera mainCamera;
    private RaycastHit hit;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (!isActive) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            var ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, clickableLayerMask))
            {
                if (hit.collider.TryGetComponent(out IClickable clickableObject))
                {
                    if (!clickableObject.IsClickable) return;
                    
                    if (clickableObject.CanClick())
                    {
                        clickableObject.OnClicked();
                    }
                    else
                    {
                        clickableObject.OnClickFailed();
                    }
                }
            }
        }
    }

    public static void Disable()
    {
        isActive = false;
    }

    public static void Enable()
    {
        isActive = true;
    }
}