using Unity.VisualScripting;
using UnityEngine;

public static class PlayerStatusRegistar
{
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public static BattleStatus SetPlayerStatus()
    {
        BattleStatus battleStatus = new BattleStatus(100,20,0,1,MovePattern.Invalid,0,0);

        // 初回起動の時のステータス設定
        if(!PlayerSwordParameterSaveManager.HasPlayerStatusData())
        {
            PlayerSwordParameterHolder.SetPlayerStatus(battleStatus);
        }
        if (!PlayerSwordParameterSaveManager.HasSwordStatusData())
        {
            PlayerSwordParameterHolder.SetSwordStatus(0, 0, battleStatus.Move);
        }
        var battleParameter = PlayerSwordParameterHolder.GetBattleStatus();
        battleStatus.SetStatus(
            battleParameter.HP + PlayerSwordParameterHolder.SwordStatus.HP,
            battleParameter.Attack + PlayerSwordParameterHolder.SwordStatus.Attack,
            battleParameter.Defend,
            battleParameter.Move,
            battleStatus.MovePattern,
            battleParameter.Experience,
            battleStatus.Energy
        );
        battleStatus.SetLevel(battleParameter.Level);
        return battleStatus;
    }
}
