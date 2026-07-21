using System;
using Cysharp.Threading.Tasks;
using VahTyah;

[Serializable]
public sealed class EventGameResultHandler : IGameResultHandler
{
    public void Handle(GameResultContext context)
    {
        if (context.Result == GameResult.Win)
            EventBus.Publish(new LevelCompleted { ShowScreen = true }).Forget();
        else if (context.Result == GameResult.Lose)
            EventBus.Publish(new LevelFailed { ShowScreen = true }).Forget();
    }
}
