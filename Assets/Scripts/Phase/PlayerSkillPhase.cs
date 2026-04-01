using System;
using Cysharp.Threading.Tasks;

public class PlayerSkillPhase : IPhase
{

    /// <summary>
    /// スキルフェーズのインスタンス
    /// </summary>
    private static PlayerSkillPhase _instance;

    /// <summary>
    /// 他のスクリプトから干渉するプロパティ
    /// </summary>
    public static PlayerSkillPhase Instance => _instance ??= new PlayerSkillPhase();
    public async UniTask<IPhase> WaitPhase()
    {
        PlayerView.Instance.SkillAnim();
        await CameraManager.Instance.ActPlayerWeaponZoom(PlayerView.Instance.Transform.position);
        await UniTask.Delay(TimeSpan.FromSeconds(5f));
        PlayerView.Instance.ResetRotateAnim();
        await CameraManager.Instance.ActResetCameraTarget();
        
        return PlayerPhase.Instance;
    }
}