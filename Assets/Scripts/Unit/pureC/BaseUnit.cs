using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Unit.pureC.Unit;


[System.Serializable]
public　class BaseUnit: IUnit, IDamagable
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

    private BattleStatus _battleStatus;
    private IUnitAction _unitAction;

    public BaseUnit(int h, int w)
    {
        Initialize(h, w);
    }

    /// <summary>
    /// CSV から エネミー ID に対応するステータスを読み込む
    /// </summary>
    /// <param name="enemyId">EnemyInfo.csv の ID</param>
    public async UniTask LoadStatus(EnemyKinds enemyId)
    {
        var status = new BattleStatus();
        bool loaded = await EnemyStatusLoader.TryLoad((int)enemyId, status);
        if (!loaded)
        {
            Debug.LogWarning($"enemyId {(int)enemyId} のステータスを読み込めませんでした。デフォルトステータスで起動します。");
        }
        status.Initialize();
        _battleStatus = status;
        
        // ユニット特有のアクションを行う
        _unitAction = UnitActionSelector.SelectAction(enemyId);
    }

    /// <summary>
    /// 表示用 View を紐づけ、UnitAction を生成する
    /// </summary>
    /// <param name="view">対応する BaseUnitView</param>
    public void SetView(BaseUnitView view)
    {
        _view = view;
    }

    public async void Initialize(int h, int w)
    {
        height = h;
        width = w;
        await UniTask.WaitUntil(()=> MapManager.Instance);
        MapManager.Instance.RegisterUnit(this,h,w);
    }

    public async UniTask Dead()
    {
        await _unitAction.Dead();
        // 死んだら自身のいるマスを空にする
        MapManager.Instance.RemoveUnitAt(Height, Width);
    }

    public async UniTask Attack()
    {
        BattleManager.RegisterEnemy(_battleStatus);
        await _view.WaitAttack();
        if (_unitAction == null) return;
        await _unitAction.Attack();
    }

    public async UniTask Specific()
    {
        if (_unitAction == null) return;
        await _unitAction.Specific(Height, Width);
    }

    public async UniTask OnTurnStart()
    {
        if (_unitAction == null) return;
        await _unitAction.OnTurnStart();
    }

    public async UniTask OnTurnEnd()
    {
        if (_unitAction == null) return;
        await _unitAction.OnTurnEnd();
    }

    public bool CanMove()
    {
        return true;
    }

    public async UniTask MoveTurn()
    {
        if (_battleStatus.MovePattern == MovePattern.Before)
        {
            // 攻撃をするが、遠距離か近距離かで攻撃するか変更する
            await Specific();
        }
        
        // 移動
        try
        {
            await MapManager.Instance.TryMoveUnit(_battleStatus.Move, Height, Width);
        }
        catch
        {
            
        }
        
        if (_battleStatus.MovePattern == MovePattern.After)
        {
            // 行動をする
            // いったん攻撃か
            await Specific();
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
        return _battleStatus;
    }

    public async UniTask<(int damage, bool isDeath)> Damage(int damage)
    {
        var result = await _battleStatus.Damage(damage);

        if (result.isDeath)
        {
            await Dead();
        }
            
        return result;
    }
}
