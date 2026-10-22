namespace EchoEdge.Domain.Battle
{
    /// <summary>
    /// マップのグリッドに登録されず、盤面の外に立つユニット（『シーカー』など）。
    /// マスを占有せず、一閃の対象にもならず、全滅クリア条件の敵数にも数えない。
    /// ターンの行動（OnTurnStart / MoveTurn / OnTurnEnd）だけは MapManager から呼ばれる。
    /// </summary>
    public interface IOffBoardUnit : IUnit
    {
        /// <summary>
        /// マップのリセット時（ウェーブ切り替えなど）に呼ばれ、表示を片付ける。
        /// </summary>
        public void Despawn();
    }
}
