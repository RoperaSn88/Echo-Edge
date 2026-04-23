public struct SwordParameter
{
    public int HP;
    public int Attack;

    public SwordParameter(int hp, int attack)
    {
        HP = hp;
        Attack = attack;
    }
}

public static class PlayerSwordParameterHolder
{
    public static BattleStatus PlayerStatus { get; private set; }
    public static SwordParameter SwordStatus { get; private set; }
    
    static PlayerSwordParameterHolder()
    {
        // 後でセーブ/ロード機能から読みとる
        PlayerStatus = new BattleStatus();
        PlayerStatus.SetStatus(0, 0, 0, 0, MovePattern.Before, 0, 0);
        SwordStatus = new SwordParameter(0, 0);
    }

    public static void SetPlayerStatus(BattleStatus playerStatus)
    {
        if (playerStatus == null)
        {
            PlayerStatus.SetStatus(0, 0, 0, 0, MovePattern.Before, 0, 0);
            return;
        }

        PlayerStatus.SetStatus(
            playerStatus.HP,
            playerStatus.Attack,
            playerStatus.Defend,
            playerStatus.Move,
            playerStatus.MovePattern,
            playerStatus.Experience,
            playerStatus.Energy
        );
    }

    public static void SetSwordStatus(int hp, int attack)
    {
        SwordStatus = new SwordParameter(hp, attack);
    }

    public static (int hp, int attack) GetBattleStatus()
    {
        return (PlayerStatus.HP + SwordStatus.HP, PlayerStatus.Attack + SwordStatus.Attack);
    }
}
