using Cysharp.Threading.Tasks;
using UnityEngine;

using EchoEdge.App.Battle;
using EchoEdge.Domain.Phase;

namespace EchoEdge.Domain.Battle
{
    /// <summary>
    /// プッシャーの効果クラス
    /// 選択したマスにいる敵を、プッシャーの方向に1マスずらす
    /// </summary>
    public class Pusher : IEquipEffect
    {
        /// <summary>
        /// 敵を押し出す方向
        /// </summary>
        private readonly PusherDirection _direction;

        public Pusher(PusherDirection direction)
        {
            _direction = direction;
        }

        /// <summary>
        /// プッシャーの効果を発揮する
        /// </summary>
        public async UniTask Activate()
        {
            if (MapManager.Instance == null)
            {
                return;
            }

            Vector2Int targetFloorPos = PlayerEquipPhase.Instance.TargetFloorPos;
            int targetH = targetFloorPos.y;
            int targetW = targetFloorPos.x;
            if (!MapManager.Instance.IsInBounds(targetH, targetW))
            {
                return;
            }

            IUnit targetUnit = MapManager.Instance.GetUnitAt(targetH, targetW);
            if (targetUnit is not IEnemyUnit || !targetUnit.CanMove())
            {
                return;
            }

            // 2x2などの大きいユニットは左上のマスを基準に移動するため、基準座標を取得する
            foreach (var unitInfo in MapManager.Instance.GetUnitPositionsSnapshot())
            {
                if (!ReferenceEquals(unitInfo.unit, targetUnit))
                {
                    continue;
                }

                Vector2Int offset = GetOffset(_direction);
                // 移動先が範囲外や他ユニットで埋まっている場合は TryMoveUnitTo が false を返し、何も起きない
                await MapManager.Instance.TryMoveUnitTo(targetUnit, unitInfo.h + offset.y, unitInfo.w + offset.x);
                return;
            }
        }

        /// <summary>
        /// 方向に対応するマスのずらし量を返す（x: w方向, y: h方向）
        /// </summary>
        private static Vector2Int GetOffset(PusherDirection direction)
        {
            switch (direction)
            {
                case PusherDirection.Up: return new Vector2Int(0, 1);
                case PusherDirection.Down: return new Vector2Int(0, -1);
                case PusherDirection.Left: return new Vector2Int(-1, 0);
                case PusherDirection.Right: return new Vector2Int(1, 0);
                default: return Vector2Int.zero;
            }
        }
    }
}
