namespace EchoEdge.Domain.Battle
{
    /// <summary>
    /// エネミーがマップ上で占有するマスのサイズ。
    /// 値はそのまま「1辺あたりのマス数」として扱う（Default: 1x1, Large: 2x2）。
    /// キャラクター単位で設定できるよう、EnemyInfo.csv から読み込む想定。
    /// </summary>
    public enum EnemySize
    {
        /// <summary>1×1マスのキャラクター</summary>
        Default = 1,

        /// <summary>2×2マスのキャラクター（例: でかエナー）</summary>
        Large = 2,
    }
}
