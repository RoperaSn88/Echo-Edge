using Cysharp.Threading.Tasks;
using Unit.pureC.Unit;

public interface IUnitAction
{
    /// <summary>
    /// 攻撃の処理
    /// </summary>
    public UniTask Attack();
    
    /// <summary>
    /// 攻撃をする前の処理
    /// </summary>
    /// <returns></returns>
    public UniTask BeforeAttack();

    /// <summary>
    /// 死亡時の処理
    /// </summary>
    public UniTask Dead();

    /// <summary>
    /// 移動を含めない行動の処理
    /// </summary>
    public UniTask<EnemyMoveKinds> Act(int selfHeight, int selfWidth);

    /// <summary>
    /// 特殊行動の処理
    /// </summary>
    public UniTask Specific(int selfHeight, int selfWidth);
    
    /// <summary>
    /// 攻撃をする前の処理
    /// </summary>
    /// <returns></returns>
    public UniTask BeforeSpecific();
    
    /// <summary>
    /// ターン開始時の処理
    /// </summary>
    public UniTask OnTurnStart();

    /// <summary>
    /// ターン終了時の処理
    /// </summary>
    public UniTask OnTurnEnd();

    /// <summary>
    /// ダメージの処理
    /// </summary>
    /// <returns></returns>
    public UniTask Damage();
}
