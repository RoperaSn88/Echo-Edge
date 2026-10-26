using Cysharp.Threading.Tasks;
using UnityEngine;

using EchoEdge.App.Battle;
using EchoEdge.Domain.UI;
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

            // プレイヤーのターンが終わったので、防御による反射回数の増加を元に戻す
            BattleManager.ClearReflectBonus();

            await UniTask.WhenAll(
                NextWaveView.Instance.ShowNextWave(),
                MapManager.Instance.BuildStageFromCsv()
            );

            GameClearManager.SetStageClearCondition(false);

            // 通常ウェーブとボスウェーブが切り替わる場合はBGMを変える
            await BattleBgmController.ChangeWaveBgmAsync();

            await UniTask.Delay(1000);

            await NextWaveView.Instance.HideNextWave();
            
            await TurnChangeView.Instance.ShowTurnChange(TurnChangeKinds.PlayerTurn);

            return PlayerPhase.Instance;
        }
    }
}
