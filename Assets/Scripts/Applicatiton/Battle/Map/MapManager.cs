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
    /// マップ配置のベースとなるオブジェクトのTransform
    /// </summary>
    [SerializeField]
    private Transform _baseTransform;

    /// <summary>
    /// マップ配置のベース座標を返す
    /// </summary>
    public Vector3 GetBasePos()
    {
        if (_baseTransform == null)
            throw new InvalidOperationException("MapManager: _baseTransform が設定されていません。インスペクターで割り当ててください。");
        return _baseTransform.position;
    }

    public int Height => MapHeight;
    public int Width => MapWidth;

    public void Start()
    {
        Instance = this;
    }

    public async UniTask BuildStageFromCsv()
    {
        ResetMap();

        var placements = await StageLayoutLoader.GetPlacementsAsync(MapHeight, MapWidth);
        var initialEnemyCount = 0;
        await UniTask.WaitUntil(() => BuildingManager.Instance != null);
        await UniTask.WaitUntil(() => UnitSpawner.Instance != null);

        foreach (var placement in placements)
        {
            if (placement.objectKind == StageObjectKind.Wall)
            {
                var buildingUnit = new building();
                RegisterWall(buildingUnit, placement.height, placement.width);
                BuildingManager.Instance.SetBuilding(placement.height, placement.width);
                continue;
            }

            if (placement.objectKind == StageObjectKind.Unit)
            {
                initialEnemyCount++;
                var unit = new BaseUnit(placement.height, placement.width);
                await unit.LoadStatus(placement.enemyKind);
                UnitSpawner.Instance.SpawnView(unit, placement.enemyKind);
            }
        }

        GameClearManager.SetConditionValue(initialEnemyCount);
    }

    private void RegisterWall(IUnit wall, int h, int w)
    {
        if (_mapGrid[h, w] == null)
            _mapGrid[h, w] = wall;
        else throw new InvalidOperationException("指定したマスにはunitがいるか、範囲外です h:" + h + ", w:" + w);
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

    public bool IsInBounds(int h, int w)
    {
        return h >= 0 && h < MapHeight && w >= 0 && w < MapWidth;
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
        // 始点チェック
        if (!IsInBounds(srcH, srcW))
        {
            throw new NullReferenceException("始点がおかしいです。");
        }

        if (count <= 0) return true;

        // ユニットゲット
        var unit = _mapGrid[srcH, srcW];
        if(unit == null)
        {
            throw new NullReferenceException($"その位置にユニットはいません。h:{srcH}, w:{srcW}");
        }

        var mapSize = count * 2 + 1;
        var scoreMap = new byte[mapSize, mapSize];
        var steps = count;
        var minScore = int.MinValue / 4;
        var scoreByStep = new int[steps + 1, MapHeight, MapWidth];
        var prevH = new int[steps + 1, MapHeight, MapWidth];
        var prevW = new int[steps + 1, MapHeight, MapWidth];
        var offset = Math.Max(0, Math.Min(byte.MaxValue, count));

        for (var step = 0; step <= steps; step++)
        {
            for (var h = 0; h < MapHeight; h++)
            {
                for (var w = 0; w < MapWidth; w++)
                {
                    scoreByStep[step, h, w] = minScore;
                    prevH[step, h, w] = -1;
                    prevW[step, h, w] = -1;
                }
            }
        }

        scoreByStep[0, srcH, srcW] = 0;

        var dirH = new[] { 0, -1, 1, 0 };
        var dirW = new[] { -1, 0, 0, 1 };
        var dirScore = new[] { 2, 1, 1, -1 };

        for (var step = 1; step <= steps; step++)
        {

            for (var h = 0; h < MapHeight; h++)
            {
                for (var w = 0; w < MapWidth; w++)
                {
                    var baseScore = scoreByStep[step - 1, h, w];
                    if (baseScore == minScore) continue;

                    for (var dir = 0; dir < dirH.Length; dir++)
                    {
                        var nextH = h + dirH[dir];
                        var nextW = w + dirW[dir];
                        if (!IsInBounds(nextH, nextW)) continue;
                        if (_mapGrid[nextH, nextW] != null) continue;

                        var candidate = baseScore + dirScore[dir];
                        if (candidate <= scoreByStep[step, nextH, nextW]) continue;

                        scoreByStep[step, nextH, nextW] = candidate;
                        prevH[step, nextH, nextW] = h;
                        prevW[step, nextH, nextW] = w;
                    }
                }
            }
        }

        var hasDestination = false;
        var bestStep = -1;
        var dstH = srcH;
        var dstW = srcW;
        var bestScore = minScore;

        for (var step = 1; step <= steps; step++)
        {
            for (var h = 0; h < MapHeight; h++)
            {
                for (var w = 0; w < MapWidth; w++)
                {
                    var score = scoreByStep[step, h, w];
                    if (score == minScore) continue;

                    var localH = h - srcH + count;
                    var localW = w - srcW + count;
                    if (localH >= 0 && localH < mapSize && localW >= 0 && localW < mapSize)
                    {
                        var stored = score + offset;
                        if (stored < 0) stored = 0;
                        if (stored > byte.MaxValue) stored = byte.MaxValue;
                        if (scoreMap[localH, localW] < stored) scoreMap[localH, localW] = (byte)stored;
                    }

                    if (!hasDestination || score > bestScore || (score == bestScore && (w < dstW || (w == dstW && Math.Abs(h - srcH) < Math.Abs(dstH - srcH)))))
                    {
                        hasDestination = true;
                        bestScore = score;
                        bestStep = step;
                        dstH = h;
                        dstW = w;
                    }
                }
            }
        }

        if (!hasDestination) return true;

        var path = new List<(int h, int w)>();
        var currentH = dstH;
        var currentW = dstW;
        var currentStep = bestStep;

        while (currentStep > 0)
        {
            path.Add((currentH, currentW));
            var fromH = prevH[currentStep, currentH, currentW];
            var fromW = prevW[currentStep, currentH, currentW];
            if (fromH < 0 || fromW < 0) break;
            currentH = fromH;
            currentW = fromW;
            currentStep--;
        }

        path.Reverse();

        var moveFromH = srcH;
        var moveFromW = srcW;
        foreach (var waypoint in path)
        {
            _mapGrid[moveFromH, moveFromW] = null;
            _mapGrid[waypoint.h, waypoint.w] = unit;
            await unit.Move(waypoint.h, waypoint.w);
            moveFromH = waypoint.h;
            moveFromW = waypoint.w;
            _unitPositions[unit] = (moveFromH, moveFromW);
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

    public async UniTask<bool> TryMoveUnitTo(IUnit unit, int dstH, int dstW)
    {
        if (unit == null) return false;
        if (!IsInBounds(dstH, dstW)) return false;
        if (_mapGrid[dstH, dstW] != null) return false;
        if (!_unitPositions.TryGetValue(unit, out var src)) return false;

        _mapGrid[src.h, src.w] = null;
        _mapGrid[dstH, dstW] = unit;
        _unitPositions[unit] = (dstH, dstW);

        try
        {
            await unit.Move(dstH, dstW);
        }
        catch
        {
            _mapGrid[dstH, dstW] = null;
            _mapGrid[src.h, src.w] = unit;
            _unitPositions[unit] = src;
            return false;
        }

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

    public List<(IUnit unit, int h, int w)> GetUnitPositionsSnapshot()
    {
        return _unitPositions
            .Select(pair => (pair.Key, pair.Value.h, pair.Value.w))
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
            if (_unitPositions.ContainsKey(unit) && unit.CanMove())
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

    /// <summary>
    /// マップ上の敵ユニットの数を返します。
    /// </summary>
    public int CountEnemies()
    {
        return _unitPositions.Count;
    }

    public void ResetMap()
    {
        _mapGrid = new IUnit[MapHeight, MapWidth];
        _unitPositions.Clear();
    }
}
