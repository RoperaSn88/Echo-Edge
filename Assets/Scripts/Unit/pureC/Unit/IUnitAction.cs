using Cysharp.Threading.Tasks;

public interface IUnitAction
{
    /// <summary>
    /// 攻撃の処理
    /// </summary>
    public UniTask Attack();

    /// <summary>
    /// 死亡時の処理
    /// </summary>
    public UniTask Dead();

    /// <summary>
    /// 移動を含めない行動の処理
    /// </summary>
    public UniTask Act(int selfHeight, int selfWidth);

    /// <summary>
    /// 特殊行動の処理
    /// </summary>
    public UniTask Specific(int selfHeight, int selfWidth);
    
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
