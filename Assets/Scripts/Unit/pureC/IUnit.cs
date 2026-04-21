using Cysharp.Threading.Tasks;
using UnityEngine;

public interface IUnit : IDamagable
{
    /// <summary>
    /// 初期位置設定用
    /// </summary>
    /// <param name="h">縦方向の座標</param>
    /// <param name="w">横方向の座標</param>
    /// <returns></returns>
    public void Initialize(int h, int w);

    /// <summary>
    /// 攻撃
    /// </summary>
    public UniTask Attack();

    /// <summary>
    /// 移動できるか
    /// </summary>
    public bool CanMove();

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
    public UniTask Specific();

    public int GetMoveHeight();

    public int GetMoveWidth();
    public int GetHeight();
    public int GetWidth();

    public BattleStatus GetStatus();
    
    /// <summary>
    /// ターン開始時の行動
    /// </summary>
    public UniTask OnTurnStart();

    /// <summary>
    /// ターン終了時の行動
    /// </summary>
    public UniTask OnTurnEnd();

    /// <summary>
    /// そのユニットの行動を定義する
    /// この中で移動、攻撃を行う。
    /// </summary>
    /// <returns></returns>
    public UniTask MoveTurn();

    public UniTask<(int damage, bool isDeath)> Damage(int damage);
}
