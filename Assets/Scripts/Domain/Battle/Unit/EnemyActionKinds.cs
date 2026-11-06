namespace EchoEdge.Domain.Battle
{
    /// <summary>
    /// ステージ CSV の行動リストで指定する、エネミーの1フェーズ分の行動。
    /// CSV には名前（Attack / Skill）で書く。
    /// </summary>
    public enum EnemyActionKinds
    {
        /// <summary>
        /// 攻撃（<see cref="EnemyMoveKinds.Attack"/>）
        /// </summary>
        Attack = 0,

        /// <summary>
        /// スキル（特殊行動。<see cref="EnemyMoveKinds.Specific"/>）
        /// </summary>
        Skill = 1,
    }
}
