using System;
using Applicatiton.Battle.Phase;
using Cysharp.Threading.Tasks;

/// <summary>
/// 「敵を全滅させる」をクリア条件とするタスク。
/// EnemyDefeatedEvent を自ら購読し、進捗の更新から満了判定・GameClearManagerへの通知までを完結させる。
/// </summary>
public sealed class DefeatAllEnemiesStageClearTask : IStageClearTask
{
    public const StageClearConditionType ConditionType = StageClearConditionType.DefeatAllEnemies;

    private const string BaseMessage = "残りの敵はあと";
    private int _remainingEnemyCount;

    public string ObjectiveBaseText => BaseMessage;

    public string ObjectiveConditionValue => _remainingEnemyCount.ToString();

    public bool IsGameClearCondition => _remainingEnemyCount == 0;

    public void Initialize(int conditionValue)
    {
        _remainingEnemyCount = Math.Max(0, conditionValue);
        GameClearManager.UpdateText(ObjectiveBaseText, _remainingEnemyCount);
    }

    public void Subscribe()
    {
        DomainEventDispatcher.Register<EnemyDefeatedEvent>(OnEnemyDefeated);
    }

    public void Unsubscribe()
    {
        DomainEventDispatcher.Unregister<EnemyDefeatedEvent>(OnEnemyDefeated);
    }

    private void OnEnemyDefeated(EnemyDefeatedEvent e)
    {
        GameReward.UpdateLastEnemyPosition(e.Position.Height, e.Position.Width);
        GameReward.AddStageEarnedExperience(e.ExperienceReward);
        UpdateCondition();
    }

    private void UpdateCondition()
    {
        if (_remainingEnemyCount > 0)
        {
            _remainingEnemyCount--;
        }

        GameClearManager.UpdateText(ObjectiveBaseText, _remainingEnemyCount);

        if (!IsGameClearCondition) return;

        GameClearManager.SetStageClearCondition(true);

        if (!WaveManager.HasNextWave)
        {
            GameClearManager.StartGameClearSequenceAsync().Forget();
        }
    }
}
