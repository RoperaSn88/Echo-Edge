using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

using EchoEdge.App.Battle;
using EchoEdge.Domain.Phase;
using EchoEdge.Presenter.Actions;

namespace EchoEdge.Domain.Battle
{
    /// <summary>
    /// エナジーウォールの効果クラス。
    /// 選択したマスに、向き（斜面が向く方向を4方向から選べる）三角柱の壁を1ターン設置する。
    /// </summary>
    public class EnergyWall : IEquipEffect
    {
        /// <summary>
        /// 前回選んだ向き。次に使うときの初期値にする。
        /// 最初はプレイヤー側（左）へ斜面を向けておく
        /// </summary>
        private static EnergyWallDirection _lastDirection = EnergyWallDirection.UpLeft;

        private const int DirectionCount = 4;

        private EnergyWallDirection _direction = _lastDirection;

        private bool _isDecided;
        private bool _isCanceled;

        /// <summary>
        /// 床マスを選んだ後に、斜面の向きを選ぶ。
        /// ホイールの回転で向きを 90° ずつ回し（上で時計回り、下で反時計回り）、ホイールの押し込みでも時計回りに回す。
        /// 左クリックで確定、右クリックで取り消す。
        /// </summary>
        /// <returns>確定したら true、取り消したり設置できないマスだったら false</returns>
        public async UniTask<bool> SelectDirectionAsync()
        {
            if (EnergyWallManager.Instance == null || !TryGetTargetFloorPos(out int h, out int w))
            {
                return false;
            }

            if (!EnergyWallManager.Instance.TryBeginPreview(h, w, _direction))
            {
                return false;
            }

            _isDecided = false;
            _isCanceled = false;

            PlayerActions playerActions = new PlayerActions();
            playerActions.PlayerPhase.Attack.started += OnPressDecide;
            playerActions.PlayerPhase.Skill.started += OnPressCancel;
            playerActions.PlayerPhase.Scroll.performed += OnScroll;
            playerActions.PlayerPhase.ToggleFlash.started += OnPressToggle;
            playerActions.Enable();

            await UniTask.WaitUntil(() => _isDecided || _isCanceled);

            playerActions.Dispose();

            if (_isCanceled)
            {
                EnergyWallManager.Instance.CancelPreview();
                return false;
            }

            _lastDirection = _direction;
            return true;
        }

        /// <summary>
        /// エナジーウォールの効果を発揮する
        /// </summary>
        public async UniTask Activate()
        {
            if (MapManager.Instance == null || !TryGetTargetFloorPos(out int h, out int w))
            {
                EnergyWallManager.Instance?.CancelPreview();
                return;
            }

            EnergyWallManager.Instance.TryConfirmPreview(h, w, _direction);
            await UniTask.CompletedTask;
        }

        private static bool TryGetTargetFloorPos(out int h, out int w)
        {
            Vector2Int targetFloorPos = PlayerEquipPhase.Instance.TargetFloorPos;
            h = targetFloorPos.y;
            w = targetFloorPos.x;
            return targetFloorPos.x != int.MinValue && targetFloorPos.y != int.MinValue;
        }

        /// <summary>
        /// 向きを 90° ずつ回す
        /// </summary>
        /// <param name="step">1 で時計回り、-1 で反時計回り</param>
        private void RotateDirection(int step)
        {
            _direction = (EnergyWallDirection)(((int)_direction + step + DirectionCount) % DirectionCount);
            EnergyWallManager.Instance.SetPreviewDirection(_direction);
        }

        private void OnPressDecide(InputAction.CallbackContext context)
        {
            _isDecided = true;
        }

        private void OnPressCancel(InputAction.CallbackContext context)
        {
            _isCanceled = true;
        }

        private void OnScroll(InputAction.CallbackContext context)
        {
            float scroll = context.ReadValue<float>();
            if (Mathf.Approximately(scroll, 0f)) return;
            RotateDirection(scroll > 0f ? 1 : -1);
        }

        private void OnPressToggle(InputAction.CallbackContext context)
        {
            RotateDirection(1);
        }
    }
}
