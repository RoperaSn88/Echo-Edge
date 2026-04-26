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
        Debug.Log("エナジーウォール発動");
        await UniTask.CompletedTask;
    }
}
