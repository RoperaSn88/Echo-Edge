using Cysharp.Threading.Tasks;

using EchoEdge.App.Battle;
using EchoEdge.Presenter.UI;

namespace EchoEdge.Domain.Phase
{
    /// <summary>
    /// 回復を行うフェーズ。
    /// 最大HPの50%を回復してターンを終える。回復はステージ全体で2回まで。
    /// </summary>
    public class PlayerHealPhase : IPhase
    {
        /// <summary>
        /// 回復フェーズのインスタンス
        /// </summary>
        private static PlayerHealPhase _instance;

        /// <summary>
        /// 他のスクリプトから干渉するプロパティ
        /// </summary>
        public static PlayerHealPhase Instance => _instance ??= new PlayerHealPhase();

        /// <summary>
        /// 最大HPに対する回復量の割合
        /// </summary>
        private const float HealRate = 0.5f;

        public async UniTask<IPhase> WaitPhase()
        {
            // 回数が残っていない場合は行動を選び直す（通常はボタンが押せないため来ない）
            if (!BattleManager.TryConsumeHeal())
            {
                return PlayerPhase.Instance;
            }

            int healed = BattleManager.HealPlayer(HealRate);

            await MessagePresenter.Instance.AppearMessage($"HPが{healed}回復した");
            await MessagePresenter.Instance.DisappearMessage();

            return EnemyPhase.Instance;
        }
    }
}
