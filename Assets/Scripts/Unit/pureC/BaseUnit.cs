using System.Data;
using UnityEngine;

public class BaseUnit: IUnit
{
    private int height;
    public int Height => height;
    private int width;
    public int Width => width;

    private int moveHeight;
    public int MoveHeight => moveHeight;

    private int moveWidth;
    public int MoveWidth => moveWidth;

    private BaseUnitView _view;

    public BaseUnit(BaseUnitView unit, int h, int w, int mh, int mw)
    {
        _view = unit;
        Initialize(h,w,mh,mw);
    }

    public void Initialize(int h, int w, int mh, int mw)
    {
        height = h;
        width = w;
        moveHeight = mh;
        moveWidth = mw;
        MapManager.Instance.RegisterUnit(this,h,w);
    }

    public void Attack()
    {
        
    }

    public void Move(int x, int y)
    {
        Debug.Log("moving");
        _view.Move();
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