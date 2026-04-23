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
        PlayerStatus = PlayerSwordParameterSaveManager.HasPlayerStatusData()
            ? PlayerSwordParameterSaveManager.LoadPlayerStatus()
            : new PlayerParameter(0, 0, 0, 0);
        SwordStatus = PlayerSwordParameterSaveManager.HasSwordStatusData()
            ? PlayerSwordParameterSaveManager.LoadSwordStatus()
            : new SwordParameter(0, 0);
    }

    public static void SetPlayerStatus(BattleStatus playerStatus)
    {
        if (playerStatus == null)
        {
            PlayerStatus = new PlayerParameter(0, 0, 0, 0);
            PlayerSwordParameterSaveManager.SavePlayerStatus(PlayerStatus);
            return;
        }

        var clampedReflectCount = playerStatus.Move;
        if (clampedReflectCount < byte.MinValue) clampedReflectCount = byte.MinValue;
        if (clampedReflectCount > byte.MaxValue) clampedReflectCount = byte.MaxValue;
        var reflectCount = (byte)clampedReflectCount;
        PlayerStatus = new PlayerParameter(
            playerStatus.HP,
            playerStatus.Attack,
            playerStatus.Defend,
            reflectCount
        );
        PlayerSwordParameterSaveManager.SavePlayerStatus(PlayerStatus);
    }

    public static void SetSwordStatus(int hp, int attack)
    {
        SwordStatus = new SwordParameter(hp, attack);
        PlayerSwordParameterSaveManager.SaveSwordStatus(SwordStatus);
    }

    public static BattleStatus GetBattleStatus()
    {
        var hp = PlayerStatus.HP + SwordStatus.HP;
        return new BattleStatus
        {
            HP = hp,
            MaxHP = hp,
            Attack = PlayerStatus.Attack + SwordStatus.Attack,
        };
    }
}
