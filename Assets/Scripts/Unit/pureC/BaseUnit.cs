using System;
using Cysharp.Threading.Tasks;
using UnityEngine;


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

    private BaseStatus baseStatus;

    private BattleStatus battleStatus;

    public BaseUnit(BaseUnitView unit, int h, int w)
    {
        _view = unit;
        Initialize(h,w);
    }

    public void RegistarStatus(BaseStatus status)
    {
        baseStatus = status;
        battleStatus = new BattleStatus(baseStatus);
    }

    public async void Initialize(int h, int w)
    {
        height = h;
        width = w;
        await UniTask.WaitUntil(()=> MapManager.Instance);
        MapManager.Instance.RegisterUnit(this,h,w);
    }

    public void Attack()
    {
        
    }

    public void Specific()
    {
        
    }

    public bool CanMove()
    {
        return true;
    }

    public async UniTask Move(int y, int x)
    {
        Debug.Log("動くよ");
        height = y;
        width = x;
        await _view.Move(y, x);
        Debug.Log("動いた");
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
        return battleStatus.Damage(damage);
    }
}