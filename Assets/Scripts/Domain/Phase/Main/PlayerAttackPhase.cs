using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

using EchoEdge.App.Battle;
using EchoEdge.Infra.Camera;
using EchoEdge.Infra.Tutorial;
using EchoEdge.Presenter.Player;
using EchoEdge.Presenter.UI;

namespace EchoEdge.Domain.Phase
{
    public class PlayerAttackPhase: IPhase
    {
        /// <summary>
        /// アタックフェーズのインスタンス
        /// </summary>
        private static PlayerAttackPhase _instance;

        /// <summary>
        /// 他のスクリプトから干渉するプロパティ
        /// </summary>
        public static PlayerAttackPhase Instance => _instance ??= new PlayerAttackPhase();

        public const int LayerNumber = 1;

        /// <summary>
        /// 一閃準備フェーズでクリックした瞬間に確定した攻撃目標。未設定ならフェーズ開始時のポインター位置を使う。
        /// </summary>
        private Vector3? _targetPosition;

        /// <summary>
        /// 攻撃目標を確定させる。一閃準備フェーズのクリック時に呼ぶ。
        /// </summary>
        public void SetTargetPosition(Vector3 targetPosition)
        {
            _targetPosition = targetPosition;
        }

        public async UniTask<IPhase> WaitPhase()
        {
            OperateInfos.Instance.SetOperateInfo(PhaseKinds.Attack).Forget();

            // クリック時に確定した攻撃目標を使う。未設定の場合のみ、カメラを動かす前に今のポインターの先の位置を取得する。
            Vector3 targetPosition;
            if (_targetPosition.HasValue)
            {
                targetPosition = _targetPosition.Value;
            }
            else
            {
                Ray ray = Camera.main.ScreenPointToRay(CameraManager.Instance.GetMousePosition());
                Physics.Raycast(ray, out RaycastHit rch, math.INFINITY, LayerNumber);
                targetPosition = rch.point;
            }
            _targetPosition = null;

            CameraManager.Instance.ActMoveCameraToDefault();

            // プレイヤーの攻撃開始時に追尾を開始する
            CameraManager.Instance.StartTracking(PlayerController.Instance.transform);
            
            if (TutorialSaveManager.TryBeginTutorial(TutorialKinds.QTE))
            {
                try
                {
                    await TutorialActivator.Instance.StartTutorial(TutorialKinds.QTE);
                }catch (System.Exception e)
                {
                    Debug.LogWarning($"チュートリアルを中止: {e}");
                }
            }

            await PlayerController.Instance.ExecuteAttack(targetPosition);

            // 追尾を終了する
            CameraManager.Instance.StopTracking();

            return await WaveManager.ResolvePhaseAfterAttackAsync();
        }
    }
}
