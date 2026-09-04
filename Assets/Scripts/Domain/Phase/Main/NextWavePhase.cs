using Cysharp.Threading.Tasks;
using UnityEngine;

using EchoEdge.App.Battle;
using EchoEdge.Infra.Camera;
using EchoEdge.Presenter.UI;

namespace EchoEdge.Domain.Phase
{
    public class NextWavePhase: IPhase
    {
        private static NextWavePhase _instance;
        public static NextWavePhase Instance => _instance ??= new NextWavePhase();

        public async UniTask<IPhase> WaitPhase()
        {
            CameraManager.Instance.ActResetCameraTarget();

            await UniTask.WhenAll(
                NextWaveView.Instance.ShowNextWave(),
                MapManager.Instance.BuildStageFromCsv()
            );

            GameClearManager.SetStageClearCondition(false);

            await UniTask.Delay(1000);

            await NextWaveView.Instance.HideNextWave();

            return PlayerPhase.Instance;
        }
    }
}
