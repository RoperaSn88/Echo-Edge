using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using EchoEdge.App.Battle;
using EchoEdge.Domain.Phase;

namespace EchoEdge.Domain.Battle
{
    /// <summary>
    /// マグネティックコアの効果クラス
    /// </summary>
    public class MagneticCore : IEquipEffect
    {
        private const int EffectRadius = 3;

        /// <summary>
        /// マグネティックコアの効果を発揮する
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

            List<(IUnit unit, int h, int w)> targetUnits = MapManager.Instance.GetUnitPositionsSnapshot()
                .Where(unitInfo => unitInfo.unit.CanMove())
                .Where(unitInfo => Mathf.Abs(unitInfo.h - targetH) + Mathf.Abs(unitInfo.w - targetW) <= EffectRadius)
                .OrderBy(unitInfo => Mathf.Abs(unitInfo.h - targetH) + Mathf.Abs(unitInfo.w - targetW))
                .ToList();

            List<(int h, int w)> candidateCells = GetCandidateCells(targetH, targetW);
            if (candidateCells.Count == 0)
            {
                return;
            }

            // 吸引対象以外のユニット・壁がいるマスは固定で埋まっている扱いにして、
            // 再配置先に割り当てないようにする。
            // 2x2など複数マスを占有するユニット（『でかエナー』）もいるため、占有している全マスを見る。
            HashSet<IUnit> targetUnitSet = targetUnits.Select(unitInfo => unitInfo.unit).ToHashSet();
            HashSet<(int h, int w)> reservedDestinations = new();
            for (int h = 0; h < MapManager.Instance.Height; h++)
            {
                for (int w = 0; w < MapManager.Instance.Width; w++)
                {
                    IUnit occupant = MapManager.Instance.GetUnitAt(h, w);
                    if (occupant != null && !targetUnitSet.Contains(occupant))
                    {
                        reservedDestinations.Add((h, w));
                    }
                }
            }

            Dictionary<IUnit, (int h, int w)> movePlans = new();
            foreach (var unitInfo in targetUnits)
            {
                // 2x2のユニットは候補マスを左上（基準マス）として、占有する全マスが空いている場所を探す
                int size = (int)unitInfo.unit.GetSize();
                foreach (var cell in candidateCells)
                {
                    if (!CanReserveFootprint(cell.h, cell.w, size, reservedDestinations))
                    {
                        continue;
                    }

                    movePlans[unitInfo.unit] = cell;
                    ReserveFootprint(cell.h, cell.w, size, reservedDestinations);
                    break;
                }
            }

            // 移動先がまだ移動していない別の吸引対象と重なっていると TryMoveUnitTo が失敗するため、
            // 1体も動けなくなるまで残りのユニットの移動を繰り返す（2x2のユニットは他と重なりやすい）。
            List<(IUnit unit, int h, int w)> pendingUnits = targetUnits
                .Where(unitInfo => movePlans.TryGetValue(unitInfo.unit, out var destination)
                    && (destination.h != unitInfo.h || destination.w != unitInfo.w))
                .ToList();

            bool progressed = true;
            while (pendingUnits.Count > 0 && progressed)
            {
                progressed = false;
                foreach (var unitInfo in pendingUnits.ToList())
                {
                    var destination = movePlans[unitInfo.unit];
                    var moved = await MapManager.Instance.TryMoveUnitTo(unitInfo.unit, destination.h, destination.w);
                    if (!moved)
                    {
                        continue;
                    }

                    progressed = true;
                    pendingUnits.Remove(unitInfo);
                    if (unitInfo.unit is IForcedMoveReceiver receiver)
                    {
                        await receiver.OnPulledByMagneticCore();
                    }
                }
            }
        }

        /// <summary>
        /// anchorH/anchorW を左上とする size マス四方が、すべて盤面内かつ未予約か判定する
        /// </summary>
        private static bool CanReserveFootprint(int anchorH, int anchorW, int size, HashSet<(int h, int w)> reserved)
        {
            for (int dh = 0; dh < size; dh++)
            {
                for (int dw = 0; dw < size; dw++)
                {
                    int h = anchorH + dh;
                    int w = anchorW + dw;
                    if (!MapManager.Instance.IsInBounds(h, w) || reserved.Contains((h, w)))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// anchorH/anchorW を左上とする size マス四方を予約済みにする
        /// </summary>
        private static void ReserveFootprint(int anchorH, int anchorW, int size, HashSet<(int h, int w)> reserved)
        {
            for (int dh = 0; dh < size; dh++)
            {
                for (int dw = 0; dw < size; dw++)
                {
                    reserved.Add((anchorH + dh, anchorW + dw));
                }
            }
        }

        private List<(int h, int w)> GetCandidateCells(int targetH, int targetW)
        {
            List<(int h, int w)> cells = new();
            HashSet<(int h, int w)> added = new();
            // target マスからのマンハッタン距離を 1 ずつ広げて、
            // 近いマスから順番に候補を作る。
            int maxDistance = MapManager.Instance.Height + MapManager.Instance.Width - 2;
            int maxCellCount = (MapManager.Instance.Height * MapManager.Instance.Width) - 1;

            for (int distance = 1; distance <= maxDistance; distance++)
            {
                if (cells.Count >= maxCellCount)
                {
                    break;
                }

                for (int hOffset = -distance; hOffset <= distance; hOffset++)
                {
                    // |hOffset| + |wOffset| = distance を満たす2点を追加し、
                    // 1つの距離リング上のマスを列挙する。
                    int wOffsetMagnitude = distance - Mathf.Abs(hOffset);
                    AddCandidate(targetH + hOffset, targetW + wOffsetMagnitude, cells, added);
                    if (wOffsetMagnitude != 0)
                    {
                        AddCandidate(targetH + hOffset, targetW - wOffsetMagnitude, cells, added);
                    }
                }
            }

            return cells;
        }

        private void AddCandidate(int h, int w, List<(int h, int w)> cells, HashSet<(int h, int w)> added)
        {
            if (!MapManager.Instance.IsInBounds(h, w))
            {
                return;
            }

            var candidate = (h, w);
            if (added.Add(candidate))
            {
                cells.Add(candidate);
            }
        }
    }
}
