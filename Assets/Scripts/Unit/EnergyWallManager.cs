using System.Collections.Generic;
using UnityEngine;

public class EnergyWallManager : IEnemyPhaseStartAction
{
    private const int EnergyWallLifetimeEnemyPhaseStarts = 1;

    private class ActiveEnergyWall
    {
        public BuildingView View;
        public int Height;
        public int Width;
        public int RemainingEnemyPhaseStarts;
    }

    private static EnergyWallManager _instance;
    public static EnergyWallManager Instance => _instance ??= new EnergyWallManager();

    private readonly List<ActiveEnergyWall> _activeEnergyWalls = new();
    private WallStack _energyWallStack;

    private EnergyWallManager()
    {
        EnemyPhaseStartActionDispatcher.Register(this);
    }

    public bool TrySetEnergyWall(int h, int w)
    {
        if (MapManager.Instance == null)
        {
            return false;
        }

        if (!TryGetEnergyWallStack(out var wallStack))
        {
            return false;
        }

        var wallUnit = new building();
        if (!MapManager.Instance.PlaceUnitAt(wallUnit, h, w))
        {
            return false;
        }

        var wallView = wallStack.GetBuilderWall();
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
            RemainingEnemyPhaseStarts = EnergyWallLifetimeEnemyPhaseStarts
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
            if (activeWall.RemainingEnemyPhaseStarts > 0)
            {
                activeWall.RemainingEnemyPhaseStarts -= 1;
                continue;
            }

            if (MapManager.Instance != null)
            {
                MapManager.Instance.RemoveUnitAt(activeWall.Height, activeWall.Width);
            }

            wallStack.ReturnBuilderWall(activeWall.View);
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
        if (wallStacks.Length <= 0)
        {
            wallStack = null;
            return false;
        }

        _energyWallStack = wallStacks[0];
        wallStack = _energyWallStack;
        return true;
    }
}
