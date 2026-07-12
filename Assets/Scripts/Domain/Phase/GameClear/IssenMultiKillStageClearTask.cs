/// <summary>
/// 一閃の多重撃破イベントを受け取り、特殊条件のクリア条件を更新するイベントハンドラー。
/// DomainEventDispatcher に購読登録することで、ドメイン層とアプリケーション層を疎結合に保つ。
/// </summary>
public static class IssenMultiKillStageClearTask
{
    /// <summary>
    /// IssenMultiKillEvent のハンドラーを DomainEventDispatcher に登録する。
    /// ステージ開始時（StartPhase）に呼び出す。
    /// </summary>
    public static void Subscribe()
    {
        DomainEventDispatcher.Register<IssenMultiKillEvent>(OnIssenMultiKill);
    }

    /// <summary>
    /// IssenMultiKillEvent のハンドラーを DomainEventDispatcher から解除する。
    /// ステージ終了時に呼び出す。
    /// </summary>
    public static void Unsubscribe()
    {
        DomainEventDispatcher.Unregister<IssenMultiKillEvent>(OnIssenMultiKill);
    }

    private static void OnIssenMultiKill(IssenMultiKillEvent e)
    {
        GameClearManager.NotifyIssenMultiKill(e.DefeatedCount);
    }
}
