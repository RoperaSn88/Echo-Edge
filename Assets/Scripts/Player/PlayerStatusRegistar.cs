using Unity.VisualScripting;
using UnityEngine;

public class PlayerStatusRegistar : MonoBehaviour
{
    [SerializeField]
    private BattleStatus _battleStatus;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (_battleStatus == null)
        {
            _battleStatus = new BattleStatus();
        }

        if (!PlayerSwordParameterSaveManager.HasPlayerStatusData())
        {
            PlayerSwordParameterHolder.SetPlayerStatus(_battleStatus);
        }
        if (!PlayerSwordParameterSaveManager.HasSwordStatusData())
        {
            PlayerSwordParameterHolder.SetSwordStatus(0, 0, _battleStatus.Move);
        }
        var battleParameter = PlayerSwordParameterHolder.GetBattleStatus();
        _battleStatus.SetStatus(
            battleParameter.HP,
            battleParameter.Attack,
            battleParameter.Defend,
            battleParameter.Move,
            _battleStatus.MovePattern,
            battleParameter.Experience,
            _battleStatus.Energy
        );
        _battleStatus.SetLevel(battleParameter.Level);
        BattleManager.RegisterPlayer(_battleStatus);
    }
}
