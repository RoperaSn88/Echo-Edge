using Cysharp.Threading.Tasks;
using UnityEngine;

public interface IUnit 
{
    public UniTask Initialize(int h, int w, int mh, int mw);
    /// <summary>
    /// 攻撃
    /// </summary>
    public void Attack();

    /// <summary>
    /// 移動
    /// </summary>
    /// <param name="h">移動する縦の大きさ</param>
    /// <param name="w">移動する横の大きさ</param>
    public UniTask Move(int h,int w);

    /// <summary>
    /// なんかの技
    /// たぶんViewでやるべき
    /// </summary>
    public void Specific();

    public int GetMoveHeight();

    public int GetMoveWidth();

    public BattleStatus GetStatus();
}
