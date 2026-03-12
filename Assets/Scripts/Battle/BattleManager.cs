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

    public static void EnemyDamage()
    {
        _enemyStatus.Damage(_playerStatus.attack);
    }
}
