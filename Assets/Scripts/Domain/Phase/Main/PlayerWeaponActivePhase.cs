using Cysharp.Threading.Tasks;

using EchoEdge.App.Battle;
using EchoEdge.Domain.Battle;
using EchoEdge.Infra.Camera;
using EchoEdge.Presenter.UI;

namespace EchoEdge.Domain.Phase
{
    /// <summary>
    /// 装備品の効果を発揮するフェーズ
    /// </summary>
    public class PlayerWeaponActivePhase : IPhase
    {
        /// <summary>
        /// 装備品効果発揮フェーズのインスタンス
        /// </summary>
        private static PlayerWeaponActivePhase _instance;

        /// <summary>
        /// 他のスクリプトから干渉するプロパティ
        /// </summary>
        public static PlayerWeaponActivePhase Instance => _instance ??= new PlayerWeaponActivePhase();

        public async UniTask<IPhase> WaitPhase()
        {
            // 床マス選択で作った効果を使う（エナジーウォールはプレビューの向きを持っている）
            IEquipEffect effect = PlayerEquipPhase.Instance.SelectedEffect
                ?? EquipEffectFactory.Create(WeaponController.Instance.SelectedWeaponIndex);

            // カメラをもとの位置に戻す
            await CameraManager.Instance.ActMoveCameraToDefault();

            // 選択された装備品の効果を発揮する
            var energyResult = EnergyManager.RemoveEnergy(WeaponController.TargetModel.WeaponCost);
            UIPresenter.Instance.PlayerStatusPresenter.SetEnergy(energyResult.gaugeValue, energyResult.energyCount);

            await effect.Activate();

            return PlayerPhase.Instance;
        }
    }
}
