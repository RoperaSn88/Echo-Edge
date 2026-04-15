using Cysharp.Threading.Tasks;
using UI.QTE;
using UI;
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
            _result = await UIPresenter.Instance.AppearQTE(QTEKinds.Attack);
            _QTEFlug = true;
        } 
        return await _enemyStatus.Damage((int)(_playerStatus.Attack * _result));
    }

    public async static UniTask<(int damage, bool isDeath)> PlayerDamage()
    {
        if (!_QTEFlug)
        {
            _result = await UIPresenter.Instance.AppearQTE(QTEKinds.Defend);
            _QTEFlug = true;
        } 
        var result = await _playerStatus.Damage((int)(_enemyStatus.Attack * _result));
        
        PlayerStatusPresenter.Instance.SetPlayerHP(_playerStatus.HP, _playerStatus.MaxHP);
        
        return result;
    }
}
