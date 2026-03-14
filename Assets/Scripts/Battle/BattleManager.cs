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

    public static void RegistarEnemy(BattleStatus targetStatus)
    {
        _enemyStatus = targetStatus;
    }

    public static void RegistarPlayer(BattleStatus targetStatus)
    {
        _playerStatus = targetStatus;
    }

    public async static UniTask<(int damage, bool isDeath)> EnemyDamage()
    {
        var QTEObject = (QTEPresenter)await UIPresenter.Instance.QtePool.GetPooledObject();
        var result = QTEObject.Result;
        QTEObject.Release();
        return _enemyStatus.Damage((int)(_playerStatus.Attack * result));
    }

    public static (int damage, bool isDeath) PlayerDamage()
    {
        return _playerStatus.Damage(_enemyStatus.Attack);
    }
}
