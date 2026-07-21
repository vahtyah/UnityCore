using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VahTyah;

// Template minh hoạ pattern B: condition tự hỏi state qua EventBus (Reply), không nhận context.
// Ở đây hỏi LevelGet/LevelGetTries (ModuleLevel trả lời) nên chạy được luôn.
// Game thật: thay bằng query event của game (vd BoardStateGet) + cache ở service trả lời.

[Serializable]
public sealed class ExampleReachLevel : IWinCondition
{
    [SerializeField] private int level = 5;

    public string Reason => $"level >= {level}";

    public bool Evaluate()
    {
        int current = 0;
        EventBus.Publish(new LevelGet { Reply = v => current = v }).Forget();
        return current >= level;
    }
}

[Serializable]
public sealed class ExampleTooManyTries : ILoseCondition
{
    [SerializeField] private int maxTries = 3;

    public string Reason => $"tries >= {maxTries}";

    public bool Evaluate()
    {
        int tries = 0;
        EventBus.Publish(new LevelGetTries { Reply = v => tries = v }).Forget();
        return tries >= maxTries;
    }
}
