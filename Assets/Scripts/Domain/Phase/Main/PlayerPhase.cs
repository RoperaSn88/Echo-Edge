using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

using EchoEdge.App.Battle;
using EchoEdge.Presenter.Actions;
using EchoEdge.Presenter.UI;

namespace EchoEdge.Domain.Phase
{
    public class PlayerPhase: IPhase
    {
        /// <summary>
        /// プレイヤーフェーズのインスタンス
        /// </summary>
        private static PlayerPhase _instance;

        /// <summary>
        /// 他のスクリプトから干渉するプロパティ
        /// </summary>
        public static PlayerPhase Instance => _instance ??= new PlayerPhase();


        public async UniTask<IPhase> WaitPhase()
        {
            OperateInfos.Instance.SetOperateInfo(PhaseKinds.Player).Forget();

            // 前のターンに防御していた場合、このプレイヤーフェーズの反射回数を増やす
            BattleManager.ApplyPendingReflectBonus();

            PlayerActionKinds action;
            if (PlayerPhaseViewController.Instance == null)
            {
                // シーンにボタンが未配置の間も進行が止まらないよう、従来のクリック操作（左: 一閃、右: 装備品）で代用する
                Debug.LogWarning("PlayerPhaseController がシーンに配置されていないため、クリック操作で行動を選択します");
                action = await WaitClickAction();
            }
            else
            {
                // 行動選択ボタンが押されるまで待機する
                action = await PlayerPhaseViewController.Instance.WaitSelectAction(BattleManager.HealRemaining);
            }

            switch (action)
            {
                case PlayerActionKinds.Attack:
                    // 一閃の準備フェーズへ移行する
                    return PlayerAttackPreparationPhase.Instance;
                case PlayerActionKinds.Weapon:
                    // 装備品の選択フェーズへ移行する
                    return PlayerWeaponPhase.Instance;
                case PlayerActionKinds.Defend:
                    return PlayerDefendPhase.Instance;
                case PlayerActionKinds.Heal:
                    return PlayerHealPhase.Instance;
            }

            throw new InvalidOperationException($"不明な行動です: {action}");
        }

        /// <summary>
        /// PlayerPhaseController が無い場合の代替入力。左クリックで一閃、右クリックで装備品を選ぶ。
        /// </summary>
        private async UniTask<PlayerActionKinds> WaitClickAction()
        {
            var source = new UniTaskCompletionSource<PlayerActionKinds>();
            var playerActions = new PlayerActions();
            playerActions.PlayerPhase.Attack.started += _ => source.TrySetResult(PlayerActionKinds.Attack);
            playerActions.PlayerPhase.Skill.started += _ => source.TrySetResult(PlayerActionKinds.Weapon);
            playerActions.Enable();

            try
            {
                return await source.Task;
            }
            finally
            {
                playerActions.Dispose();
            }
        }
    }
}
