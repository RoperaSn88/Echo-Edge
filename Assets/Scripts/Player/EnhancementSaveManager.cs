using UnityEngine;

/// <summary>
/// 強化画面で使う累積経験値を PlayerPrefs で保存・読み込みするマネージャー。
/// </summary>
public static class EnhancementSaveManager
{
    private const string ExperienceKey = "Enhancement.Experience";

    /// <summary>
    /// 経験値データが保存済みか確認する。
    /// </summary>
    public static bool HasExperienceData()
    {
        return PlayerPrefs.HasKey(ExperienceKey);
    }

    /// <summary>
    /// 現在の累積経験値を読み込む。保存データがない場合は 0 を返す。
    /// </summary>
    public static int LoadExperience()
    {
        return PlayerPrefs.GetInt(ExperienceKey, 0);
    }

    /// <summary>
    /// 累積経験値を保存する。
    /// </summary>
    public static void SaveExperience(int experience)
    {
        if (experience < 0)
        {
            Debug.LogWarning($"{nameof(EnhancementSaveManager)}: 負の経験値を保存しようとしました ({experience})。0 にクランプします。");
            experience = 0;
        }
        PlayerPrefs.SetInt(ExperienceKey, experience);
        PlayerPrefs.Save();
    }
}
