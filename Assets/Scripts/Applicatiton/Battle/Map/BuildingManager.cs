using UnityEngine;
using System.Collections.Generic;

using EchoEdge.Domain.Battle;
using EchoEdge.Presenter.Battle;

namespace EchoEdge.App.Battle
{
    public class BuildingManager : MonoBehaviour
    {
        public static BuildingManager Instance;
        public event System.Action OnTurnStart;

        [SerializeField]
        private WallStack wallStack;

        [SerializeField]
        private WallStack builderWallStack;

        /// <summary>
        /// ステージ CSV の SlopeWall 用のプール（エナジーウォール用の <see cref="EnergyWallStack"/> とは別）
        /// </summary>
        [SerializeField]
        private SlopeWallStack slopeWallStack;

        private readonly List<(BuildingView view, int h, int w)> _activeBuilderWalls = new();
        private readonly List<(BuildingView view, int h, int w)> _activeWalls = new();
        private readonly List<(SlopeWallView view, int h, int w)> _activeSlopeWalls = new();

        void Start()
        {
            Instance = this;
            if (wallStack == null)
            {
                var wallStacks = FindObjectsByType<WallStack>(FindObjectsSortMode.None);
                if (wallStacks.Length > 0)
                {
                    wallStack = wallStacks[0];
                }
            }

            if (builderWallStack == null)
            {
                builderWallStack = wallStack;
                Debug.LogWarning("Builder専用WallStackが未設定のため、通常WallStack内のBuilder専用プールを使用します。");
            }

            if (slopeWallStack == null)
            {
                foreach (var stack in FindObjectsByType<SlopeWallStack>(FindObjectsSortMode.None))
                {
                    // エナジーウォール用のプールは使わない
                    if (stack is EnergyWallStack) continue;
                    slopeWallStack = stack;
                    break;
                }
            }

            RegisterTurnStartAction(ReturnAllBuilderWalls);
        }

        public void SetBuilding(int h, int w)
        {
            if (wallStack == null)
            {
                Debug.LogError("WallStack が見つかりません。");
                return;
            }

            var v = wallStack.GetWall();
            if (v == null)
            {
                return;
            }
            v.Set(h, w);
            _activeWalls.Add((v, h, w));
        }

        /// <summary>
        /// ステージ CSV の斜面の壁を指定座標・向きで配置します。
        /// </summary>
        public void SetSlopeWall(int h, int w, EnergyWallDirection direction)
        {
            if (slopeWallStack == null)
            {
                Debug.LogError("SlopeWallStack が見つかりません。");
                return;
            }

            var v = slopeWallStack.GetWall();
            if (v == null)
            {
                return;
            }
            v.BuildingView.Set(h, w);
            v.Apply(direction, false);
            _activeSlopeWalls.Add((v, h, w));
        }

        /// <summary>
        /// 指定座標の壁（斜面の壁を含む）を削除し、プールに返却します。
        /// </summary>
        public void TryRemoveWallAt(int h, int w)
        {
            if (slopeWallStack != null)
            {
                for (int i = _activeSlopeWalls.Count - 1; i >= 0; i--)
                {
                    var slopeWall = _activeSlopeWalls[i];
                    if (slopeWall.h == h && slopeWall.w == w)
                    {
                        if (MapManager.Instance != null)
                        {
                            MapManager.Instance.RemoveUnitAt(h, w);
                        }
                        slopeWallStack.ReturnWall(slopeWall.view);
                        _activeSlopeWalls.RemoveAt(i);
                        return;
                    }
                }
            }

            if (wallStack == null) return;
            for (int i = _activeWalls.Count - 1; i >= 0; i--)
            {
                var wall = _activeWalls[i];
                if (wall.h == h && wall.w == w)
                {
                    if (MapManager.Instance != null)
                    {
                        MapManager.Instance.RemoveUnitAt(h, w);
                    }
                    wallStack.ReturnWall(wall.view);
                    _activeWalls.RemoveAt(i);
                    return;
                }
            }
        }

        /// <summary>
        /// 現在配置されている壁をすべてプールに返却します（ウェーブ切り替え時に使用）。
        /// </summary>
        public void ResetWalls()
        {
            if (wallStack != null)
            {
                foreach (var wall in _activeWalls)
                {
                    wallStack.ReturnWall(wall.view);
                }
            }
            _activeWalls.Clear();

            if (slopeWallStack != null)
            {
                foreach (var slopeWall in _activeSlopeWalls)
                {
                    slopeWallStack.ReturnWall(slopeWall.view);
                }
            }
            _activeSlopeWalls.Clear();
        }

        public bool TrySetBuilderWall(int h, int w)
        {
            if (builderWallStack == null || MapManager.Instance == null)
            {
                return false;
            }

            var wallUnit = new building();
            if (!MapManager.Instance.PlaceUnitAt(wallUnit, h, w))
            {
                return false;
            }

            var wallView = builderWallStack.GetBuilderWall();
            if (wallView == null)
            {
                MapManager.Instance.RemoveUnitAt(h, w);
                return false;
            }

            wallView.Set(h, w);
            _activeBuilderWalls.Add((wallView, h, w));
            return true;
        }

        public void ReturnAllBuilderWalls()
        {
            if (builderWallStack == null)
            {
                return;
            }

            foreach (var activeWall in _activeBuilderWalls)
            {
                if (MapManager.Instance != null)
                {
                    MapManager.Instance.RemoveUnitAt(activeWall.h, activeWall.w);
                }
                builderWallStack.ReturnBuilderWall(activeWall.view);
            }
            _activeBuilderWalls.Clear();
        }

        public void ExecuteTurnStartActions()
        {
            OnTurnStart?.Invoke();
        }

        public void RegisterTurnStartAction(System.Action action)
        {
            if (action == null)
            {
                return;
            }
            OnTurnStart += action;
        }
    }
}
