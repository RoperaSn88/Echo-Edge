using Unity.VisualScripting;
using UnityEngine;

public class PlayerStatusRegistar : MonoBehaviour
{
    [SerializeField]
    private BattleStatus _battleStatus;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (!PlayerSwordParameterSaveManager.HasPlayerStatusData())
        {
            PlayerSwordParameterHolder.SetPlayerStatus(_battleStatus);
        }
        var playerParameter = PlayerSwordParameterHolder.PlayerStatus;
        var battleParameter = PlayerSwordParameterHolder.GetBattleStatus();
        _battleStatus.SetStatus(
            battleParameter.HP,
            battleParameter.Attack,
            playerParameter.Defend,
            playerParameter.ReflectCount,
            _battleStatus.MovePattern,
            _battleStatus.Experience,
            _battleStatus.Energy
        );
        BattleManager.RegisterPlayer(_battleStatus);
    }
}
