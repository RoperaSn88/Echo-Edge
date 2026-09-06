using Cysharp.Threading.Tasks;
using UnityEngine;

using EchoEdge.App.Battle;
using EchoEdge.Domain.Battle;
using EchoEdge.Domain.UI;
using EchoEdge.Infra.Camera;
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
            await TurnChangeView.Instance.ShowTurnChange(TurnChangeKinds.EnemyTurn);
            EnemyPhaseStartActivator.ExecuteEnemyPhaseStartActions();
            BuildingManager.Instance?.ExecuteTurnStartActions();
            await MapManager.Instance.ExecuteTurnStartActions();
            await MapManager.Instance.MoveUnit();
            await MapManager.Instance.ExecuteTurnEndActions();
            await CameraManager.Instance.ActResetCameraTarget();

            // 耐久クリア条件などのターン経過ハンドラーを await する。
            // クリア成立ならクリア演出・シナリオ再生の完了までここで止まる。
            await DomainEventDispatcher.Dispatch(new TurnEndEvent());

            return PlayerPhase.Instance;
        }
    }
}
