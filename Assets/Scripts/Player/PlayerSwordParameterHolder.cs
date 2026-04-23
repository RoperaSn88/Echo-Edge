public struct SwordParameter
{
    public int HP;
    public int Attack;
    public byte ReflectCount;

    public SwordParameter(int hp, int attack, byte reflectCount)
    {
        HP = hp;
        Attack = attack;
        ReflectCount = reflectCount;
    }
}

public struct PlayerParameter
{
    public int HP;
    public int Attack;
    public int Defend;

    public PlayerParameter(int hp, int attack, int defend)
    {
        HP = hp;
        Attack = attack;
        Defend = defend;
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
            : new PlayerParameter(100, 20, 0);
        SwordStatus = PlayerSwordParameterSaveManager.HasSwordStatusData()
            ? PlayerSwordParameterSaveManager.LoadSwordStatus()
            : new SwordParameter(0, 0, 0);
    }

    public static void SetPlayerStatus(BattleStatus playerStatus)
    {
        if (playerStatus == null)
        {
            PlayerStatus = new PlayerParameter(100, 20, 0);
            PlayerSwordParameterSaveManager.SavePlayerStatus(PlayerStatus);
            return;
        }

        PlayerStatus = new PlayerParameter(
            playerStatus.HP,
            playerStatus.Attack,
            playerStatus.Defend
        );
        PlayerSwordParameterSaveManager.SavePlayerStatus(PlayerStatus);
    }

    public static void SetSwordStatus(int hp, int attack, byte reflectCount)
    {
        SwordStatus = new SwordParameter(hp, attack, reflectCount);
        PlayerSwordParameterSaveManager.SaveSwordStatus(SwordStatus);
    }

    public static BattleStatus GetBattleStatus()
    {
        return new BattleStatus
        {
            HP = PlayerStatus.HP + SwordStatus.HP,
            MaxHP = PlayerStatus.HP + SwordStatus.HP,
            Attack = PlayerStatus.Attack + SwordStatus.Attack,
            Defend = PlayerStatus.Defend,
            Move = SwordStatus.ReflectCount,
        };
    }
}
