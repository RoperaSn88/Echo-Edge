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

            // ターゲットに近いユニットから1体ずつ、その時点の盤面で空いている最も近い場所へ移動させる。
            // 事前に全員分の移動先を決めると、2x2のユニット（『でかエナー』）と他のユニットが
            // 互いの現在地を移動先に取り合って、どちらも動けなくなることがあるため、
            // 移動済みのユニットを反映した盤面で毎回空き判定を行う（壁や他ユニットの占有マスは全て埋まっている扱い）。
            foreach (var unitInfo in targetUnits)
            {
                int size = (int)unitInfo.unit.GetSize();
                (int h, int w)? destination = null;
                foreach (var cell in candidateCells)
                {
                    // 2x2のユニットは候補マスを左上（基準マス）として、占有する全マスが空いているか見る
                    if (MapManager.Instance.IsFootprintFree(cell.h, cell.w, size, unitInfo.unit))
                    {
                        destination = cell;
                        break;
                    }
                }

                if (destination == null)
                {
                    continue;
                }

                if (destination.Value.h == unitInfo.h && destination.Value.w == unitInfo.w)
                {
                    continue;
                }

                var moved = await MapManager.Instance.TryMoveUnitTo(unitInfo.unit, destination.Value.h, destination.Value.w);
                if (moved && unitInfo.unit is IForcedMoveReceiver receiver)
                {
                    await receiver.OnPulledByMagneticCore();
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
