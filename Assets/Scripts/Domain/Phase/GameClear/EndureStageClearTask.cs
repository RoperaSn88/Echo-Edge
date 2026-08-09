using Cysharp.Threading.Tasks;

namespace Domain.Phase.GameClear
{
    public class EndureStageClearTask: IStageClearTask
    {
        private const string BaseMessage = "クリアまであと";
        
        private int _remainingTurns;

        public string ObjectiveBaseText => BaseMessage;
        public string ObjectiveConditionValue => _remainingTurns.ToString();
        public bool IsGameClearCondition => _remainingTurns == 0;
        public void Initialize(int conditionValue)
        {
            _remainingTurns = conditionValue;
        }

        public void Subscribe()
        {
            DomainEventDispatcher.Register<TurnEndEvent>(OnTurnEnd);
        }

        public void Unsubscribe()
        {
            DomainEventDispatcher.Unregister<TurnEndEvent>(OnTurnEnd);
        }

        private void OnTurnEnd(TurnEndEvent e)
        {
            UpdateCondition();
        }
        
        private void UpdateCondition()
        {
            if (_remainingTurns > 0)
            {
                _remainingTurns--;
            }

            GameClearManager.UpdateText(BaseMessage, _remainingTurns);

            if (!IsGameClearCondition) return;

            GameClearManager.SetStageClearCondition(true);

            if (!WaveManager.HasNextWave)
            {
                GameClearManager.StartGameClearSequenceAsync().Forget();
            }
        }
    }
}