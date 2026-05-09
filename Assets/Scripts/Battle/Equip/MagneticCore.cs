using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

        if (targetUnits.Count == 0)
        {
            return;
        }

        List<(int h, int w)> candidateCells = GetCandidateCells(targetH, targetW);
        if (candidateCells.Count == 0)
        {
            return;
        }

        HashSet<(int h, int w)> fixedOccupiedCells = MapManager.Instance.GetUnitPositionsSnapshot()
            .Where(unitInfo => !targetUnits.Any(target => target.unit == unitInfo.unit))
            .Select(unitInfo => (unitInfo.h, unitInfo.w))
            .ToHashSet();

        Dictionary<IUnit, (int h, int w)> movePlans = new();
        HashSet<(int h, int w)> reservedDestinations = new(fixedOccupiedCells);
        foreach (var unitInfo in targetUnits)
        {
            foreach (var cell in candidateCells)
            {
                if (reservedDestinations.Contains(cell))
                {
                    continue;
                }

                movePlans[unitInfo.unit] = cell;
                reservedDestinations.Add(cell);
                break;
            }
        }

        foreach (var unitInfo in targetUnits)
        {
            if (!movePlans.TryGetValue(unitInfo.unit, out var destination))
            {
                continue;
            }

            if (destination.h == unitInfo.h && destination.w == unitInfo.w)
            {
                continue;
            }

            await MapManager.Instance.TryMoveUnitTo(unitInfo.unit, destination.h, destination.w);
        }
    }

    private List<(int h, int w)> GetCandidateCells(int targetH, int targetW)
    {
        List<(int h, int w)> cells = new();
        HashSet<(int h, int w)> added = new();
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
