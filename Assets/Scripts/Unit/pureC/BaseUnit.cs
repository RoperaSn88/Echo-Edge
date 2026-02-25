using System;
using Cysharp.Threading.Tasks;
using UnityEngine;


[System.Serializable]
public class BaseUnit: IUnit
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

    public BaseUnit(BaseUnitView unit, int h, int w, int mh, int mw)
    {
        _view = unit;
        Initialize(h,w,mh,mw);
    }

    public async UniTask Initialize(int h, int w, int mh, int mw)
    {
        height = h;
        width = w;
        moveHeight = mh;
        moveWidth = mw;
        await UniTask.Delay(TimeSpan.FromMilliseconds(1000));
        MapManager.Instance.RegisterUnit(this,h,w);
    }

    public void Attack()
    {
        
    }

    public void Specific()
    {
        
    }

    public void Move(int y, int x)
    {
        width += x;
        height += y;
        _view.Move(y, x);
    }

    public int GetMoveHeight()
    {
        return moveHeight;
    }

    public int GetMoveWidth()
    {
        return moveWidth;
    }
}