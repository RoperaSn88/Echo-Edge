namespace EchoEdge.Presenter.Battle
{
    /// <summary>
    /// 床ギミックのリエイムパッド。一閃が触れるとパッドの中心で止まり、そこから一閃の向きを選び直せる。
    /// 残りの【反射回数】・コンボ・反射数ボーナス・分度器QTEの縮み具合はそのまま引き継ぐ。
    /// 同じパッドは1回の一閃で1回だけ使える（使うと半透明になり、次のプレイヤーフェーズで戻る）。
    /// </summary>
    public class ReAimPadView : FloorGimmickView
    {
    }
}
