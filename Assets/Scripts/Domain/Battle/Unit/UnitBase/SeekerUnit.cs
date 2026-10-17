using EchoEdge.Presenter.Battle;

namespace EchoEdge.Domain.Battle
{
    /// <summary>
    /// 『シーカー』専用のユニット。
    /// ステージの奥地（盤面の外）に立ち、マスを占有しない。攻撃も移動もせず、ダメージも受けない。
    /// CSV で指定した座標は、エナーを召喚する基準のマスとして使う。
    /// </summary>
    public class SeekerUnit : BaseUnit, IOffBoardUnit
    {
        public SeekerUnit(int h, int w, EnemySize size = EnemySize.Default) : base(h, w, size)
        {
        }

        /// <inheritdoc/>
        protected override bool IsOffBoard => true;

        /// <inheritdoc/>
        public void Despawn()
        {
            if (View is BaseUnitView view)
            {
                UnitSpawner.Instance?.ReturnView(view);
            }
        }
    }
}
