using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;


[System.Serializable]
public class BaseUnit: IUnit, IDamagable
{
    [SerializeField]
    private int height;
    public int Height => height;

    [SerializeField]
    private int width;
    public int Width => width;

    [SerializeField]
    private int moveHeight;
    public int MoveHeight => moveHeight;

    [SerializeField]
    private int moveWidth;
    public int MoveWidth => moveWidth;

    private BaseUnitView _view;
    private Image _image;

    private BattleStatus battleStatus;
    private IUnitAttackAndDead _attackAndDead;

    public BaseUnit(BaseUnitView unit, int h, int w, Image image)
    {
        _view = unit;
        _image = image;
        Initialize(h,w);
    }

    public void RegistarStatus(BattleStatus status)
    {
        status.Initialize();
        battleStatus = status;
        _attackAndDead = new UnitAttackAndDead(battleStatus, _view, _image);
    }

    public async void Initialize(int h, int w)
    {
        height = h;
        width = w;
        await UniTask.WaitUntil(()=> MapManager.Instance);
        MapManager.Instance.RegisterUnit(this,h,w);
    }

    public async UniTask Attack()
    {
        await _attackAndDead.Attack();
    }

    public async UniTask Specific()
    {
        
    }

    public bool CanMove()
    {
        return true;
    }

    public async UniTask MoveTurn()
    {
        if (battleStatus.MovePattern == MovePattern.Before)
        {
            // 行動をする
            // いったん攻撃か
            await Attack();
        }
        
        // 移動
        try
        {
            await MapManager.Instance.TryMoveUnit(battleStatus.Move, Height, Width);
        }
        catch
        {
            
        }
        
        if (battleStatus.MovePattern == MovePattern.After)
        {
            // 行動をする
            // いったん攻撃か
            await Attack();
        }
    } 

    public async UniTask Move(int y, int x)
    {
        height = y;
        width = x;
        await _view.Move(y, x);
    }

    public int GetMoveHeight()
    {
        return Height;
    }

    public int GetMoveWidth()
    {
        return Width;
    }

    public int GetHeight()
    {
        return Height;
    }

    public int GetWidth()
    {
        return Width;
    }

    public BattleStatus GetStatus()
    {
        return battleStatus;
    }

    public (int damage, bool isDeath) Damage(int damage)
    {
        var result =  battleStatus.Damage(damage);

        if (result.isDeath)
        {
            // 死んだら自身のいるマスを空にする
            MapManager.Instance.RemoveUnitAt(Height, Width);
        }
            
        return result;
    }
}