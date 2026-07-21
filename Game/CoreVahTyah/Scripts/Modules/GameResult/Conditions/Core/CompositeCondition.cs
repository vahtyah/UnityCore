using System;
using System.Collections.Generic;
using UnityEngine;

public enum ConditionOperator { And, Or }

[Serializable]
public sealed class CompositeWinCondition : IWinCondition
{
    [SerializeField] private ConditionOperator _op = ConditionOperator.And;

    [SerializeReference, SubclassSelector]
    private List<IWinCondition> _conditions = new List<IWinCondition>();

    public CompositeWinCondition() { }

    public CompositeWinCondition(ConditionOperator op, params IWinCondition[] conditions)
    {
        _op         = op;
        _conditions = new List<IWinCondition>(conditions);
    }

    public string Reason => BuildReason();

    public bool Evaluate()
    {
        if (_conditions == null || _conditions.Count == 0) return false;

        if (_op == ConditionOperator.And)
        {
            foreach (var c in _conditions)
                if (c == null || !c.Evaluate()) return false;
            return true;
        }

        foreach (var c in _conditions)
            if (c != null && c.Evaluate()) return true;
        return false;
    }

    private string BuildReason()
    {
        var op    = _op == ConditionOperator.And ? " AND " : " OR ";
        var parts = new System.Text.StringBuilder();
        for (int i = 0; i < _conditions.Count; i++)
        {
            if (i > 0) parts.Append(op);
            parts.Append('[').Append(_conditions[i]?.Reason).Append(']');
        }
        return parts.ToString();
    }
}

[Serializable]
public sealed class CompositeLoseCondition : ILoseCondition
{
    [SerializeField] private ConditionOperator _op = ConditionOperator.And;

    [SerializeReference, SubclassSelector]
    private List<ILoseCondition> _conditions = new List<ILoseCondition>();

    public CompositeLoseCondition() { }

    public CompositeLoseCondition(ConditionOperator op, params ILoseCondition[] conditions)
    {
        _op         = op;
        _conditions = new List<ILoseCondition>(conditions);
    }

    public string Reason => BuildReason();

    public bool Evaluate()
    {
        if (_conditions == null || _conditions.Count == 0) return false;

        if (_op == ConditionOperator.And)
        {
            foreach (var c in _conditions)
                if (c == null || !c.Evaluate()) return false;
            return true;
        }

        foreach (var c in _conditions)
            if (c != null && c.Evaluate()) return true;
        return false;
    }

    private string BuildReason()
    {
        var op    = _op == ConditionOperator.And ? " AND " : " OR ";
        var parts = new System.Text.StringBuilder();
        for (int i = 0; i < _conditions.Count; i++)
        {
            if (i > 0) parts.Append(op);
            parts.Append('[').Append(_conditions[i]?.Reason).Append(']');
        }
        return parts.ToString();
    }
}
