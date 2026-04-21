using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using AndanteTribe.Utils.Unity;

public class MapManager: MonoBehaviour
{
    public static MapManager Instance;
    
    /// <summary>
    /// マップの縦
    /// </summary>
    private const int MapHeight = 9;

    /// <summary>
    /// マップの横
    /// </summary>
    private const int MapWidth = 16;

    /// <summary>
    /// マップ全体
    /// </summary>
    private IUnit[,] _mapGrid = new IUnit[MapHeight, MapWidth];
    private readonly Dictionary<IUnit, (int h, int w)> _unitPositions = new();

    /// <summary>
    /// キャッシュ用
    /// </summary>
    private Vector3 vector;

    public void Start()
    {
        Instance = this;
    }

    public async UniTask BuildStageFromCsv()
    {
        ResetMap();

        var placements = await StageLayoutLoader.GetPlacementsAsync(MapHeight, MapWidth);
        await UniTask.WaitUntil(() => BuildingManager.Instance != null);
        await UniTask.WaitUntil(() => UnitSpawner.Instance != null);

        foreach (var placement in placements)
        {
            if (placement.objectKind == StageObjectKind.Wall)
            {
                var buildingUnit = new building();
                RegisterUnit(buildingUnit, placement.height, placement.width);
                BuildingManager.Instance.SetBuilding(placement.height, placement.width);
                continue;
            }

            if (placement.objectKind == StageObjectKind.Unit)
            {
                var unit = new BaseUnit(placement.height, placement.width);
                await unit.LoadStatus(placement.enemyKind);
                UnitSpawner.Instance.SpawnView(unit, placement.enemyKind);
            }
        }
    }

    public void RegisterUnit(IUnit unit, int h, int w)
    {
        if(_mapGrid[h, w] == null)
        {
            _mapGrid[h, w] = unit;
            _unitPositions[unit] = (h, w);
        }
        else throw new InvalidOperationException("指定したマスにはunitがいるか、範囲外です h:" + h + ", w:" + w);
    }

    /// <summary>
    /// 指定座標のユニットを取得します。範囲外なら null を返します。
    /// </summary>
    /// <param name="h">高さ</param>
    /// <param name="w">横</param>
    /// <returns></returns>
    public IUnit GetUnitAt(int h, int w)
    {
        if (h < 0 || h >= MapHeight || w < 0 || w >= MapWidth)
        {
            return null;
        }
        return _mapGrid[h, w];
    }

    /// <summary>
    /// 指定座標のユニットを削除します（座標が有効なら null を代入）。
    /// </summary>
    public void RemoveUnitAt(int h, int w)
    {
        if (h < 0 || h >= MapHeight || w < 0 || w >= MapWidth) return;
        var unit = _mapGrid[h, w];
        if (unit != null)
        {
            _unitPositions.Remove(unit);
        }
        _mapGrid[h, w] = null;
    }

    /// <summary>
    /// 指定した座標にいるユニットに対して移動を行う。
    /// ユニットが
    /// </summary>
    public async UniTask<bool> TryMoveUnit(int count, int srcH, int srcW)
    {
        // ユニットゲット
        var unit = _mapGrid[srcH, srcW];
        if(unit == null)
        {
            throw new NullReferenceException($"その位置にユニットはいません。h:{srcH}, w:{srcW}");
        }

        // 始点チェック
        if (srcH < 0 || srcH >= MapHeight || srcW < 0 || srcW >= MapWidth)
        {
            throw new NullReferenceException("始点がおかしいです。");
        }

        // count回数分、動くのを繰り返す
        for(int i = 0; i < count; i++)
        {
            // 上下に動くか、左に進むか計算する。
            // 現在のマスの左が空ならば進む。
            Debug.Log("現在のマス; h:" + srcH + ", w:" + srcW);
            if(GetUnitAt(srcH, srcW - 1) == null)
            {
                Debug.Log("左動き");
                // 一番左なので行動終了
                if (srcW == 0)
                {
                    break;
                }
                
                vector = MoveDirections.MoveLeft;
                _mapGrid[srcH, srcW - 1] = unit;
                _mapGrid[srcH, srcW] = null;
                await unit.Move(srcH, srcW - 1);
                srcW = srcW - 1;
                _unitPositions[unit] = (srcH, srcW);
            }
            else if(GetUnitAt(srcH - 1, srcW) == null)
            {
                Debug.Log("下動き");
                vector = MoveDirections.MoveDown;
                _mapGrid[srcH - 1, srcW] = unit;
                _mapGrid[srcH, srcW] = null;
                await unit.Move(srcH - 1, srcW);
                srcH = srcH - 1;
                _unitPositions[unit] = (srcH, srcW);
            }
            else if(GetUnitAt(srcH + 1, srcW) == null)
            {
                Debug.Log("上動き");
                vector = MoveDirections.MoveUp;
                _mapGrid[srcH + 1, srcW] = unit;
                _mapGrid[srcH, srcW] = null;
                await unit.Move(srcH + 1, srcW);
                srcH = srcH + 1;
                _unitPositions[unit] = (srcH, srcW);
            }
        }

        return true;
    }

    /// <summary>
    /// ユニットを指定位置に配置します（テストや初期配置用）。範囲外や既存ユニットがある場合は false。
    /// </summary>
    public bool PlaceUnitAt(IUnit unit, int h, int w)
    {
        if (unit == null) return false;
        if (h < 0 || h >= MapHeight || w < 0 || w >= MapWidth) return false;
        if (_mapGrid[h, w] != null) return false;
        _mapGrid[h, w] = unit;
        _unitPositions[unit] = (h, w);
        return true;
    }

    private List<IUnit> GetUnitsInMapOrderSnapshot()
    {
        return _unitPositions
            .OrderBy(pair => pair.Value.w)
            .ThenBy(pair => pair.Value.h)
            .Select(pair => pair.Key)
            .ToList();
    }

    /// <summary>
    /// ユニットを行動させる
    /// </summary>
    [Button("動かす")]
    public async UniTask MoveUnit()
    {
        foreach (var unit in GetUnitsInMapOrderSnapshot())
        {
            if (!_unitPositions.ContainsKey(unit))
            {
                continue;
            }

            if (unit.CanMove())
            {
                await unit.MoveTurn();
            }
        }
    }

    public async UniTask ExecuteTurnStartActions()
    {
        foreach (var unit in GetUnitsInMapOrderSnapshot())
        {
            if (_unitPositions.ContainsKey(unit))
            {
                await unit.OnTurnStart();
            }
        }
    }

    public async UniTask ExecuteTurnEndActions()
    {
        foreach (var unit in GetUnitsInMapOrderSnapshot())
        {
            if (_unitPositions.ContainsKey(unit))
            {
                await unit.OnTurnEnd();
            }
        }
    }

    public void ResetMap()
    {
        _mapGrid = new IUnit[MapHeight, MapWidth];
        _unitPositions.Clear();
    }
}
