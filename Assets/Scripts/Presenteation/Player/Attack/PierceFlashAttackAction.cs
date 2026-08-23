using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 貫通攻撃(めちゃくちゃ早い一閃)の実装。
/// TODO: 貫通攻撃の仕様が決まり次第実装する。
/// </summary>
public class PierceFlashAttackAction: IPlayerAttackAction
{
    private static PierceFlashAttackAction _instance;

    public static PierceFlashAttackAction Instance => _instance ??= new PierceFlashAttackAction();

    public UniTask ExecuteAsync(Vector3 targetPos)
    {
        throw new NotImplementedException("貫通攻撃(めちゃくちゃ早い一閃)は未実装です。");
    }

    public void OnTriggerEnter(Collider other)
    {
        // 未実装。貫通攻撃固有のダメージ判定は仕様確定後に実装する。
    }
}
