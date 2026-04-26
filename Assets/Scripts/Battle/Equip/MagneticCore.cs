using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// マグネティックコアの効果クラス
/// </summary>
public class MagneticCore : IEquipEffect
{
    /// <summary>
    /// マグネティックコアの効果を発揮する
    /// </summary>
    public async UniTask Activate()
    {
        Debug.Log("マグネティックコア発動");
        await UniTask.CompletedTask;
    }
}
