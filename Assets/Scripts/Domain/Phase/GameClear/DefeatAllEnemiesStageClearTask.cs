/// <summary>
/// 敵撃破イベントを受け取り、ゲームクリア条件を更新するイベントハンドラー。
/// DomainEventDispatcher に購読登録することで、ドメイン層とアプリケーション層を疎結合に保つ。
/// </summary>
public static class DefeatAllEnemiesStageClearTask
{
    /// <summary>
    /// EnemyDefeatedEvent のハンドラーを DomainEventDispatcher に登録する。
    /// ステージ開始時（StartPhase）に呼び出す。
    /// </summary>
    public static void Subscribe()
    {
        DomainEventDispatcher.Register<EnemyDefeatedEvent>(OnEnemyDefeated);
    }

    /// <summary>
    /// EnemyDefeatedEvent のハンドラーを DomainEventDispatcher から解除する。
    /// ステージ終了時に呼び出す。
    /// </summary>
    public static void Unsubscribe()
    {
        DomainEventDispatcher.Unregister<EnemyDefeatedEvent>(OnEnemyDefeated);
    }

    private static void OnEnemyDefeated(EnemyDefeatedEvent e)
    {
        GameClearManager.UpdateLastEnemyPosition(e.Position.Height, e.Position.Width);
        GameClearManager.AddStageEarnedExperience(e.ExperienceReward);
        GameClearManager.UpdateCondition();
    }

    /// <summary>
    /// 後方互換ラッパー。BaseUnitView など View 層の既存呼び出しに対応する。
    /// 新規コードでは DomainEventDispatcher.Dispatch(new EnemyDefeatedEvent(...)) を使うこと。
    /// </summary>
    [System.Obsolete("DomainEventDispatcher.Dispatch(new EnemyDefeatedEvent(...)) を使ってください。")]
    public static void OnEnemyDead(int h, int w, int experience)
    {
        DomainEventDispatcher.Dispatch(new EnemyDefeatedEvent(new UnitPosition(h, w), experience));
    }
}
