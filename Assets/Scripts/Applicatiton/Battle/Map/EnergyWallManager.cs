using System.Collections.Generic;
using UnityEngine;

using EchoEdge.Domain.Battle;
using EchoEdge.Domain.Phase;
using EchoEdge.Presenter.Battle;

namespace EchoEdge.App.Battle
{
    public class EnergyWallManager : IEnemyPhaseStartAction
    {
        private const int EnergyWallLifetime = 1;

        private class ActiveEnergyWall
        {
            public SlopeWallView View;
            public int Height;
            public int Width;
            public int RemainingLifetime;
        }

        private static EnergyWallManager _instance;
        public static EnergyWallManager Instance => _instance ??= new EnergyWallManager();

        private readonly List<ActiveEnergyWall> _activeEnergyWalls = new();
        private EnergyWallStack _energyWallStack;

        /// <summary>
        /// 床マスを選んでいる間、カーソルの下に表示しているプレビュー用の壁
        /// </summary>
        private SlopeWallView _previewWall;
        private int _previewHeight;
        private int _previewWidth;

        private EnergyWallManager()
        {
            EnemyPhaseStartActivator.Register(this);
        }

        /// <summary>
        /// オブジェクトプールと壁リストをリセットする。ゲーム開始時に呼ぶ。
        /// </summary>
        public static void Reset()
        {
            if (_instance == null)
            {
                return;
            }

            _instance._activeEnergyWalls.Clear();
            _instance._energyWallStack = null;
            _instance._previewWall = null;
        }

        /// <summary>
        /// 指定マスにエナジーウォールのプレビューを表示する。表示中のプレビューがあれば、そのまま指定マスへ動かす。
        /// マスが範囲外・使用中の場合はプレビューを片付けて false を返す。
        /// </summary>
        public bool MovePreview(int h, int w, EnergyWallDirection direction)
        {
            if (MapManager.Instance == null || !MapManager.Instance.IsFootprintFree(h, w, 1, null))
            {
                CancelPreview();
                return false;
            }

            if (_previewWall == null)
            {
                if (!TryGetEnergyWallStack(out var wallStack))
                {
                    return false;
                }

                _previewWall = wallStack.GetWall();
                if (_previewWall == null)
                {
                    return false;
                }
            }

            _previewWall.BuildingView.Set(h, w);
            _previewWall.Apply(direction, true);
            _previewHeight = h;
            _previewWidth = w;
            return true;
        }

        /// <summary>
        /// プレビュー中の斜面の向きを変更する。
        /// </summary>
        public void SetPreviewDirection(EnergyWallDirection direction)
        {
            if (_previewWall == null) return;
            _previewWall.Apply(direction, true);
        }

        /// <summary>
        /// プレビューを取り消し、壁をプールへ返す。
        /// </summary>
        public void CancelPreview()
        {
            if (_previewWall == null) return;

            if (TryGetEnergyWallStack(out var wallStack))
            {
                wallStack.ReturnWall(_previewWall);
            }

            _previewWall = null;
        }

        /// <summary>
        /// プレビュー中の壁を、当たり判定のある斜面の壁として確定する。
        /// プレビューが無い場合は、指定マス・向きで新しく設置する。
        /// </summary>
        public bool TryConfirmPreview(int h, int w, EnergyWallDirection direction)
        {
            if (_previewWall == null || _previewHeight != h || _previewWidth != w)
            {
                if (!MovePreview(h, w, direction))
                {
                    return false;
                }
            }

            var wallUnit = new building();
            if (MapManager.Instance == null || !MapManager.Instance.PlaceUnitAt(wallUnit, h, w))
            {
                CancelPreview();
                return false;
            }

            var wallView = _previewWall;
            _previewWall = null;

            wallView.Apply(direction, false);
            _activeEnergyWalls.Add(new ActiveEnergyWall
            {
                View = wallView,
                Height = h,
                Width = w,
                RemainingLifetime = EnergyWallLifetime
            });

            return true;
        }

        public void OnEnemyPhaseStart()
        {
            if (!TryGetEnergyWallStack(out var wallStack))
            {
                return;
            }

            for (int i = _activeEnergyWalls.Count - 1; i >= 0; i--)
            {
                var activeWall = _activeEnergyWalls[i];
                activeWall.RemainingLifetime -= 1;

                if (activeWall.RemainingLifetime > 0)
                {
                    continue;
                }

                if (MapManager.Instance != null)
                {
                    MapManager.Instance.RemoveUnitAt(activeWall.Height, activeWall.Width);
                }

                wallStack.ReturnWall(activeWall.View);
                _activeEnergyWalls.RemoveAt(i);
            }
        }

        private bool TryGetEnergyWallStack(out EnergyWallStack wallStack)
        {
            if (_energyWallStack == null)
            {
                _energyWallStack = Object.FindFirstObjectByType<EnergyWallStack>();
                if (_energyWallStack == null)
                {
                    Debug.LogWarning("EnergyWallStack がシーンに配置されていないため、エナジーウォールを設置できません。");
                }
            }

            wallStack = _energyWallStack;
            return wallStack != null;
        }
    }
}
