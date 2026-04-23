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
    }

    public static void SetSwordStatus(int hp, int attack)
    {
        SwordStatus = new SwordParameter(hp, attack);
    }

    public static BattleStatus GetBattleStatus()
    {
        var battleStatus = new BattleStatus
        {
            HP = PlayerStatus.HP + SwordStatus.HP,
            Attack = PlayerStatus.Attack + SwordStatus.Attack,
        };
        battleStatus.MaxHP = battleStatus.HP;
        return battleStatus;
    }
}
