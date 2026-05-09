using UnityEngine;
using System.Collections.Generic;

public class BuildingManager : MonoBehaviour
{
    private const int EnergyWallLifetimePlayerPhaseStarts = 2;

    private class ActiveEnergyWall
    {
        public BuildingView View;
        public int Height;
        public int Width;
        public int RemainingPlayerPhaseStarts;
    }

    public static BuildingManager Instance;
    public event System.Action OnTurnStart;

    [SerializeField]
    private WallStack wallStack;

    [SerializeField]
    private WallStack builderWallStack;

    private readonly List<(BuildingView view, int h, int w)> _activeBuilderWalls = new();
    private readonly List<ActiveEnergyWall> _activeEnergyWalls = new();
    private readonly List<(BuildingView view, int h, int w)> _activeWalls = new();

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
    /// 指定座標の壁を削除し、プールに返却します。
    /// </summary>
    public void TryRemoveWallAt(int h, int w)
    {
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

    public bool TrySetEnergyWall(int h, int w)
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
        _activeEnergyWalls.Add(new ActiveEnergyWall
        {
            View = wallView,
            Height = h,
            Width = w,
            RemainingPlayerPhaseStarts = EnergyWallLifetimePlayerPhaseStarts
        });
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

    public void ReturnAllEnergyWalls()
    {
        if (builderWallStack == null)
        {
            return;
        }

        for (int i = _activeEnergyWalls.Count - 1; i >= 0; i--)
        {
            var activeWall = _activeEnergyWalls[i];
            activeWall.RemainingPlayerPhaseStarts -= 1;

            if (activeWall.RemainingPlayerPhaseStarts > 0)
            {
                continue;
            }

            if (MapManager.Instance != null)
            {
                MapManager.Instance.RemoveUnitAt(activeWall.Height, activeWall.Width);
            }
            builderWallStack.ReturnBuilderWall(activeWall.View);
            _activeEnergyWalls.RemoveAt(i);
        }
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
