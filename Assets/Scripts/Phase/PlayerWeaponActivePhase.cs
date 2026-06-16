using System;
using Cysharp.Threading.Tasks;
using UI.Weapon;

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
        // カメラをもとの位置に戻す
        await CameraManager.Instance.ActMoveCameraToDefault();

        // 選択された装備品の効果を発揮する
        var energyResult = EnergyManager.RemoveEnergy(WeaponController.TargetModel.WeaponCost);
        UIPresenter.Instance.PlayerStatusPresenter.SetEnergy(energyResult.gaugeValue, energyResult.energyCount);
        
        IEquipEffect effect = CreateEquipEffect(WeaponController.Instance.SelectedWeaponIndex);
        await effect.Activate();

        return PlayerPhase.Instance;
    }

    /// <summary>
    /// インデックスに対応する装備品エフェクトを生成する
    /// </summary>
    private IEquipEffect CreateEquipEffect(int index)
    {
        switch (index)
        {
            case 0: return new MagneticCore();
            case 1: return new EnergyWall();
            default: throw new InvalidOperationException($"不明な装備品インデックスです: {index}");
        }
    }
}
