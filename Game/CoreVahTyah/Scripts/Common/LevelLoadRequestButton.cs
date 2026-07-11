using System;
using UnityEngine;
using UnityEngine.UI;
using VahTyah;

[RequireComponent(typeof(Button))]
public class LevelLoadRequestButton : MonoBehaviour
{
    [SerializeField] private Button playAgainButton;

    private void Awake()
    {
        if(playAgainButton == null) playAgainButton = GetComponent<Button>();
        playAgainButton.onClick.AddListener(OnPlayAgainButtonClick);
    }

    private void OnPlayAgainButtonClick()
    {
        EventBus.Publish(new LevelLoadRequest());
    }

    private void OnValidate()
    {
        if(playAgainButton == null) playAgainButton = GetComponent<Button>();
    }
}