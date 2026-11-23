namespace EchoEdge.Domain.Map
{
    public enum StageObjectKind : byte
    {
        Invalid,
        Wall,
        Unit,

        /// <summary>
        /// 斜面の壁（三角柱）。CSV では「SlopeWall, 縦, 横, 角度」で、角度は 45 / 135 / -135 / -45 のいずれか
        /// </summary>
        SlopeWall,

        /// <summary>
        /// 床ギミックのワープホール。CSV では「WarpHole, 縦, 横, ペア番号」で、同じペア番号の2つが対になる
        /// </summary>
        WarpHole,

        /// <summary>
        /// 床ギミックのリエイムパッド。CSV では「ReAimPad, 縦, 横」。一閃が触れると、そこから向きを選び直せる
        /// </summary>
        ReAimPad,
    }

    public static class StageObjectKindExtensions
    {
        /// <summary>
        /// 床ギミック（マスは占有せず、敵は通過できる）かどうか
        /// </summary>
        public static bool IsFloorGimmick(this StageObjectKind kind)
        {
            return kind is StageObjectKind.WarpHole or StageObjectKind.ReAimPad;
        }
    }
}
