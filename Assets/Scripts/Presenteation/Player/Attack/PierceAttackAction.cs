using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 貫通攻撃(通常)の実装。
/// TODO: 貫通攻撃の仕様が決まり次第実装する。
/// </summary>
public class PierceAttackAction: IPlayerAttackAction
{
    private static PierceAttackAction _instance;

    public static PierceAttackAction Instance => _instance ??= new PierceAttackAction();

    public UniTask ExecuteAsync(Vector3 targetPos)
    {
        throw new NotImplementedException("貫通攻撃(通常)は未実装です。");
    }

    public void OnTriggerEnter(Collider other)
    {
        // 未実装。貫通攻撃固有のダメージ判定は仕様確定後に実装する。
    }
}
