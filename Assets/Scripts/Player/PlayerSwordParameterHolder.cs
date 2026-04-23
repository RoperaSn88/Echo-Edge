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

public struct PlayerParameter
{
    public int HP;
    public int Attack;
    public int Defend;
    public byte ReflectCount;

    public PlayerParameter(int hp, int attack, int defend, byte reflectCount)
    {
        HP = hp;
        Attack = attack;
        Defend = defend;
        ReflectCount = reflectCount;
    }
}

public static class PlayerSwordParameterHolder
{
    public static PlayerParameter PlayerStatus { get; private set; }
    public static SwordParameter SwordStatus { get; private set; }
    
    static PlayerSwordParameterHolder()
    {
        // 後でセーブ/ロード機能から読みとる
        PlayerStatus = new PlayerParameter(0, 0, 0, 0);
        SwordStatus = new SwordParameter(0, 0);
    }

    public static void SetPlayerStatus(BattleStatus playerStatus)
    {
        if (playerStatus == null)
        {
            PlayerStatus = new PlayerParameter(0, 0, 0, 0);
            return;
        }

        var reflectCount = playerStatus.Move < byte.MinValue
            ? byte.MinValue
            : playerStatus.Move > byte.MaxValue
                ? byte.MaxValue
                : (byte)playerStatus.Move;
        PlayerStatus = new PlayerParameter(
            playerStatus.HP,
            playerStatus.Attack,
            playerStatus.Defend,
            reflectCount
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
