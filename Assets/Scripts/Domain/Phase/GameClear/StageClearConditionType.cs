public enum StageClearConditionType
{
    /// <summary>
    /// 通常: 出現する敵を全て撃破するとクリア。
    /// </summary>
    DefeatAllEnemies,

    /// <summary>
    /// 耐久型: 開始からkターン経過するまでプレイヤーが生存していればクリア。
    /// </summary>
    Endurance,

    /// <summary>
    /// 特殊条件: 一閃による攻撃で1度にn体の敵を撃破するとクリア。
    /// </summary>
    IssenMultiKill,

    /// <summary>
    /// ボス: 特定の敵(ボス)を撃破するとクリア。
    /// </summary>
    Boss,
}
