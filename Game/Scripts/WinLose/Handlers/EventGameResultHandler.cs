
using VahTyah;
#if UNITY_EDITOR
using VahTyah.LevelEditor;
#endif

public sealed class EventGameResultHandler : IGameResultHandler
{
    public void Handle(GameResultContext context)
    {
#if UNITY_EDITOR
        Debug.Log(context.ToString());
        if (LevelEditorUtils.IsInScene("LevelEditor")) return;
#endif

        if (context.Result == GameResult.Win) EventBus.Publish(new LevelCompleted{ShowScreen = true});
        else if(context.Result == GameResult.Lose) EventBus.Publish(new LevelFailed{ShowScreen = true});
    }

}