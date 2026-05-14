using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Unit.pureC.Unit;


[System.Serializable]
public　class BaseUnit: IEnemyUnit, IDamagable
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

    private IUnitView _view;

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
    /// <param name="view">対応する IUnitView</param>
    public void SetView(IUnitView view)
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
        Debug.Log("EnemyWillDead");
        await _unitAction.Dead();
        Debug.Log("EnemyDeaded");
        var h = Height;
        var w = Width;
        // 死んだら自身のいるマスを空にする
        MapManager.Instance.RemoveUnitAt(h, w);
        // ゲームクリア判定
        GameClearManager.OnEnemyDead(h, w, _battleStatus.Experience);
    }

    public async UniTask Attack()
    {
        BattleManager.RegisterEnemy(_battleStatus);
        await UniTask.WhenAll(
            _view.WaitToCameraZoom(),
            _unitAction.BeforeAttack()
        );

        await _view.WaitAttackAnim();
        
        await _unitAction.Attack();
    }

    public async UniTask Specific()
    {
        BattleManager.RegisterEnemy(_battleStatus);

        UniTask action;
        if (_unitAction is IFlyingUnit flyingUnit1)
        {
            if(flyingUnit1.IsFlying) action = flyingUnit1.WaitFlyingMessage();
            else action = flyingUnit1.WaitToFlyMessage();
        }
        else
        {
            action = _unitAction.BeforeSpecific();
        }
        
        await UniTask.WhenAll(
            _view.WaitToCameraZoom(),
            action
        );

        // 飛行可能なユニットは飛行状態に応じてアニメーションを切り替える。
        // IsFlying が true（既に飛行中）→ ビームアニメ、false（地上）→ 飛び上がりアニメ
        if (_unitAction is IFlyingUnit flyingUnit)
        {
            if (_view is IFlyingUnitView flyingView)
            {
                if (flyingUnit.IsFlying)
                {
                    await flyingView.WaitAnimAfterFlying();
                }
                else
                {
                    await flyingView.WaitFlyAnim();
                }
            }
            else
            {
                Debug.LogWarning($"{_view.GetType().Name} は IFlyingUnitView を実装していません。WaitSpecificAnim にフォールバックします。");
                await _view.WaitSpecificAnim();
            }
        }
        else
        {
            await _view.WaitSpecificAnim();
        }
        
        await _unitAction.Specific(Height, Width);
    }

    public async UniTask Act()
    {
        if (_unitAction == null) return;
        var pattern = await _unitAction.Act(Height, Width);
        switch (pattern)
        {
            case EnemyMoveKinds.Attack:
                await Attack();
                break;
            case EnemyMoveKinds.Specific:
                await Specific();
                break;
        }
    }

    public async UniTask OnTurnStart()
    {
        _battleStatus?.TickBuffs();
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
            await Act();
            await MessageManager.Instance.DisappearMessage();
        }
        
        // 移動
        try
        {
            // もし飛行中ならナシ
            if (_unitAction is IFlyingUnit fly)
            {
                if(!fly.IsFlying) await MapManager.Instance.TryMoveUnit(_battleStatus.Move, Height, Width);
            }
            else
            {
                await MapManager.Instance.TryMoveUnit(_battleStatus.Move, Height, Width);
            }
        }
        catch
        {
            
        }
        
        if (_battleStatus.MovePattern == MovePattern.After)
        {
            // 行動をする
            // いったん攻撃か
            await Act();
            await MessageManager.Instance.DisappearMessage();
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
