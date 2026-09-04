using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EchoEdge.Presenter.Player
{
    /// <summary>
    /// 爆発攻撃(めちゃくちゃ早い一閃)の実装。
    /// TODO: 爆発攻撃の仕様が決まり次第実装する。
    /// </summary>
    public class BombFlashAttackAction: IPlayerAttackAction
    {
        private static BombFlashAttackAction _instance;

        public static BombFlashAttackAction Instance => _instance ??= new BombFlashAttackAction();

        public UniTask ExecuteAsync(Vector3 targetPos)
        {
            throw new NotImplementedException("爆発攻撃(めちゃくちゃ早い一閃)は未実装です。");
        }

        public void OnTriggerEnter(Collider other)
        {
            // 未実装。爆発攻撃固有のダメージ判定は仕様確定後に実装する。
        }

        public void OnCollisionEnter(Collision collision)
        {
            throw new NotImplementedException();
        }
    }
}
