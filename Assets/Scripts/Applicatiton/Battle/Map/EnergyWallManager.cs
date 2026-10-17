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
            public BuildingView View;
            public SlopeWallView SlopeView;
            public int Height;
            public int Width;
            public int RemainingLifetime;
        }

        private static EnergyWallManager _instance;
        public static EnergyWallManager Instance => _instance ??= new EnergyWallManager();

        private readonly List<ActiveEnergyWall> _activeEnergyWalls = new();
        private WallStack _energyWallStack;

        /// <summary>
        /// 向き選択中に表示しているプレビュー用の壁
        /// </summary>
        private BuildingView _previewWall;
        private SlopeWallView _previewSlope;
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
            _instance._previewSlope = null;
        }

        /// <summary>
        /// 指定マスに斜面のエナジーウォールのプレビューを表示する。
        /// マスが範囲外・使用中の場合は表示せず false を返す。
        /// </summary>
        public bool TryBeginPreview(int h, int w, EnergyWallDirection direction)
        {
            CancelPreview();

            if (MapManager.Instance == null || !MapManager.Instance.IsFootprintFree(h, w, 1, null))
            {
                return false;
            }

            if (!TryGetEnergyWallStack(out var wallStack))
            {
                return false;
            }

            var wallView = wallStack.GetTemporaryWall();
            if (wallView == null)
            {
                return false;
            }

            wallView.Set(h, w);
            _previewWall = wallView;
            _previewSlope = SlopeWallView.GetOrAdd(wallView);
            _previewSlope.ApplySlope(direction, true);
            _previewHeight = h;
            _previewWidth = w;
            return true;
        }

        /// <summary>
        /// プレビュー中の斜面の向きを変更する。
        /// </summary>
        public void SetPreviewDirection(EnergyWallDirection direction)
        {
            if (_previewSlope == null) return;
            _previewSlope.ApplySlope(direction, true);
        }

        /// <summary>
        /// プレビューを取り消し、壁をプールへ返す。
        /// </summary>
        public void CancelPreview()
        {
            if (_previewWall == null) return;

            _previewSlope?.Restore();
            if (TryGetEnergyWallStack(out var wallStack))
            {
                wallStack.ReturnTemporaryWall(_previewWall);
            }

            _previewWall = null;
            _previewSlope = null;
        }

        /// <summary>
        /// プレビュー中の壁を、当たり判定のある斜面の壁として確定する。
        /// プレビューが無い場合は、指定マス・向きで新しく設置する。
        /// </summary>
        public bool TryConfirmPreview(int h, int w, EnergyWallDirection direction)
        {
            if (_previewWall == null || _previewHeight != h || _previewWidth != w)
            {
                if (!TryBeginPreview(h, w, direction))
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
            var slopeView = _previewSlope;
            _previewWall = null;
            _previewSlope = null;

            slopeView.ApplySlope(direction, false);
            _activeEnergyWalls.Add(new ActiveEnergyWall
            {
                View = wallView,
                SlopeView = slopeView,
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

                // プールはビルダーの壁と共有しているため、キューブの壁に戻してから返す
                activeWall.SlopeView?.Restore();
                wallStack.ReturnTemporaryWall(activeWall.View);
                _activeEnergyWalls.RemoveAt(i);
            }
        }

        private bool TryGetEnergyWallStack(out WallStack wallStack)
        {
            if (_energyWallStack != null)
            {
                wallStack = _energyWallStack;
                return true;
            }

            var wallStacks = Object.FindObjectsByType<WallStack>(FindObjectsSortMode.None);
            if (wallStacks.Length == 0)
            {
                wallStack = null;
                return false;
            }

            _energyWallStack = wallStacks[0];
            wallStack = _energyWallStack;
            return true;
        }
    }
}
