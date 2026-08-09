using System;
using Applicatiton.Battle.Phase;
using Cysharp.Threading.Tasks;
using UI;

/// <summary>
/// 敵撃破イベントを受け取り、ゲームクリア条件を更新するイベントハンドラー。
/// DomainEventDispatcher に購読登録することで、ドメイン層とアプリケーション層を疎結合に保つ。
/// </summary>
public static class DefeatAllEnemiesStageClearTask
{
    public const StageClearConditionType ConditionType = StageClearConditionType.DefeatAllEnemies;
    
    private const string BaseMessage = "残りの敵はあと";
    private static int _remainingEnemyCount;

    public static string ObjectiveBaseText => BaseMessage;

    public static string ObjectiveConditionValue => _remainingEnemyCount.ToString();
    
    public static void Initialize(int enemyCount)
    {
        _remainingEnemyCount = Math.Max(0, enemyCount);
        GameClearManager.UpdateText(BaseMessage, _remainingEnemyCount);
    }
    
    public static void UpdateCondition()
    {
        if (_remainingEnemyCount > 0)
        {
            _remainingEnemyCount--;
        }
        
        GameClearManager.UpdateText(BaseMessage, _remainingEnemyCount);

        if (IsGameClearCondition && WaveManager.HasNextWave)
        {
            GameClearManager.SetStageClearCondition(true);
        }
        else if (IsGameClearCondition && !WaveManager.HasNextWave)
        {
            GameClearManager.StartGameClearSequenceAsync().Forget();
            GameClearManager.SetStageClearCondition(true);
        }
    }
    
    private static bool IsGameClearCondition => _remainingEnemyCount == 0;
    
    private static void OnEnemyDefeated(EnemyDefeatedEvent e)
    {
        GameReward.UpdateLastEnemyPosition(e.Position.Height, e.Position.Width);
        GameReward.AddStageEarnedExperience(e.ExperienceReward);
    }
}