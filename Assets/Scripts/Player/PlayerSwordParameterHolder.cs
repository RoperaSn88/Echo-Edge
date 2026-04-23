public struct PlayerParameter
{
    public int HP;
    public int Attack;

    public PlayerParameter(int hp, int attack)
    {
        HP = hp;
        Attack = attack;
    }
}

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
    public static PlayerParameter PlayerStatus { get; private set; }
    public static SwordParameter SwordStatus { get; private set; }
    
    static PlayerSwordParameterHolder()
    {
        PlayerStatus = new PlayerParameter(0, 0);
        SwordStatus = new SwordParameter(0, 0);
    }

    public static void SetPlayerStatus(int hp, int attack)
    {
        PlayerStatus = new PlayerParameter(hp, attack);
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
