/// <summary>
/// 一閃(1回の攻撃アクション)が終了したときに発行されるドメインイベント。
/// </summary>
public sealed class IssenMultiKillEvent : IDomainEvent
{
    /// <summary>この一閃で同時に撃破した敵の数</summary>
    public int DefeatedCount { get; }

    public IssenMultiKillEvent(int defeatedCount)
    {
        DefeatedCount = defeatedCount;
    }
}
