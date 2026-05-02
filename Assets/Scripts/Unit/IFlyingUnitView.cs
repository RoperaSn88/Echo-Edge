using Cysharp.Threading.Tasks;

/// <summary>
/// 飛行アニメーションを持つ View のインターフェース。
/// IUnitView を継承し、飛び上がり・ビーム攻撃のアニメーションを追加する。
/// </summary>
public interface IFlyingUnitView : IUnitView
{
    /// <summary>
    /// 飛び上がるアニメーションを実行する
    /// </summary>
    UniTask WaitFlyAnim();

    /// <summary>
    /// ビームエフェクトを表示し地上に戻るアニメーションを実行する
    /// </summary>
    UniTask WaitBeamAnim();
}
