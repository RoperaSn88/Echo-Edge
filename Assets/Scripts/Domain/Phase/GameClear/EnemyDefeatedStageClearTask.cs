/// <summary>
/// 敵撃破イベントを受け取り、ゲームクリア条件を更新するイベントハンドラー。
/// 「通常」(全滅)と「ボス」のクリア条件はどちらも敵の撃破によって進行するため、
/// このハンドラーが一括で受け取り GameClearManager 側で条件タイプごとに処理を振り分ける。
/// DomainEventDispatcher に購読登録することで、ドメイン層とアプリケーション層を疎結合に保つ。
/// </summary>
public static class EnemyDefeatedStageClearTask
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
        GameClearManager.NotifyEnemyDefeated(e.EnemyKind);
    }

    /// <summary>
    /// 後方互換ラッパー。View 層の既存呼び出しに対応する。
    /// 新規コードでは DomainEventDispatcher.Dispatch(new EnemyDefeatedEvent(...)) を使うこと。
    /// </summary>
    [System.Obsolete("DomainEventDispatcher.Dispatch(new EnemyDefeatedEvent(...)) を使ってください。")]
    public static void OnEnemyDead(int h, int w, int experience)
    {
        DomainEventDispatcher.Dispatch(new EnemyDefeatedEvent(new UnitPosition(h, w), experience));
    }
}
