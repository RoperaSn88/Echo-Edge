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

    private BattleStatus battleStatus;

    public BaseUnit(BaseUnitView unit, int h, int w)
    {
        _view = unit;
        Initialize(h,w);
    }

    public void RegistarStatus(BattleStatus status)
    {
        status.Initialize();
        battleStatus = status;
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
        BattleManager.RegisterEnemy(battleStatus);
        await _view.WaitAttack();
        Time.timeScale = 0.001f;
        var damageValue = await BattleManager.PlayerDamage();
        Time.timeScale = 1.0f;
        
        UIPresenter.Instance.AppearDamageText($"{damageValue.damage}", PlayerController.Instance.transform.position).Forget();
        
        await UniTask.Delay(TimeSpan.FromSeconds(1.0f));
        
        BattleManager.ResetQTE();
        await CameraManager.Instance.ActResetCameraTarget();
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