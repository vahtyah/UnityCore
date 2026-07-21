public interface IWinCondition
{
    bool Evaluate();
    string Reason { get; }
}

public interface ILoseCondition
{
    bool Evaluate();
    string Reason { get; }
}
