using Cysharp.Threading.Tasks;

public interface IUnitAttackAndDead
{
    /// <summary>
    /// 攻撃の処理
    /// </summary>
    public UniTask Attack();

    /// <summary>
    /// 死亡時の処理
    /// </summary>
    public void Dead();
}
