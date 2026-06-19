using Cysharp.Threading.Tasks;

/// <summary>
/// ユニット View の基本インターフェース。
/// BaseUnitView および飛行ユニット専用 View など、View 実装クラスが共通して持つメソッドを定義する。
/// </summary>
public interface IUnitView
{
    /// <summary>
    /// 攻撃アニメーション前にカメラをズームさせる
    /// </summary>
    UniTask WaitToCameraZoom();

    /// <summary>
    /// 攻撃アニメーションを実行する
    /// </summary>
    UniTask WaitAttackAnim();

    /// <summary>
    /// 特殊行動アニメーションを実行する
    /// </summary>
    UniTask WaitSpecificAnim();

    /// <summary>
    /// ユニットを指定座標へ移動させる
    /// </summary>
    UniTask Move(int y, int x);

    /// <summary>
    /// HPゲージの透明度を設定する
    /// </summary>
    /// <param name="value">透明度の値（0〜1）</param>
    /// <returns></returns>
    UniTask FadeGauge(float value);
}
