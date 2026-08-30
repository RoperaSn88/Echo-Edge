namespace Domain.Scenario.Controller
{
    /// <summary>
    /// シナリオの早送りモード。
    /// テキストは一括表示のため、早送りは「次の Step へ自動で進むまでの待機時間」を
    /// 段階的に短くすることで表現する。クリックすればいつでも即座に次へ進める。
    /// </summary>
    public enum FastForwardMode
    {
        /// <summary>早送りしない。クリックされるまで次の Step に進まない。</summary>
        Off = 0,

        /// <summary>速度1。クリックがなくても短い待機時間で次の Step に進む。</summary>
        Speed1 = 1,

        /// <summary>速度2。速度1よりさらに短い待機時間で次の Step に進む。</summary>
        Speed2 = 2,
    }
}
