using System;
using UnityEngine;

// Handler cho context LevelEditor: chỉ log, KHÔNG chạy game flow (advance level, show screen).
// Editor publish qua WinLoseSetHandler để override _handler mặc định.
[Serializable]
public sealed class LevelEditorResultHandler : IGameResultHandler
{
    public void Handle(GameResultContext context) => Debug.Log($"[GameResult/Editor] {context}");
}
