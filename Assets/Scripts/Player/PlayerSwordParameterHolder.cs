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
    public int Experience;
    public int Level;

    public PlayerParameter(int hp, int attack, int defend, int experience = 0, int level = 1)
    {
        HP = hp;
        Attack = attack;
        Defend = defend;
        Experience = experience;
        Level = level;
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
            : new PlayerParameter(100, 20, 0, 0, 1);
        SwordStatus = PlayerSwordParameterSaveManager.HasSwordStatusData()
            ? PlayerSwordParameterSaveManager.LoadSwordStatus()
            : new SwordParameter(0, 0, 0);
    }

    public static void SetPlayerStatus(BattleStatus playerStatus)
    {
        if (playerStatus == null)
        {
            PlayerStatus = new PlayerParameter(100, 20, 0, 0, 1);
            PlayerSwordParameterSaveManager.SavePlayerStatus(PlayerStatus);
            return;
        }

        PlayerStatus = new PlayerParameter(
            playerStatus.HP,
            playerStatus.Attack,
            playerStatus.Defend,
            playerStatus.Experience,
            playerStatus.Level
        );
        PlayerSwordParameterSaveManager.SavePlayerStatus(PlayerStatus);
    }

    public static void SetSwordStatus(int hp, int attack, byte reflectCount)
    {
        SwordStatus = new SwordParameter(hp, attack, reflectCount);
        PlayerSwordParameterSaveManager.SaveSwordStatus(SwordStatus);
    }

    public static void SetPlayerProgress(int experience, int level)
    {
        PlayerStatus = new PlayerParameter(
            PlayerStatus.HP,
            PlayerStatus.Attack,
            PlayerStatus.Defend,
            experience,
            level
        );
        PlayerSwordParameterSaveManager.SavePlayerStatus(PlayerStatus);
    }

    public static BattleStatus GetBattleStatus()
    {
        var status = new BattleStatus();
        status.SetStatus(
            PlayerStatus.HP + SwordStatus.HP,
            PlayerStatus.Attack + SwordStatus.Attack,
            PlayerStatus.Defend,
            SwordStatus.ReflectCount,
            default,
            PlayerStatus.Experience,
            0,
            PlayerStatus.Level
        );
        return status;
    }
}
