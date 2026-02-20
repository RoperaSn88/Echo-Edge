using System.Data;
using UnityEngine;

public class BaseUnit: IUnit
{
    private int height;
    public int Height => height;
    private int width;
    public int Width => width;

    public BaseUnit(int h, int w)
    {
        Initialize(h,w);
    }

    public void Initialize(int h, int w)
    {
        height = h;
        width = w;
    }

    public void Attack()
    {
        
    }

    public void Move(int x, int y)
    {
        
    }
}