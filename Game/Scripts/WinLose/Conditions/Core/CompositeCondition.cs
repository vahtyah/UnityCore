using System.Collections.Generic;

public enum ConditionOperator { And, Or }

public sealed class CompositeWinCondition : IWinCondition
{
    private readonly ConditionOperator   _op;
    private readonly List<IWinCondition> _conditions;

    public string Reason { get; }

    public CompositeWinCondition(ConditionOperator op, params IWinCondition[] conditions)
    {
        _op         = op;
        _conditions = new List<IWinCondition>(conditions);
        Reason      = BuildReason();
    }

    public bool Evaluate(in WinLoseContext ctx)
    {
        if (_op == ConditionOperator.And)
        {
            foreach (var c in _conditions)
                if (!c.Evaluate(in ctx)) return false;
            return true;
        }

        foreach (var c in _conditions)
            if (c.Evaluate(in ctx)) return true;
        return false;
    }

    private string BuildReason()
    {
        var op    = _op == ConditionOperator.And ? " AND " : " OR ";
        var parts = new System.Text.StringBuilder();
        for (int i = 0; i < _conditions.Count; i++)
        {
            if (i > 0) parts.Append(op);
            parts.Append('[').Append(_conditions[i].Reason).Append(']');
        }
        return parts.ToString();
    }
}

public sealed class CompositeLoseCondition : ILoseCondition
{
    private readonly ConditionOperator    _op;
    private readonly List<ILoseCondition> _conditions;

    public string Reason { get; }

    public CompositeLoseCondition(ConditionOperator op, params ILoseCondition[] conditions)
    {
        _op         = op;
        _conditions = new List<ILoseCondition>(conditions);
        Reason      = BuildReason();
    }

    public bool Evaluate(in WinLoseContext ctx)
    {
        if (_op == ConditionOperator.And)
        {
            foreach (var c in _conditions)
                if (!c.Evaluate(in ctx)) return false;
            return true;
        }

        foreach (var c in _conditions)
            if (c.Evaluate(in ctx)) return true;
        return false;
    }

    private string BuildReason()
    {
        var op    = _op == ConditionOperator.And ? " AND " : " OR ";
        var parts = new System.Text.StringBuilder();
        for (int i = 0; i < _conditions.Count; i++)
        {
            if (i > 0) parts.Append(op);
            parts.Append('[').Append(_conditions[i].Reason).Append(']');
        }
        return parts.ToString();
    }
}
