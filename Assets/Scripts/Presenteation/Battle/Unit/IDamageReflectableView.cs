using Cysharp.Threading.Tasks;

using EchoEdge.Domain.Battle;

namespace EchoEdge.Presenter.Battle
{
    /// <summary>
    /// ドメイン側（BaseUnit）で発火したダメージ計算の結果を反映できる View のインターフェース。
    /// View の OnTriggerEnter から発火する既存のダメージ処理とは経路を分け、
    /// 「計算済みの結果を見た目に反映するだけ」の責務を持つ。
    /// </summary>
    public interface IDamageReflectableView : IUnitView
    {
        /// <summary>
        /// 計算済みのダメージ結果を View に反映する。
        /// ダメージテキスト・HPゲージ・被弾／死亡アニメーションの再生までを行う。
        /// </summary>
        /// <param name="damage">実際に与えたダメージ量</param>
        /// <param name="isDeath">この結果で死亡したか</param>
        /// <param name="status">反映対象ユニットのステータス（HPゲージの割合算出に使う）</param>
        /// <param name="showEnergy">エナジー獲得演出を出すか。犠牲など報酬の対象外となる被弾では false を渡す</param>
        UniTask ReflectDamage(int damage, bool isDeath, BattleStatus status, bool showEnergy = true);

        /// <summary>
        /// 回復結果を View に反映する（回復量テキスト・HPゲージ）。
        /// </summary>
        /// <param name="amount">実際に回復した量</param>
        /// <param name="status">反映対象ユニットのステータス（HPゲージの割合算出に使う）</param>
        UniTask ReflectHeal(int amount, BattleStatus status);
    }
}
