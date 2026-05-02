using Cysharp.Threading.Tasks;

/// <summary>
/// 飛行アニメーションを持つ View のインターフェース
/// </summary>
public interface IFlyingUnitView
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
