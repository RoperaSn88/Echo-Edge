/// <summary>
/// ターン終了イベントを受け取り、耐久型のクリア条件を更新するイベントハンドラー。
/// DomainEventDispatcher に購読登録することで、ドメイン層とアプリケーション層を疎結合に保つ。
/// </summary>
public static class EnduranceStageClearTask
{
    /// <summary>
    /// TurnEndedEvent のハンドラーを DomainEventDispatcher に登録する。
    /// ステージ開始時（StartPhase）に呼び出す。
    /// </summary>
    public static void Subscribe()
    {
        DomainEventDispatcher.Register<TurnEndedEvent>(OnTurnEnded);
    }

    /// <summary>
    /// TurnEndedEvent のハンドラーを DomainEventDispatcher から解除する。
    /// ステージ終了時に呼び出す。
    /// </summary>
    public static void Unsubscribe()
    {
        DomainEventDispatcher.Unregister<TurnEndedEvent>(OnTurnEnded);
    }

    private static void OnTurnEnded(TurnEndedEvent e)
    {
        GameClearManager.NotifyTurnEnded();
    }
}
