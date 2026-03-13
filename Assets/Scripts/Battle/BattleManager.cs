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

    public static (int damage, bool isDeath) EnemyDamage()
    {
        return _enemyStatus.Damage(_playerStatus.attack);
    }

    public static (int damage, bool isDeath) PlayerDamage()
    {
        return _playerStatus.Damage(_enemyStatus.attack);
    }
}
