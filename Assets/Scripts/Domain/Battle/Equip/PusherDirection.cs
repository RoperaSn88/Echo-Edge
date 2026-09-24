namespace EchoEdge.Domain.Battle
{
    /// <summary>
    /// プッシャーが敵を押し出す方向
    /// </summary>
    public enum PusherDirection
    {
        /// <summary>画面上方向（h + 1）</summary>
        Up,

        /// <summary>画面下方向（h - 1）</summary>
        Down,

        /// <summary>画面左方向（w - 1）</summary>
        Left,

        /// <summary>画面右方向（w + 1）</summary>
        Right,
    }
}
