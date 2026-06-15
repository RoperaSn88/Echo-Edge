using UnityEngine;

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
    public int CurrentHP;
    public int Attack;
    public int Defend;
    public int Experience;
    public int Level;

    public PlayerParameter(int hp, int attack, int defend, int experience = 0, int level = 1, int? currentHp = null)
    {
        HP = hp;
        CurrentHP = currentHp ?? hp;
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

        var maxHpWithoutSword = Mathf.Max(1, playerStatus.MaxHP - SwordStatus.HP);
        PlayerStatus = new PlayerParameter(
            maxHpWithoutSword,
            playerStatus.Attack,
            playerStatus.Defend,
            playerStatus.Experience,
            playerStatus.Level,
            playerStatus.HP
        );
        PlayerSwordParameterSaveManager.SavePlayerStatus(PlayerStatus);
    }

    /// <summary>
    /// 強化したパラメーターを更新するために、PlayerParameter を直接指定してプレイヤーステータスを更新し永続化する。
    /// </summary>
    public static void SetPlayerStatus(PlayerParameter playerParameter)
    {
        PlayerStatus = playerParameter;
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
            level,
            PlayerStatus.CurrentHP
        );
        PlayerSwordParameterSaveManager.SavePlayerStatus(PlayerStatus);
    }

    public static BattleStatus GetBattleStatus()
    {
        var maxHp = PlayerStatus.HP + SwordStatus.HP;
        var currentHp = Mathf.Clamp(PlayerStatus.CurrentHP, 0, maxHp);
        var status = new BattleStatus();
        status.SetStatus(
            currentHp,
            PlayerStatus.Attack + SwordStatus.Attack,
            PlayerStatus.Defend,
            SwordStatus.ReflectCount,
            default,
            PlayerStatus.Experience,
            0
        );
        status.MaxHP = maxHp;
        status.SetLevel(PlayerStatus.Level);
        return status;
    }
}
