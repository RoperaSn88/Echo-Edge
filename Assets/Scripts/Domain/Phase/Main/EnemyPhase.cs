using Cysharp.Threading.Tasks;
using UnityEngine;

using EchoEdge.App.Battle;
using EchoEdge.Domain.Battle;
using EchoEdge.Domain.UI;
using EchoEdge.Infra.Camera;
using EchoEdge.Infra.Tutorial;
using EchoEdge.Presenter.UI;

namespace EchoEdge.Domain.Phase
{
    public class EnemyPhase: IPhase
    {
        /// <summary>
        /// クリックされたか検知するブール
        /// </summary>
        [SerializeField]
        bool _clickFlug;

        /// <summary>
        /// エネミーフェーズのインスタンス
        /// </summary>
        private static EnemyPhase _instance;

        /// <summary>
        /// 他のスクリプトから干渉するプロパティ
        /// </summary>
        public static EnemyPhase Instance => _instance ??= new EnemyPhase();

        public async UniTask<IPhase> WaitPhase()
        {
            // 直前のプレイヤー攻撃でクリアが確定していた場合、敵ターンには入らない。
            // (フェーズループ側でも弾かれるが、演出中の余計な処理を避けるため入口でも確認する)
            if (GameClearManager.IsGameClearSequenceRunning)
            {
                return PlayerPhase.Instance;
            }

            _clickFlug = false;

            // プレイヤーのターンが終わったので、防御による反射回数の増加を元に戻す
            BattleManager.ClearReflectBonus();

            OperateInfos.Instance.SetOperateInfo(PhaseKinds.Enemy).Forget();
            await TurnChangeView.Instance.ShowTurnChange(TurnChangeKinds.EnemyTurn);
            
            if (TutorialSaveManager.TryBeginTutorial(TutorialKinds.Enemy))
            {
                try
                {
                    await TutorialActivator.Instance.StartTutorial(TutorialKinds.Enemy);
                }catch (System.Exception e)
                {
                    Debug.LogWarning($"チュートリアルを中止: {e}");
                }
            }
            
            EnemyPhaseStartActivator.ExecuteEnemyPhaseStartActions();
            BuildingManager.Instance?.ExecuteTurnStartActions();
            await MapManager.Instance.ExecuteTurnStartActions();
            await MapManager.Instance.MoveUnit();
            await MapManager.Instance.ExecuteTurnEndActions();
            await CameraManager.Instance.ActResetCameraTarget();

            // 耐久クリア条件などのターン経過ハンドラーを await する。
            // クリア成立ならクリア演出・シナリオ再生の完了までここで止まる。
            await DomainEventDispatcher.Dispatch(new TurnEndEvent());

            // 防御による被ダメージ半減はこのエネミーフェーズまで
            BattleManager.EndDefend();
            
            if (TutorialSaveManager.TryBeginTutorial(TutorialKinds.Final))
            {
                try
                {
                    await TutorialActivator.Instance.StartTutorial(TutorialKinds.Final);
                }catch (System.Exception e)
                {
                    Debug.LogWarning($"チュートリアルを中止: {e}");
                }
            }
            
            await TurnChangeView.Instance.ShowTurnChange(TurnChangeKinds.PlayerTurn);
            
            return PlayerPhase.Instance;
        }
    }
}
