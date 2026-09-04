namespace EchoEdge.Domain.Scenario
{
    /// <summary>
    /// <see cref="BgmEvent"/> が行う操作の種類。
    /// </summary>
    public enum BgmEventAction
    {
        /// <summary>BGM を再生する。</summary>
        Play,

        /// <summary>再生中の BGM を停止する。</summary>
        Stop
    }
}
