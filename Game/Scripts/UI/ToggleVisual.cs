using System;
using UnityEngine;
using UnityEngine.UI;

public class ToggleVisual : MonoBehaviour
{
    [SerializeField] Toggle toggle;
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