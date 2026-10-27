namespace EchoEdge.Infra.Audio
{
    /// <summary>
    /// SE/Battle フォルダの SE。値の名前がファイル名(拡張子なし)と一致する。
    /// </summary>
    public enum BattleSeType
    {
        Attack,
        EnemyTurn,
        Kill,
        NextWave,
        QTE,
        QTE_Bad,
        QTE_Good,
        QTE_Perfect,
        TurnChange,
        Damage_Bad,
        Damage_Good,
        Damage_Perfect,
    }
}
