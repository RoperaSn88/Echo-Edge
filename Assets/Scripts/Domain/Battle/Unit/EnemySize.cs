namespace EchoEdge.Domain.Battle
{
    /// <summary>
    /// エネミーの見た目の大きさ。
    /// 値はそのまま見た目の拡大率として扱う（Default: 等倍, Large: 2倍）。
    /// マス目の占有は大きさに関係なく 1x1（<see cref="EnemySizeExtensions.FootprintSpan"/>）。
    /// キャラクター単位で設定できるよう、EnemyInfo.csv から読み込む想定。
    /// </summary>
    public enum EnemySize
    {
        /// <summary>等倍のキャラクター</summary>
        Default = 1,

        /// <summary>見た目が2倍のキャラクター（例: でかエナー）。占有するマスは 1x1</summary>
        Large = 2,
    }

    public static class EnemySizeExtensions
    {
        /// <summary>
        /// マップ上で占有するマスの1辺あたりの数。
        /// 見た目の大きさに関係なく、すべてのエネミーが 1x1 マスを占有する。
        /// </summary>
        public static int FootprintSpan(this EnemySize size) => 1;
    }
}
