using Cysharp.Threading.Tasks;

using EchoEdge.App.Battle;
using EchoEdge.Presenter.UI;

namespace EchoEdge.Domain.Phase
{
    /// <summary>
    /// 防御を行うフェーズ。
    /// 次のエネミーフェーズの被ダメージ半減、エナジー+2、次のプレイヤーフェーズの反射回数+1を得てターンを終える。
    /// </summary>
    public class PlayerDefendPhase : IPhase
    {
        /// <summary>
        /// 防御フェーズのインスタンス
        /// </summary>
        private static PlayerDefendPhase _instance;

        /// <summary>
        /// 他のスクリプトから干渉するプロパティ
        /// </summary>
        public static PlayerDefendPhase Instance => _instance ??= new PlayerDefendPhase();

        /// <summary>
        /// 防御で得られるエナジーの個数（ゲージではなく所持数）
        /// </summary>
        private const int DefendEnergyCount = 2;

        public async UniTask<IPhase> WaitPhase()
        {
            BattleManager.StartDefend();

            var energyResult = EnergyManager.AddEnergyCount(DefendEnergyCount);
            PlayerStatusPresenter.Instance.SetEnergy(energyResult.gaugeValue, energyResult.energyCount);

            await MessagePresenter.Instance.AppearMessage("防御の構えをとった");
            await MessagePresenter.Instance.DisappearMessage();

            return EnemyPhase.Instance;
        }
    }
}
