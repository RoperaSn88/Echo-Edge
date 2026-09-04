using EchoEdge.Domain.Phase;

namespace EchoEdge.Domain.Map
{
    /// <summary>
    /// ステージ構築用 CSV の 1 行目から読み取るクリア条件データ
    /// </summary>
    [System.Serializable]
    public class StageClearConditionData
    {
        /// <summary>
        /// クリア条件の種類
        /// </summary>
        public StageClearConditionType conditionType;

        /// <summary>
        /// クリア条件に必要な値（種類によっては未使用）
        /// </summary>
        public int conditionValue;
    }
}
