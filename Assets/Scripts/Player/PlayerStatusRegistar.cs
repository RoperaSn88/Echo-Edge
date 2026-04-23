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
            return;
        }

        if (!PlayerSwordParameterSaveManager.HasPlayerStatusData())
        {
            PlayerSwordParameterHolder.SetPlayerStatus(_battleStatus);
        }
        if (!PlayerSwordParameterSaveManager.HasSwordStatusData())
        {
            PlayerSwordParameterHolder.SetSwordStatus(0, 0, _battleStatus.Move);
        }
        var playerParameter = PlayerSwordParameterHolder.PlayerStatus;
        var swordParameter = PlayerSwordParameterHolder.SwordStatus;
        var battleParameter = PlayerSwordParameterHolder.GetBattleStatus();
        _battleStatus.SetStatus(
            battleParameter.HP,
            battleParameter.Attack,
            playerParameter.Defend,
            swordParameter.ReflectCount,
            _battleStatus.MovePattern,
            _battleStatus.Experience,
            _battleStatus.Energy
        );
        BattleManager.RegisterPlayer(_battleStatus);
    }
}
