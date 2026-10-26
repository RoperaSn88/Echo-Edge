using System;

namespace EchoEdge.Domain.Battle
{
    /// <summary>
    /// エネミーごとの特性。種別ごとの割り当ては <see cref="EnemyTraitsExtensions.GetTraits"/> で行う。
    /// </summary>
    [Flags]
    public enum EnemyTraits : byte
    {
        None = 0,

        /// <summary>
        /// 盤面の外に立ち、マスを占有しない（StageLayout.csv の座標が他の配置と重なってもよい）
        /// </summary>
        OffBoard = 1 << 0,

        /// <summary>
        /// 倒す対象ではない。全滅条件の敵数に数えない
        /// </summary>
        NotDefeatTarget = 1 << 1,
    }

    public static class EnemyTraitsExtensions
    {
        /// <summary>
        /// エネミー種別に割り当てた特性を返す
        /// </summary>
        public static EnemyTraits GetTraits(this EnemyKinds enemyKind)
        {
            switch (enemyKind)
            {
                case EnemyKinds.Skya:
                case EnemyKinds.Builder:
                case EnemyKinds.Booster:
                case EnemyKinds.Brute:
                case EnemyKinds.Enar:
                case EnemyKinds.BigEnar:
                    return EnemyTraits.None;

                case EnemyKinds.Seeker:
                    return EnemyTraits.OffBoard | EnemyTraits.NotDefeatTarget;

                default:
                    return EnemyTraits.None;
            }
        }

        /// <summary>
        /// 盤面の外に立つエネミーか
        /// </summary>
        public static bool IsOffBoard(this EnemyKinds enemyKind)
        {
            return (enemyKind.GetTraits() & EnemyTraits.OffBoard) != 0;
        }

        /// <summary>
        /// 全滅条件で倒す対象に数えるエネミーか
        /// </summary>
        public static bool IsDefeatTarget(this EnemyKinds enemyKind)
        {
            return (enemyKind.GetTraits() & EnemyTraits.NotDefeatTarget) == 0;
        }
    }
}
