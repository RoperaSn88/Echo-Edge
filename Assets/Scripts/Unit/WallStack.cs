using System.Collections.Generic;
using UnityEngine;

public class WallStack : MonoBehaviour
{
    [SerializeField]
    private BuildingView wallPrefab;

    [SerializeField]
    private Transform wallsParent;

    [SerializeField, Min(0)]
    private int initialPoolSize = 16;

    private readonly Stack<BuildingView> _availableWalls = new();

    private void Awake()
    {
        SetupPool();
    }

    public BuildingView GetWall()
    {
        while (_availableWalls.Count > 0)
        {
            var pooledWall = _availableWalls.Pop();
            if (pooledWall != null)
            {
                pooledWall.gameObject.SetActive(true);
                return pooledWall;
            }
        }

        var wall = CreateWall();
        if (wall != null)
        {
            wall.gameObject.SetActive(true);
        }
        return wall;
    }

    public void ReturnWall(BuildingView wall)
    {
        if (wall == null) return;
        wall.gameObject.SetActive(false);
        _availableWalls.Push(wall);
    }

    private void SetupPool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            var wall = CreateWall();
            if (wall != null)
            {
                _availableWalls.Push(wall);
            }
        }
    }

    private BuildingView CreateWall()
    {
        if (wallPrefab == null)
        {
            Debug.LogError("wallPrefab が未設定です。");
            return null;
        }

        var wall = Instantiate(wallPrefab, wallsParent);
        wall.gameObject.SetActive(false);
        return wall;
    }
}
