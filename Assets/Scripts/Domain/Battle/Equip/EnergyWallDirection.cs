namespace EchoEdge.Domain.Battle
{
    /// <summary>
    /// エナジーウォール（斜面の壁）の向き。真上から見たときの斜面の傾きを表す。
    /// </summary>
    public enum EnergyWallDirection
    {
        /// <summary>
        /// ／ 向き（マスの左下から右上へ伸びる斜面）
        /// </summary>
        Slash,

        /// <summary>
        /// ＼ 向き（マスの左上から右下へ伸びる斜面）
        /// </summary>
        BackSlash,
    }
}
