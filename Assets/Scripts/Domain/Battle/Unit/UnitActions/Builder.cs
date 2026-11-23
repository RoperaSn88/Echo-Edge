using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

using EchoEdge.App.Battle;
using EchoEdge.Infra.Camera;
using EchoEdge.Presenter.Battle;
using EchoEdge.Presenter.Player;
using EchoEdge.Presenter.UI;

namespace EchoEdge.Domain.Battle
{
    public class Builder: IUnitAction, IUnitViewAware, IScriptedSkillAction
    {
        private const float PlayerDamageRate = 1.0f;
        private const float SpecificRate = 0.8f;

        /// <summary>
        /// 自分の View。壁を建てたときに、自分と壁を同時にカメラで映すために使う
        /// </summary>
        private IUnitView _view;
        
        public async UniTask BeforeAttack()
        {
            await MessagePresenter.Instance.AppearMessage("ビルダーの攻撃");
        }
        
        /// <inheritdoc/>
        public async UniTask Attack()
        {
            try
            {
                await BattleManager.PlayerDamageWithText(PlayerDamageRate);

                await UniTask.Delay(TimeSpan.FromSeconds(1.0f));
            }
            finally
            {
                await CameraManager.Instance.ActResetCameraTarget();
            }
        }
        
        /// <inheritdoc/>
        public UniTask<EnemyMoveKinds> Act(int selfHeight, int selfWidth)
        {
            if (selfWidth == 0)
            {
                return UniTask.FromResult(EnemyMoveKinds.Attack);
            }
            
            // 自分以外に壁を建てる対象のユニットがいなければ、スキルは使わない
            if (TryPrepareSkill(selfHeight, selfWidth) && UnityEngine.Random.value < SpecificRate)
            {
                return UniTask.FromResult(EnemyMoveKinds.Specific);
            }

            return UniTask.FromResult(EnemyMoveKinds.None);
        }

        /// <inheritdoc/>
        public bool TryPrepareSkill(int selfHeight, int selfWidth)
        {
            if (MapManager.Instance == null || BuildingManager.Instance == null)
            {
                return false;
            }
            return GetTargetUnits(selfHeight, selfWidth).Count > 0;
        }

        /// <summary>
        /// 壁を建てる対象になる、自分以外の動けるユニットを返す
        /// </summary>
        private static List<(IUnit unit, int h, int w)> GetTargetUnits(int selfHeight, int selfWidth)
        {
            return MapManager.Instance.GetUnitPositionsSnapshot()
                .Where(unitInfo => unitInfo.unit.CanMove())
                .Where(unitInfo => unitInfo.h != selfHeight || unitInfo.w != selfWidth)
                .ToList();
        }

        /// <inheritdoc/>
        public async UniTask Dead()
        {
            await UniTask.CompletedTask;
        }
        
        public async UniTask BeforeSpecific()
        {
            await MessagePresenter.Instance.AppearMessage("ビルダーは壁を建設し始めた");
        }

        /// <inheritdoc/>
        public async UniTask Specific(int selfHeight, int selfWidth)
        {
            if (MapManager.Instance == null || BuildingManager.Instance == null)
            {
                return;
            }

            var targetUnits = GetTargetUnits(selfHeight, selfWidth);

            if (targetUnits.Count == 0)
            {
                return;
            }

            var target = targetUnits[UnityEngine.Random.Range(0, targetUnits.Count)];
            int wallWidth = target.w - 1;

            var builtWalls = new List<Transform>();
            for (int hOffset = -1; hOffset <= 1; hOffset++)
            {
                int wallHeight = target.h + hOffset;
                if (MapManager.Instance.GetUnitAt(wallHeight, wallWidth) != null)
                {
                    continue;
                }
                if (BuildingManager.Instance.TrySetBuilderWall(wallHeight, wallWidth, out var wallTransform))
                {
                    builtWalls.Add(wallTransform);
                }
            }

            try
            {
                // 壁が画面外に建つこともあるので、ビルダーと建てた壁が同時に映るようカメラを動かす
                if (_view != null && builtWalls.Count > 0)
                {
                    await _view.WaitToCameraZoomWith(builtWalls);
                }

                await UniTask.Delay(TimeSpan.FromSeconds(1f));
            }
            finally
            {
                await CameraManager.Instance.ActResetCameraTarget();
            }
        }

        /// <inheritdoc/>
        public void SetView(IUnitView view)
        {
            _view = view;
        }

        /// <inheritdoc/>
        public UniTask OnTurnStart()
        {
            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public UniTask OnTurnEnd()
        {
            return UniTask.CompletedTask;
        }
        
        /// <inheritdoc/>
        public UniTask Damage()
        {
            return UniTask.CompletedTask;
        }
    }
}
