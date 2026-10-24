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
        SlopeWall
    }
}
