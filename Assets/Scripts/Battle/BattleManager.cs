using Cysharp.Threading.Tasks;
using UI.QTE;
using UI;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    [SerializeField]
    private static BattleStatus _playerStatus;

    public static BattleStatus PlayerStatus => _playerStatus;

    [SerializeField]
    private static BattleStatus _enemyStatus;
    
    public static BattleStatus EnemyStatus => _enemyStatus;

    /// <summary>
    /// QTEを実行するためのフラグ
    /// 1回の攻撃につき一度だけ発動する。
    /// </summary>
    private static bool _QTEFlug = false;

    private static float _qteResult;

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
            _qteResult = await UIPresenter.Instance.AppearQTE(QTEKinds.Attack);
            _QTEFlug = true;
        } 
        return await _enemyStatus.Damage((int)(_playerStatus.Attack * _qteResult));
    }

    public async static UniTask<(int damage, bool isDeath)> PlayerDamage(float rate)
    {
        if (!_QTEFlug)
        {
            _qteResult = await UIPresenter.Instance.AppearQTE(QTEKinds.Defend);
            _QTEFlug = true;
        } 

        var result = await _playerStatus.Damage((int)(_enemyStatus.Attack * rate * _qteResult));
        
        PlayerStatusPresenter.Instance.SetPlayerHP(_playerStatus.HP, _playerStatus.MaxHP);
        
        return result;
    }
}
