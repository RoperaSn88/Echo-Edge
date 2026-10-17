namespace EchoEdge.Domain.Battle
{
    /// <summary>
    /// バトル中に敵ユニットが新たに出現したとき（『シーカー』の召喚など）に発行されるドメインイベント。
    /// 全滅クリア条件の残り敵数を増やすために使う。
    /// </summary>
    public struct EnemySpawnedEvent : IDomainEvent
    {
        /// <summary>出現した敵の座標</summary>
        public UnitPosition Position { get; }

        public EnemySpawnedEvent(UnitPosition position)
        {
            Position = position;
        }
    }
}
