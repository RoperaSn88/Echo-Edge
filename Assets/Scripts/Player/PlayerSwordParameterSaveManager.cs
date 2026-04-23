using UnityEngine;

public static class PlayerSwordParameterSaveManager
{
    private const string PlayerHpKey = "PlayerStatus.HP";
    private const string PlayerAttackKey = "PlayerStatus.Attack";
    private const string PlayerDefendKey = "PlayerStatus.Defend";
    private const string SwordHpKey = "SwordStatus.HP";
    private const string SwordAttackKey = "SwordStatus.Attack";
    private const string SwordReflectCountKey = "SwordStatus.ReflectCount";

    public static bool HasPlayerStatusData()
    {
        return PlayerPrefs.HasKey(PlayerHpKey);
    }

    public static bool HasSwordStatusData()
    {
        return PlayerPrefs.HasKey(SwordHpKey);
    }

    public static PlayerParameter LoadPlayerStatus()
    {
        return new PlayerParameter(
            PlayerPrefs.GetInt(PlayerHpKey, 100),
            PlayerPrefs.GetInt(PlayerAttackKey, 20),
            PlayerPrefs.GetInt(PlayerDefendKey, 0)
        );
    }

    public static SwordParameter LoadSwordStatus()
    {
        return new SwordParameter(
            PlayerPrefs.GetInt(SwordHpKey, 0),
            PlayerPrefs.GetInt(SwordAttackKey, 0),
            (byte)Mathf.Clamp(PlayerPrefs.GetInt(SwordReflectCountKey, 0), byte.MinValue, byte.MaxValue)
        );
    }

    public static void SavePlayerStatus(PlayerParameter playerStatus)
    {
        PlayerPrefs.SetInt(PlayerHpKey, playerStatus.HP);
        PlayerPrefs.SetInt(PlayerAttackKey, playerStatus.Attack);
        PlayerPrefs.SetInt(PlayerDefendKey, playerStatus.Defend);
        PlayerPrefs.Save();
    }

    public static void SaveSwordStatus(SwordParameter swordStatus)
    {
        PlayerPrefs.SetInt(SwordHpKey, swordStatus.HP);
        PlayerPrefs.SetInt(SwordAttackKey, swordStatus.Attack);
        PlayerPrefs.SetInt(SwordReflectCountKey, swordStatus.ReflectCount);
        PlayerPrefs.Save();
    }
}
