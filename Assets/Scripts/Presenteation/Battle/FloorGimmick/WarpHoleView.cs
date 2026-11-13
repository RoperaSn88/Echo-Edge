using UnityEngine;

namespace EchoEdge.Presenter.Battle
{
    /// <summary>
    /// 床ギミックのワープホール。一閃が入ると、同じペア番号のもう一方（<see cref="Partner"/>）へ移り、
    /// 入ったときの向きと速さのまま一閃を続ける。ペアで1ターンに1回だけ使える（使うと両方とも使用済みになる）。
    /// ワープは【反射回数】を消費せず、ダメージ計算の反射数にも数えない。
    /// </summary>
    public class WarpHoleView : FloorGimmickView
    {
        /// <summary>
        /// ペア番号（CSV の4列目）
        /// </summary>
        public int PairId { get; private set; }

        /// <summary>
        /// 対になるワープホール。同じペア番号がちょうど2つでないときは null（何も起きない）
        /// </summary>
        public WarpHoleView Partner { get; private set; }

        public void SetPair(int pairId, WarpHoleView partner)
        {
            PairId = pairId;
            Partner = partner;
        }

        /// <summary>
        /// 出口（対になるワープホールのマスの中心）の位置。高さは <paramref name="height"/> にそろえる
        /// </summary>
        public Vector3 GetExitPosition(float height)
        {
            var exit = Partner != null ? Partner.CellCenter : CellCenter;
            exit.y = height;
            return exit;
        }

        /// <summary>
        /// ペアを使用済みにして、<paramref name="target"/>（プレイヤー）を出口へ移す。移した先の位置を返す
        /// </summary>
        public Vector3 Warp(Transform target)
        {
            SetUsed(true);
            if (Partner != null) Partner.SetUsed(true);

            var exit = GetExitPosition(target.position.y);
            target.position = exit;
            return exit;
        }
    }
}
