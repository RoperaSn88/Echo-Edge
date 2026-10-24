namespace EchoEdge.Domain.Battle
{
    /// <summary>
    /// 床マスを選んでいる間、カーソルの下に設置予定のプレビューを出す装備品のインターフェース。
    /// <see cref="EchoEdge.Domain.Phase.PlayerEquipPhase"/> が、カーソルの移動やホイール操作を受け渡す。
    /// </summary>
    public interface IEquipPlacementPreview
    {
        /// <summary>
        /// カーソルの下のマスが変わったときに呼ばれる。指定マスにプレビューを出す。
        /// </summary>
        /// <returns>そのマスに設置できるなら true</returns>
        public bool ShowPreviewAt(int h, int w);

        /// <summary>
        /// カーソルの下に床マスが無いときに呼ばれる。プレビューを隠す。
        /// </summary>
        public void HidePreview();

        /// <summary>
        /// ホイールが回されたときに呼ばれる
        /// </summary>
        /// <param name="scroll">ホイールの回転量（上が正）</param>
        public void OnScroll(float scroll);

        /// <summary>
        /// ホイールが押し込まれたときに呼ばれる
        /// </summary>
        public void OnPressWheel();

        /// <summary>
        /// 右クリックで床マスの選択をやめたときに呼ばれる。プレビューを片付ける。
        /// </summary>
        public void CancelPreview();
    }
}
