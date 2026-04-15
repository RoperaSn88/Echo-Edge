/// <summary>
/// ユニットの初期配置情報を保持するクラス
/// </summary>
[System.Serializable]
public class UnitPlacementData
{
    /// <summary>
    /// エネミーID（EnemyInfo.csv の ID 列と対応）
    /// </summary>
    public int enemyId;

    /// <summary>
    /// 配置箇所（縦）
    /// </summary>
    public int height;

    /// <summary>
    /// 配置箇所（横）
    /// </summary>
    public int width;
}
