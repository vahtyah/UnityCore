using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VahTyah.Inspector;

namespace VahTyah
{
    [CreateAssetMenu(menuName = "VahTyah/Modules/Game Result", fileName = "Module_GameResult")]
    public sealed class ModuleGameResult : Module
    {
        [BoxGroup("Win Conditions")]
        [SerializeReference, SubclassSelector]
        private List<IWinCondition> _winConditions = new List<IWinCondition>();

        [BoxGroup("Lose Conditions")]
        [SerializeReference, SubclassSelector]
        private List<ILoseCondition> _loseConditions = new List<ILoseCondition>();

        [BoxGroup("Result")]
        [SerializeReference, SubclassSelector]
        private IGameResultHandler _handler = new EventGameResultHandler();

        // Override runtime (vd LevelEditor). null → dùng _handler mặc định.
        private IGameResultHandler _override;
        private bool _gameEnded;

        public override void Subscribe()
        {
            EventBus.On<GameResultCheckWin>(_ => CheckWin());
            EventBus.On<GameResultCheckLose>(_ => CheckLose());
            EventBus.On<GameResultSetHandler>(e => _override = e.Handler);
            EventBus.On<LevelStarted>(_ => _gameEnded = false);
        }

        private void CheckWin()
        {
            if (_gameEnded || _winConditions == null) return;

            foreach (IWinCondition condition in _winConditions)
            {
                if (condition == null || !condition.Evaluate()) continue;

                EndGame(GameResult.Win, condition.Reason);
                return;
            }
        }

        private void CheckLose()
        {
            if (_gameEnded || _loseConditions == null) return;

            foreach (ILoseCondition condition in _loseConditions)
            {
                if (condition == null || !condition.Evaluate()) continue;

                EndGame(GameResult.Lose, condition.Reason);
                return;
            }
        }

        private void EndGame(GameResult result, string reason)
        {
            _gameEnded = true;
            (_override ?? _handler)?.Handle(new GameResultContext(result, reason));
        }
    }
}
