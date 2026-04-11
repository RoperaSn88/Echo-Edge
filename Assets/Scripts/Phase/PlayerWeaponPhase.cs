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
        PlayerView.Instance.SkillAnim();
        await CameraManager.Instance.ActPlayerWeaponZoom(PlayerView.Instance.Transform.position);
        
        // 武器選びする
        await WeaponController.Instance.SelectWeapon();
        
        // キャンセル時の操作
        PlayerView.Instance.ResetRotateAnim();
        await CameraManager.Instance.ActResetCameraTarget();
        
        return PlayerPhase.Instance;
    }
}