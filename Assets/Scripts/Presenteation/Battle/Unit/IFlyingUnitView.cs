using Cysharp.Threading.Tasks;

namespace EchoEdge.Presenter.Battle
{
    /// <summary>
    /// 飛行アニメーションを持つ View のインターフェース。
    /// IUnitView を継承し、飛び上がり・ビーム攻撃のアニメーションを追加する。
    /// </summary>
    public interface IFlyingUnitView : IUnitView
    {
        /// <summary>
        /// 飛び上がるアニメーションを実行する
        /// </summary>
        public UniTask WaitFlyAnim();

        /// <summary>
        /// ビームエフェクトを表示し地上に戻るアニメーションを実行する
        /// </summary>
        public UniTask WaitAnimAfterFlying();

        /// <summary>
        /// ビームを撃たずに地上へ戻るアニメーションを実行する（飛行の強制解除時）
        /// </summary>
        public UniTask WaitLandAnim();

        /// <summary>
        /// 落下状態のアニメーションを解除する（飛行解除後の行動不可フェーズ経過時）
        /// </summary>
        public void EndFallAnim();
    }
}
