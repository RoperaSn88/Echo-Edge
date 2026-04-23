using Unity.VisualScripting;
using UnityEngine;

public class PlayerStatusRegistar : MonoBehaviour
{
    [SerializeField]
    private BattleStatus _battleStatus;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PlayerSwordParameterHolder.SetPlayerStatus(_battleStatus.HP, _battleStatus.Attack);
        var battleStatus = PlayerSwordParameterHolder.GetBattleStatus();
        _battleStatus.SetStatus(
            battleStatus.hp,
            battleStatus.attack,
            _battleStatus.Defend,
            _battleStatus.Move,
            _battleStatus.MovePattern,
            _battleStatus.Experience,
            _battleStatus.Energy
        );
        BattleManager.RegisterPlayer(_battleStatus);
    }
}
