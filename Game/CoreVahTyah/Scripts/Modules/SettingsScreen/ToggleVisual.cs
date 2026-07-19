using System;
using UnityEngine;
using UnityEngine.UI;
using VahTyah.Inspector;

public class ToggleVisual : MonoBehaviour
{
    [BoxGroup("Toggle Visual")]
    [Required("Toggle chưa gán → NRE ở Awake.")]
    [Tooltip("Toggle được lắng nghe onValueChanged.")]
    [SerializeField] Toggle toggle;

    [BoxGroup("Toggle Visual")]
    [Required("Slash chưa gán → NRE khi UpdateVisual.")]
    [Tooltip("Object hiện khi Toggle OFF (gạch chéo), ẩn khi ON.")]
    [SerializeField] GameObject slash;

    private void Awake()
    {
        toggle.onValueChanged.AddListener(UpdateVisual);
        UpdateVisual(toggle.isOn);
    }

    void UpdateVisual(bool isOn)
    {
        slash.SetActive(!isOn);
    }

    private void OnDestroy()
    {
        toggle.onValueChanged.RemoveAllListeners();
    }
}