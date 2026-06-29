/// <summary>
/// 敵ユニットが撃破されたときに発行されるドメインイベント。
/// </summary>
public sealed class EnemyDefeatedEvent : IDomainEvent
{
    /// <summary>撃破された敵の座標</summary>
    public UnitPosition Position { get; }

    /// <summary>撃破によって得られる経験値</summary>
    public int ExperienceReward { get; }

    public EnemyDefeatedEvent(UnitPosition position, int experienceReward)
    {
        Position = position;
        ExperienceReward = experienceReward;
    }
}
