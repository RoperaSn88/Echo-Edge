using Unity.VisualScripting;
using UnityEngine;

public class PlayerStatusRegistar : MonoBehaviour
{
    [SerializeField]
    private BattleStatus _battleStatus;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var baseHP = _battleStatus.HP;
        var baseAttack = _battleStatus.Attack;
        PlayerSwordParameterHolder.SetPlayerStatus(baseHP, baseAttack);
        var battleParameter = PlayerSwordParameterHolder.GetBattleStatus();
        _battleStatus.SetStatus(
            battleParameter.hp,
            battleParameter.attack,
            _battleStatus.Defend,
            _battleStatus.Move,
            _battleStatus.MovePattern,
            _battleStatus.Experience,
            _battleStatus.Energy
        );
        BattleManager.RegisterPlayer(_battleStatus);
    }
}
