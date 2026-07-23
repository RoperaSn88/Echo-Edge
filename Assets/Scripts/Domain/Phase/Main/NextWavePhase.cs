using Cysharp.Threading.Tasks;
using UnityEngine;

public class NextWavePhase: IPhase
{
    public static NextWavePhase Instance { get; } = new NextWavePhase();
    
    public async UniTask<IPhase> WaitPhase()
    {
        // ビューを起動してね
        CameraManager.Instance.ActResetCameraTarget();
        
        await MapManager.Instance.BuildStageFromCsv();

        return PlayerPhase.Instance;
    }
}