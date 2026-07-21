public enum GameResult
{
    None,
    Win,
    Lose,
}

public readonly struct GameResultContext
{
    public readonly GameResult Result;
    public readonly string Reason;

    public GameResultContext(GameResult result, string reason)
    {
        Result = result;
        Reason = reason;
    }

    public override string ToString() => $"[GameResult] {Result} - {Reason}";
}