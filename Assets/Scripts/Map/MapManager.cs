using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class MapManager
{
    public static MapManager Instance;
    
    /// <summary>
    /// マップの縦
    /// </summary>
    private const int MapHeight = 9;

    /// <summary>
    /// マップの横
    /// </summary>
    private const int MapWidth = 14;

    /// <summary>
    /// マップ全体
    /// </summary>
    private IUnit[,] _mapGrid = new IUnit[MapHeight, MapWidth];

    /// <summary>
    /// キャッシュ用
    /// </summary>
    private Vector3 vector;

    public MapManager()
    {
        Instance = this;
    }

    public void RegisterUnit(IUnit unit, int h, int w)
    {
        if(_mapGrid[h, w] != null)
        {
            throw new InvalidOperationException("指定したマスにはunitがいます");
        }
        _mapGrid[h, w] = unit;
    }

    /// <summary>
    /// 指定座標のユニットを取得します。範囲外なら null を返します。
    /// </summary>
    /// <param name="h">高さ</param>
    /// <param name="w">横</param>
    /// <returns></returns>
    public IUnit GetUnitAt(int h, int w)
    {
        if (h < 0 || h >= MapHeight || w < 0 || w >= MapWidth) return null;
        return _mapGrid[h, w];
    }

    /// <summary>
    /// 指定座標のユニットを削除します（座標が有効なら null を代入）。
    /// </summary>
    public void RemoveUnitAt(int h, int w)
    {
        if (h < 0 || h >= MapHeight || w < 0 || w >= MapWidth) return;
        _mapGrid[h, w] = null;
    }

    /// <summary>
    /// src -> dst にユニットを移動します。移動先が範囲外または非 null の場合は false を返します。
    /// 成功時は内部的にグリッドを更新し、ユニットの `Move` を呼び出して内部処理を行わせます。
    /// </summary>
    public async UniTask<bool> TryMoveUnit(int count, int srcH, int srcW)
    {
        // ユニットゲット
        var unit = _mapGrid[srcH, srcW];

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
            if(GetUnitAt(srcH - 1, srcW) == null)
            {
                vector = MoveDirections.MoveLeft;
                _mapGrid[srcH - 1, srcW] = unit;
                _mapGrid[srcH, srcW] = null;
                await unit.Move(srcH - 1, srcW);
            }
            else if(GetUnitAt(srcH, srcW - 1) == null)
            {
                vector = MoveDirections.MoveDown;
                _mapGrid[srcH, srcW - 1] = unit;
                _mapGrid[srcH, srcW] = null;
                await unit.Move(srcH, srcW - 1);
            }
            else if(GetUnitAt(srcH, srcW + 1) == null)
            {
                vector = MoveDirections.MoveUp;
                _mapGrid[srcH, srcW + 1] = unit;
                _mapGrid[srcH, srcW] = null;
                await unit.Move(srcH, srcW + 1);
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
        return true;
    }

    /// <summary>
    /// ユニットを行動させる
    /// </summary>
    public void MoveUnit()
    {
        for(int i = 0; i < MapHeight; i++)
        {
            for(int j = 0; j < MapWidth; j++)
            {
                var unit = _mapGrid[i,j];
                if(unit != null)
                {
                    Debug.Log(TryMoveUnit(unit.GetStatus().Move, unit.GetMoveHeight(), unit.GetMoveWidth()));
                }
            }
        }
        Debug.Log("おわり");
    }
}
