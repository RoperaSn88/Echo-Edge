using System;
using Cysharp.Threading.Tasks;
using UI.Weapon;

public class PlayerWeaponPhase : IPhase
{

    /// <summary>
    /// スキルフェーズのインスタンス
    /// </summary>
    private static PlayerWeaponPhase _instance;

    /// <summary>
    /// 他のスクリプトから干渉するプロパティ
    /// </summary>
    public static PlayerWeaponPhase Instance => _instance ??= new PlayerWeaponPhase();
    public async UniTask<IPhase> WaitPhase()
    {
        await CameraManager.Instance.ActPlayerWeaponZoom(PlayerView.Instance.Transform.position);
        
        // 武器選びする
        WeaponActionType result = await WeaponController.Instance.SelectWeapon();
        
        // キャンセル時の操作
        PlayerView.Instance.ResetRotateAnim();
        await CameraManager.Instance.ActResetCameraTargetWithRotate();

        // 武器が選択されたときは装備品使用フェーズへ遷移する
        if (result == WeaponActionType.Press)
        {
            return PlayerEquipPhase.Instance;
        }
        
        return PlayerPhase.Instance;
    }
}