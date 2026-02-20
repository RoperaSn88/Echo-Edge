using UnityEngine;

public interface IUnit 
{
    public void Initialize(int h, int w);
    /// <summary>
    /// 攻撃
    /// </summary>
    public void Attack();

    /// <summary>
    /// 移動
    /// </summary>
    /// <param name="h">移動する縦の大きさ</param>
    /// <param name="w">移動する横の大きさ</param>
    public void Move(int h,int w);
}
