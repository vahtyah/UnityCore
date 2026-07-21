public interface IWinCondition
{
    bool Evaluate(in WinLoseContext context);
    string Reason { get; }
}

public interface ILoseCondition
{
    bool Evaluate(in WinLoseContext context);
    string Reason { get; }
}