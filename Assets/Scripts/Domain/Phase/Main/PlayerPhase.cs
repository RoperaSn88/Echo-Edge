using System;
using Cysharp.Threading.Tasks;

using EchoEdge.App.Battle;
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

            if (PlayerPhaseController.Instance == null)
            {
                throw new InvalidOperationException("PlayerPhaseController がシーンに配置されていません");
            }

            // 行動選択ボタンが押されるまで待機する
            var action = await PlayerPhaseController.Instance.WaitSelectAction(BattleManager.HealRemaining);

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
    }
}
