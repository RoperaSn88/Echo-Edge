using EchoEdge.Domain.Battle;

namespace EchoEdge.Domain.Map
{
    /// <summary>
    /// ステージ構築用 CSV の 1 行分データ
    /// </summary>
    [System.Serializable]
    public class StagePlacementData
    {
        /// <summary>
        /// 壁かユニットか
        /// </summary>
        public StageObjectKind objectKind;

        /// <summary>
        /// 配置箇所（縦）
        /// </summary>
        public int height;

        /// <summary>
        /// 配置箇所（横）
        /// </summary>
        public int width;

        /// <summary>
        /// ユニット種別（壁の場合は Invalid）
        /// </summary>
        public EnemyKinds enemyKind;

        /// <summary>
        /// レベル（ステージごとの難易度調整用。0 以下は等倍。壁の場合は未使用）
        /// </summary>
        public int level;
    }
}
