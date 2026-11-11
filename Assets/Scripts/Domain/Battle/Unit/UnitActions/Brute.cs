using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

using EchoEdge.App.Battle;
using EchoEdge.Infra.Camera;
using EchoEdge.Presenter.Player;
using EchoEdge.Presenter.UI;

namespace EchoEdge.Domain.Battle
{
    public class Brute: IUnitAction
    {
        private const float PlayerDamageRate = 1.0f;
        private const float SpecificRate = 0.3f;
        private const int BuffDurationTurns = 3;
        
        public async UniTask Attack()
        {
            await BattleManager.PlayerDamageWithText(PlayerDamageRate);

            await UniTask.Delay(TimeSpan.FromSeconds(1.0f));

            await CameraManager.Instance.ActResetCameraTarget();
        }

        public async UniTask BeforeAttack()
        {
            await MessagePresenter.Instance.AppearMessage("ブルートの攻撃");
        }

        public UniTask Dead()
        {
            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public UniTask<EnemyMoveKinds> Act(int selfHeight, int selfWidth)
        {
            // 通常攻撃は隣接時(width=0)のみ
            if (selfWidth == 0) return UniTask.FromResult(EnemyMoveKinds.Attack);
            return UniTask.FromResult(EnemyMoveKinds.None);
        }

        public UniTask Specific(int selfHeight, int selfWidth)
        {
            return UniTask.CompletedTask;
        }

        public UniTask BeforeSpecific()
        {
            return UniTask.CompletedTask;
        }

        public UniTask OnTurnStart()
        {
            return UniTask.CompletedTask;
        }

        public UniTask OnTurnEnd()
        {
            return UniTask.CompletedTask;
        }

        public UniTask Damage()
        {
            return UniTask.CompletedTask;
        }
    }
}
