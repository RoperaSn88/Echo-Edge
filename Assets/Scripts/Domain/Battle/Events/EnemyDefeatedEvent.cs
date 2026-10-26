namespace EchoEdge.Domain.Battle
{
    /// <summary>
    /// 敵ユニットが撃破されたときに発行されるドメインイベント。
    /// </summary>
    public struct EnemyDefeatedEvent : IDomainEvent
    {
        /// <summary>撃破された敵の座標</summary>
        public UnitPosition Position { get; }

        /// <summary>撃破によって得られる経験値</summary>
        public int ExperienceReward { get; }

        /// <summary>撃破された敵の種別。特定の敵の撃破をクリア条件にする場合に使う</summary>
        public EnemyKinds EnemyKind { get; }

        public EnemyDefeatedEvent(UnitPosition position, int experienceReward, EnemyKinds enemyKind = EnemyKinds.Invalid)
        {
            Position = position;
            ExperienceReward = experienceReward;
            EnemyKind = enemyKind;
        }
    }
}
