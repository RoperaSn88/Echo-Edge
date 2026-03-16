using Cysharp.Threading.Tasks;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    [SerializeField]
    private static BattleStatus _playerStatus;

    public BattleStatus PlayerStatus => _playerStatus;

    [SerializeField]
    private static BattleStatus _enemyStatus;
    
    public BattleStatus EnemyStatus => _enemyStatus;

    /// <summary>
    /// QTEを実行するためのフラグ
    /// 1回の攻撃につき一度だけ発動する。
    /// </summary>
    private static bool _QTEFlug = false;

    private static float _result;

    public static void RegisterEnemy(BattleStatus targetStatus)
    {
        _enemyStatus = targetStatus;
    }

    public static void RegisterPlayer(BattleStatus targetStatus)
    {
        _playerStatus = targetStatus;
    }

    public static void ResetQTE()
    {
        _QTEFlug = false;
    }

    public async static UniTask<(int damage, bool isDeath)> EnemyDamage()
    {
        if (!_QTEFlug)
        {
            _result = await UIPresenter.Instance.AppearQTE();
            _QTEFlug = true;
        } 
        return _enemyStatus.Damage((int)(_playerStatus.Attack * _result));
    }

    public static (int damage, bool isDeath) PlayerDamage()
    {
        return _playerStatus.Damage(_enemyStatus.Attack);
    }
}
