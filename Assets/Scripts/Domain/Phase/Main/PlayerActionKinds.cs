namespace EchoEdge.Domain.Phase
{
    /// <summary>
    /// プレイヤーフェーズで選択できる行動の種類
    /// </summary>
    public enum PlayerActionKinds
    {
        /// <summary>
        /// 一閃
        /// </summary>
        Attack,

        /// <summary>
        /// 装備品
        /// </summary>
        Weapon,

        /// <summary>
        /// 防御
        /// </summary>
        Defend,

        /// <summary>
        /// 回復
        /// </summary>
        Heal
    }
}
