using Cysharp.Threading.Tasks;
using UnityEngine;

using EchoEdge.Domain.Phase;
using EchoEdge.Infra.Camera;
using EchoEdge.Presenter.Battle;

namespace EchoEdge.Presenter.Player
{
    /// <summary>
    /// リエイムパッドに触れたとき、パッドの中心から一閃の向きを選び直させる。
    /// 操作は一閃の角度選択（<see cref="PlayerAttackPreparationPhase"/>）と同じで、ポインターの先へ向けてクリックで発射する。
    /// 選んでいる間は見下ろしカメラにし、予測線（残りの反射回数ぶん）を出す。右クリックでのキャンセルと剣の持ち替えはできない。
    /// </summary>
    public static class ReAimDirectionSelector
    {
        /// <summary>
        /// 向きを選ばせ、選んだ向き（XZ 平面で正規化済み）を返す。
        /// </summary>
        /// <param name="origin">一閃を再開する位置（パッドの中心）</param>
        /// <param name="remainingReflectCount">残りの【反射回数】（使い切っていれば 0）</param>
        public static async UniTask<Vector3> SelectAsync(Vector3 origin, int remainingReflectCount)
        {
            var player = PlayerController.Instance;
            var camera = CameraManager.Instance;

            // 一閃の角度選択と同じ見下ろしカメラ（トゥイーン半分で直交投影＋敵スプライトの寝かせ）にする
            camera.ActMoveCameraToTopAngle();

            var guideLine = new PlayerAttackGuideLine();
            guideLine.SetMaterial(player.LineMaterial);

            Vector3? lastPointerTarget = null;
            Vector3? selectedTarget = null;

            var playerActions = new PlayerActions();
            playerActions.PlayerPhase.Attack.started += _ =>
            {
                // クリックした瞬間のポインター位置で向きを確定させる
                Vector3? target = TryGetPointerTarget(out var hitPoint) ? hitPoint : lastPointerTarget;
                if (target == null || !HasDirection(origin, target.Value)) return;
                selectedTarget = target;
            };
            playerActions.Enable();

            try
            {
                while (selectedTarget == null)
                {
                    if (TryGetPointerTarget(out var pointerTarget) && HasDirection(origin, pointerTarget))
                    {
                        guideLine.Update(origin, pointerTarget, player.AttackKind, player.IsFlashAttack,
                            remainingReflectCount, player.LayerMask);
                        lastPointerTarget = pointerTarget;
                    }
                    else
                    {
                        guideLine.Hide();
                    }

                    await UniTask.Yield();
                }
            }
            finally
            {
                playerActions.Dispose();
                guideLine.Hide();
                guideLine.Destroy();
            }

            // 一閃中の追尾カメラ（透視投影・スプライト復帰）へ戻す
            await camera.ActMoveCameraToAttackTracking();

            var selected = selectedTarget.Value;
            return new Vector3(selected.x - origin.x, 0f, selected.z - origin.z).normalized;
        }

        private static bool HasDirection(Vector3 origin, Vector3 target)
        {
            return new Vector2(target.x - origin.x, target.z - origin.z).sqrMagnitude > 0.0001f;
        }

        /// <summary>
        /// 現在のポインターが指している盤面上の位置を取得する（一閃の角度選択と同じレイキャスト）。
        /// </summary>
        private static bool TryGetPointerTarget(out Vector3 target)
        {
            target = default;
            if (Camera.main == null || CameraManager.Instance == null) return false;

            Ray pointerRay = Camera.main.ScreenPointToRay(CameraManager.Instance.GetMousePosition());
            if (!Physics.Raycast(pointerRay, out RaycastHit pointerHit, Mathf.Infinity, PlayerAttackPhase.LayerNumber))
            {
                return false;
            }

            target = pointerHit.point;
            return true;
        }
    }
}
