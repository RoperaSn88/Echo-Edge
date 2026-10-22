using Cysharp.Threading.Tasks;
using UnityEngine;

using EchoEdge.App.Battle;
using EchoEdge.Domain.Phase;

namespace EchoEdge.Domain.Battle
{
    /// <summary>
    /// エナジーウォールの効果クラス。
    /// 床マスを選んでいる間はカーソルの下に半透明の三角柱を出し、ホイールで向き（斜面が向く方向を4方向）を回す。
    /// 左クリックしたマスに、その向きの三角柱の壁を1ターン設置する。
    /// </summary>
    public class EnergyWall : IEquipEffect, IEquipPlacementPreview
    {
        /// <summary>
        /// 前回選んだ向き。次に使うときの初期値にする。
        /// 最初はプレイヤー側（左）へ斜面を向けておく
        /// </summary>
        private static EnergyWallDirection _lastDirection = EnergyWallDirection.UpLeft;

        private const int DirectionCount = 4;

        private EnergyWallDirection _direction = _lastDirection;

        /// <inheritdoc/>
        public bool ShowPreviewAt(int h, int w)
        {
            return EnergyWallManager.Instance.MovePreview(h, w, _direction);
        }

        /// <inheritdoc/>
        public void HidePreview()
        {
            EnergyWallManager.Instance.CancelPreview();
        }

        /// <inheritdoc/>
        public void OnScroll(float scroll)
        {
            if (Mathf.Approximately(scroll, 0f)) return;
            // 上で時計回り、下で反時計回り
            RotateDirection(scroll > 0f ? 1 : -1);
        }

        /// <inheritdoc/>
        public void OnPressWheel()
        {
            RotateDirection(1);
        }

        /// <inheritdoc/>
        public void CancelPreview()
        {
            EnergyWallManager.Instance.CancelPreview();
        }

        /// <summary>
        /// エナジーウォールの効果を発揮する
        /// </summary>
        public async UniTask Activate()
        {
            Vector2Int targetFloorPos = PlayerEquipPhase.Instance.TargetFloorPos;
            if (MapManager.Instance == null || targetFloorPos.x == int.MinValue || targetFloorPos.y == int.MinValue)
            {
                EnergyWallManager.Instance.CancelPreview();
                return;
            }

            EnergyWallManager.Instance.TryConfirmPreview(targetFloorPos.y, targetFloorPos.x, _direction);
            _lastDirection = _direction;
            await UniTask.CompletedTask;
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
    }
}
