using System.Collections.Generic;

public static class WinLoseChecker
{
    private static List<IWinCondition> winConditions;
    private static List<ILoseCondition> loseConditions;
    private static IGameResultHandler handler;
    private static bool gameEnded;

    public static void Initialize(IGameResultHandler resultHandler = null)
    {
        // winConditions = new List<IWinCondition>
        // {
        //     new AllBottlesSolved(),
        // };
        //
        // loseConditions = new List<ILoseCondition>
        // {
        //     new LiquidCapacityBlocked(),
        // };
        // handler = resultHandler ?? new SAGameResultHandler();
        // gameEnded = false;
    }

    public static void CheckWin()
    {
        if (gameEnded || handler == null || winConditions == null) return;

        WinLoseContext context = WinLoseContext.Capture();
        foreach (IWinCondition condition in winConditions)
        {
            if (!condition.Evaluate(in context)) continue;

            EndGame(GameResult.Win, condition.Reason);
            return;
        }
    }

    public static void CheckLose()
    {
        if (gameEnded || handler == null || loseConditions == null) return;

        WinLoseContext context = WinLoseContext.Capture();
        foreach (ILoseCondition condition in loseConditions)
        {
            if (!condition.Evaluate(in context)) continue;

            EndGame(GameResult.Lose, condition.Reason);
            return;
        }
    }

    private static void EndGame(GameResult result, string reason)
    {
        gameEnded = true;
        handler.Handle(new GameResultContext(result, reason));
    }

    public static void Dispose()
    {
        winConditions = null;
        loseConditions = null;
        handler = null;
        gameEnded = false;
    }
}