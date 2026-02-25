using System;
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
    public bool TryMoveUnit(int srcH, int srcW, int dstH, int dstW)
    {
        // 範囲チェック
        if (srcH < 0 || srcH >= MapHeight || srcW < 0 || srcW >= MapWidth)
        {
            Debug.Log("始点がおかしい");
            return false;
        }
        if (dstH < 0 || dstH >= MapHeight || dstW < 0 || dstW >= MapWidth)
        {
            Debug.Log("終点がおかしい");
            return false;
        }

        var unit = _mapGrid[srcH, srcW];
        if (unit == null)
        {
            Debug.Log("ユニットがいない");
            return false; 
        }
        
        var dest = _mapGrid[srcH - dstH, srcW - dstW];
        if (dest != null)
        {
            Debug.Log("着地地点がおかしい");
            return false;
        }

        // 移動実行
        _mapGrid[srcH - dstH, srcW - dstW] = unit;
        _mapGrid[srcH, srcW] = null;

        // ユニット側に内部処理があれば通知（位置情報をユニット側で管理するならここで更新させる）
        try
        {
            unit.Move(dstH, dstW);
        }
        catch (Exception)
        {
            // unit の Move が必須でない実装もあり得るので安全に呼び出す（例外は無視）
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
                    Debug.Log(TryMoveUnit(i, j, unit.GetMoveHeight(), unit.GetMoveWidth()));
                }
            }
        }
        Debug.Log("おわり");
    }
}
