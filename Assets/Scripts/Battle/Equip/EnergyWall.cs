using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// エナジーウォールの効果クラス
/// </summary>
public class EnergyWall : IEquipEffect
{
    /// <summary>
    /// エナジーウォールの効果を発揮する
    /// </summary>
    public async UniTask Activate()
    {
        if (MapManager.Instance == null || BuildingManager.Instance == null)
        {
            return;
        }

        Vector2Int targetFloorPos = PlayerEquipPhase.Instance.TargetFloorPos;
        if (targetFloorPos.x == int.MinValue || targetFloorPos.y == int.MinValue)
        {
            return;
        }

        int wallHeight = targetFloorPos.y;
        int wallWidth = targetFloorPos.x;
        BuildingManager.Instance.TrySetEnergyWall(wallHeight, wallWidth);
        await UniTask.CompletedTask;
    }
}
