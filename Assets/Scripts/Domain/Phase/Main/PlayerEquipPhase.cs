using System;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

using EchoEdge.App.Battle;
using EchoEdge.Domain.Battle;
using EchoEdge.Infra.Camera;
using EchoEdge.Presenter.Actions;
using EchoEdge.Presenter.UI;

namespace EchoEdge.Domain.Phase
{
    /// <summary>
    /// 装備品使用フェーズ（床マスを選ぶ）。
    /// プレビューを出す装備品（<see cref="IEquipPlacementPreview"/>）の場合は、カーソルの下のマスにプレビューを追従させ、
    /// 設置できるマスを左クリックしたときだけ次へ進む。
    /// </summary>
    public class PlayerEquipPhase : IPhase
    {
        /// <summary>
        /// 装備品使用フェーズのインスタンス
        /// </summary>
        private static PlayerEquipPhase _instance;

        /// <summary>
        /// 他のスクリプトから干渉するプロパティ
        /// </summary>
        public static PlayerEquipPhase Instance => _instance ??= new PlayerEquipPhase();

        /// <summary>
        /// クリックされたか検知するブール
        /// </summary>
        private bool _clickFlug;

        /// <summary>
        /// クリックの種類を保存する
        /// </summary>
        private ClickKinds _clickKind;

        /// <summary>
        /// クリック時のマウスのスクリーン座標
        /// </summary>
        private Vector2 _clickMousePos;

        /// <summary>
        /// クリック位置の床座標をマップベース座標からのオフセット(Vector2Int)で保存する
        /// </summary>
        private Vector2Int _targetFloorPos;
        public Vector2Int TargetFloorPos => _targetFloorPos;

        /// <summary>
        /// 選択中の装備品の効果。<see cref="PlayerWeaponActivePhase"/> が使う
        /// </summary>
        public IEquipEffect SelectedEffect { get; private set; }

        /// <summary>
        /// 選択中の装備品がプレビューを出す場合はその参照。出さない場合は null
        /// </summary>
        private IEquipPlacementPreview _placementPreview;

        /// <summary>
        /// プレビューを出しているマス。床マスが無い場合は int.MinValue
        /// </summary>
        private Vector2Int _hoverFloorPos;

        /// <summary>
        /// プレビューを出しているマスに設置できるか
        /// </summary>
        private bool _canPlaceAtHover;

        private static readonly Vector2Int NoFloorPos = new Vector2Int(int.MinValue, int.MinValue);

        public async UniTask<IPhase> WaitPhase()
        {
            SelectedEffect = EquipEffectFactory.Create(WeaponController.Instance.SelectedWeaponIndex);
            _placementPreview = SelectedEffect as IEquipPlacementPreview;
            _hoverFloorPos = NoFloorPos;
            _canPlaceAtHover = false;

            OperateInfos.Instance.SetOperateInfo(PhaseKinds.Equip).Forget();

            // 装備品のUIを非表示にする
            WeaponController.Instance.HideAllWeaponUIs();

            // カメラを上方向にする
            CameraManager.Instance.ActMoveCameraToTopAngle();

            _clickFlug = false;
            PlayerActions playerActions = new PlayerActions();
            playerActions.PlayerPhase.Attack.started += OnPressAttack;
            playerActions.PlayerPhase.Skill.started += OnPressSkill;
            if (_placementPreview != null)
            {
                playerActions.PlayerPhase.Scroll.performed += OnScroll;
                playerActions.PlayerPhase.ToggleFlash.started += OnPressWheel;
            }
            playerActions.Enable();

            // クリックを待つ。プレビューを出す装備品は、待っている間カーソルの下のマスへプレビューを動かす
            while (true)
            {
                UpdatePreview();

                if (_clickFlug)
                {
                    _clickFlug = false;

                    // 設置できないマスの左クリックは無視して選び直してもらう
                    if (_placementPreview == null || _clickKind != ClickKinds.Left || _canPlaceAtHover)
                    {
                        break;
                    }
                }

                await UniTask.Yield();
            }

            playerActions.Dispose();

            // 左クリック時はクリック位置の床座標を保存する
            if (_clickKind == ClickKinds.Left)
            {
                _targetFloorPos = _placementPreview != null
                    ? _hoverFloorPos
                    : GetFloorPos(_clickMousePos);
            }
            else
            {
                _placementPreview?.CancelPreview();
            }

            switch (_clickKind)
            {
                case ClickKinds.Left:
                    return PlayerWeaponActivePhase.Instance;
                case ClickKinds.Right:
                    return PlayerWeaponPhase.Instance;
            }

            throw new InvalidOperationException("クリックがうまくできない謎のエラーです");
        }

        /// <summary>
        /// カーソルの下の床マスが変わったときだけ、プレビューを動かす
        /// </summary>
        private void UpdatePreview()
        {
            if (_placementPreview == null) return;

            var floorPos = GetFloorPos(CameraManager.Instance.GetMousePosition());
            if (floorPos == _hoverFloorPos) return;

            _hoverFloorPos = floorPos;
            if (floorPos == NoFloorPos)
            {
                _canPlaceAtHover = false;
                _placementPreview.HidePreview();
                return;
            }

            _canPlaceAtHover = _placementPreview.ShowPreviewAt(floorPos.y, floorPos.x);
        }

        /// <summary>
        /// スクリーン座標の下にある床マスを、マップベース座標からのオフセットで返す。床が無ければ int.MinValue
        /// </summary>
        private static Vector2Int GetFloorPos(Vector2 screenPos)
        {
            Ray ray = Camera.main.ScreenPointToRay(screenPos);
            if (!Physics.Raycast(ray, out RaycastHit rch, math.INFINITY, PlayerAttackPhase.LayerNumber))
            {
                return NoFloorPos;
            }

            Vector3 basePos = MapManager.Instance.GetBasePos();
            return new Vector2Int(
                Mathf.RoundToInt(rch.point.x - basePos.x),
                Mathf.RoundToInt(rch.point.z - basePos.z)
            );
        }

        private void OnScroll(InputAction.CallbackContext context)
        {
            _placementPreview?.OnScroll(context.ReadValue<float>());
        }

        private void OnPressWheel(InputAction.CallbackContext context)
        {
            _placementPreview?.OnPressWheel();
        }

        private void OnPressAttack(InputAction.CallbackContext context)
        {
            _clickMousePos = CameraManager.Instance.GetMousePosition();
            _clickKind = ClickKinds.Left;
            _clickFlug = true;
        }

        private void OnPressSkill(InputAction.CallbackContext context)
        {
            _clickKind = ClickKinds.Right;
            _clickFlug = true;
        }
    }
}
