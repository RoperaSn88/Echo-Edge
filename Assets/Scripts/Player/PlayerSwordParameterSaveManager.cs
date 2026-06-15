using UnityEngine;

public static class PlayerSwordParameterSaveManager
{
    private const string PlayerHpKey = "PlayerStatus.HP";
    private const string PlayerCurrentHpKey = "PlayerStatus.CurrentHP";
    private const string PlayerAttackKey = "PlayerStatus.Attack";
    private const string PlayerDefendKey = "PlayerStatus.Defend";
    private const string PlayerExperienceKey = "PlayerStatus.Experience";
    private const string PlayerLevelKey = "PlayerStatus.Level";
    private const string SwordHpKey = "SwordStatus.HP";
    private const string SwordAttackKey = "SwordStatus.Attack";
    private const string SwordReflectCountKey = "SwordStatus.ReflectCount";
    private const string StoneKey = "Enhancement.Stone";

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
            PlayerPrefs.GetInt(PlayerDefendKey, 0),
            PlayerPrefs.GetInt(PlayerExperienceKey, 0),
            PlayerPrefs.GetInt(PlayerLevelKey, 1),
            PlayerPrefs.GetInt(PlayerCurrentHpKey, PlayerPrefs.GetInt(PlayerHpKey, 100))
        );
    }

    public static SwordParameter LoadSwordStatus()
    {
        return new SwordParameter(
            PlayerPrefs.GetInt(SwordHpKey, 0),
            PlayerPrefs.GetInt(SwordAttackKey, 0),
            (byte)Mathf.Clamp(PlayerPrefs.GetInt(SwordReflectCountKey, 1), byte.MinValue, byte.MaxValue)
        );
    }

    public static void SavePlayerStatus(PlayerParameter playerStatus)
    {
        PlayerPrefs.SetInt(PlayerHpKey, playerStatus.HP);
        PlayerPrefs.SetInt(PlayerCurrentHpKey, playerStatus.CurrentHP);
        PlayerPrefs.SetInt(PlayerAttackKey, playerStatus.Attack);
        PlayerPrefs.SetInt(PlayerDefendKey, playerStatus.Defend);
        PlayerPrefs.SetInt(PlayerExperienceKey, playerStatus.Experience);
        PlayerPrefs.SetInt(PlayerLevelKey, playerStatus.Level);
        PlayerPrefs.Save();
    }

    public static void SaveSwordStatus(SwordParameter swordStatus)
    {
        PlayerPrefs.SetInt(SwordHpKey, swordStatus.HP);
        PlayerPrefs.SetInt(SwordAttackKey, swordStatus.Attack);
        PlayerPrefs.SetInt(SwordReflectCountKey, swordStatus.ReflectCount);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 石（強化用通貨）のデータが保存済みか確認する。
    /// </summary>
    public static bool HasStoneData()
    {
        return PlayerPrefs.HasKey(StoneKey);
    }

    /// <summary>
    /// 現在の石の所持数を読み込む。保存データがない場合は 0 を返す。
    /// </summary>
    public static int LoadStone()
    {
        return PlayerPrefs.GetInt(StoneKey, 0);
    }

    /// <summary>
    /// 石の所持数を保存する。
    /// </summary>
    public static void SaveStone(int stone)
    {
        if (stone < 0)
        {
            Debug.LogWarning($"{nameof(PlayerSwordParameterSaveManager)}: 負の石の所持数を保存しようとしました ({stone})。0 にクランプします。");
            stone = 0;
        }
        PlayerPrefs.SetInt(StoneKey, stone);
        PlayerPrefs.Save();
    }

    public static void DeleteAllSavedData()
    {
        PlayerPrefs.DeleteKey(PlayerHpKey);
        PlayerPrefs.DeleteKey(PlayerCurrentHpKey);
        PlayerPrefs.DeleteKey(PlayerAttackKey);
        PlayerPrefs.DeleteKey(PlayerDefendKey);
        PlayerPrefs.DeleteKey(PlayerExperienceKey);
        PlayerPrefs.DeleteKey(PlayerLevelKey);
        PlayerPrefs.DeleteKey(SwordHpKey);
        PlayerPrefs.DeleteKey(SwordAttackKey);
        PlayerPrefs.DeleteKey(SwordReflectCountKey);
        PlayerPrefs.DeleteKey(StoneKey);
    }
}
