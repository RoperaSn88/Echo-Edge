using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class building : IUnit
{
    [SerializeField]
    private int height;
    public int Height => height;

    [SerializeField]
    private int width;
    public int Width => width;

    public void Initialize(int h, int w)
    {
        height = h;
        width = w;
        MapManager.Instance.RegisterUnit(this,h,w);
    }

    public void Attack()
    {
        throw new System.NotImplementedException();
    }

    public bool CanMove()
    {
        return false;
    }

    public async UniTask Move(int x, int y)
    {
        throw new System.NotImplementedException();
    }

    public void Specific()
    {
        throw new System.NotImplementedException();
    }

    public int GetMoveHeight()
    {
        throw new System.NotImplementedException();
    }

    public int GetMoveWidth()
    {
        throw new System.NotImplementedException();
    }

    public int GetHeight()
    {
        throw new System.NotImplementedException();
    }

    public int GetWidth()
    {
        throw new System.NotImplementedException();
    }

    public BattleStatus GetStatus()
    {
        throw new System.NotImplementedException();
    }

    public (int damage, bool isDeath) Damage(int damage)
    {
        throw new NotImplementedException();
    }
}