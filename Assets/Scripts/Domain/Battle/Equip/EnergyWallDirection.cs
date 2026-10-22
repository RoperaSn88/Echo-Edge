namespace EchoEdge.Domain.Battle
{
    /// <summary>
    /// エナジーウォール（三角柱の壁）の向き。真上から見たときに、斜面（三角形の斜辺）が向いている方向を表す。
    /// 上は奥（+Z）、右は +X。値の順に時計回りに 90° ずつ回る。
    /// </summary>
    public enum EnergyWallDirection
    {
        /// <summary>
        /// 斜面が右上を向く（直角の角はマスの左下）
        /// </summary>
        UpRight,

        /// <summary>
        /// 斜面が右下を向く（直角の角はマスの左上）
        /// </summary>
        DownRight,

        /// <summary>
        /// 斜面が左下を向く（直角の角はマスの右上）
        /// </summary>
        DownLeft,

        /// <summary>
        /// 斜面が左上を向く（直角の角はマスの右下）
        /// </summary>
        UpLeft,
    }
}
