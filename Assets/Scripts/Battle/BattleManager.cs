using UnityEngine;

public class BattleManager : MonoBehaviour
{
    [SerializeField]
    private BaseStatus _playerStatus;

    public BaseStatus PlayerStatus => _playerStatus;

    [SerializeField]
    private BaseStatus _enemyStatus;
    
    public BaseStatus EnemyStatus => _enemyStatus;

    public void RegistarEnemy(BaseStatus targetStatus)
    {
        _enemyStatus = targetStatus;
    }

    
}
