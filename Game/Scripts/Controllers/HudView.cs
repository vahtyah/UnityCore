using UnityEngine;
using UnityEngine.UI;
using VahTyah;

/// <summary>
/// Wires Canvas_HUD buttons. The Settings button publishes <see cref="OpenSettingsRequest"/>, which
/// ModuleSettingsPopup listens for to open the Settings popup. The Reload button restarts the current
/// level — counted as a LOSE for analytics (player gave up mid-play).
/// </summary>
public class HudView : MonoBehaviour
{
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _reloadButton;

    private void Awake()
    {
        if (_settingsButton != null)
            _settingsButton.onClick.AddListener(() => EventBus.Publish(new OpenSettingsRequest()));

        if (_reloadButton != null)
            _reloadButton.onClick.AddListener(OnReloadClicked);
    }

    private void OnReloadClicked()
    {
        // Reload giữa chừng = bỏ cuộc -> tính THUA. Track TRƯỚC khi reload; LevelLoadRequest không publish
        // LevelCompleted nên _save.Level không +1 -> level của finish(lose) và lần chơi lại là cùng số.
        // if (Services.TryGet<AnalyticsService>(out var analytics))
        // {
        //     int level = 1;
        //     Events.Publish(new LevelGet { Reply = v => level = v });
        //     analytics.OnGameFinished(false, 0f, level); // completed=false, score=0f (chưa có hệ điểm)
        // }

        EventBus.Publish(new LevelLoadRequest());
    }
}
