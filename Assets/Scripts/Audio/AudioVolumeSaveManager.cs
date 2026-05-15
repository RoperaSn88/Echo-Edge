using UnityEngine;

/// <summary>
/// 音量設定を PlayerPrefs で保存・読み込みするマネージャー。
/// </summary>
public static class AudioVolumeSaveManager
{
    private const string MasterVolumeKey = "AudioVolume.Master";
    private const string BgmVolumeKey = "AudioVolume.BGM";
    private const string SeVolumeKey = "AudioVolume.SE";

    private const float DefaultVolume = 1.0f;

    /// <summary>
    /// 音量データが保存済みかどうかを確認する。
    /// 3つの音量値は <see cref="SaveVolumes"/> で常に同時に保存されるため、
    /// MasterVolume キーの有無を判定の基準として使用する。
    /// </summary>
    public static bool HasVolumeData()
    {
        return PlayerPrefs.HasKey(MasterVolumeKey);
    }

    public static float LoadMasterVolume()
    {
        return PlayerPrefs.GetFloat(MasterVolumeKey, DefaultVolume);
    }

    public static float LoadBgmVolume()
    {
        return PlayerPrefs.GetFloat(BgmVolumeKey, DefaultVolume);
    }

    public static float LoadSeVolume()
    {
        return PlayerPrefs.GetFloat(SeVolumeKey, DefaultVolume);
    }

    public static void SaveVolumes(float masterVolume, float bgmVolume, float seVolume)
    {
        PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
        PlayerPrefs.SetFloat(BgmVolumeKey, bgmVolume);
        PlayerPrefs.SetFloat(SeVolumeKey, seVolume);
        PlayerPrefs.Save();
    }

    public static void DeleteAllSavedData()
    {
        PlayerPrefs.DeleteKey(MasterVolumeKey);
        PlayerPrefs.DeleteKey(BgmVolumeKey);
        PlayerPrefs.DeleteKey(SeVolumeKey);
        PlayerPrefs.Save();
    }
}
