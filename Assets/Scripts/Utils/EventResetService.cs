using EchoEdge.App.PlayerData;
using EchoEdge.App.Preparing;
using EchoEdge.Domain.Preparing;
using EchoEdge.Infra.Audio;
using EchoEdge.Infra.Battle;
using EchoEdge.Infra.Preparing;
using EchoEdge.Infra.Scenario;
using EchoEdge.Infra.Tutorial;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoEdge.Domain.Battle
{
    public static class EventResetService
    {
        public static void Reset()
        {
            // 1. ディスク上のセーブデータを削除する
            PlayerSwordParameterSaveManager.DeleteAllSavedData();
            AudioVolumeSaveManager.DeleteAllSavedData();
            StageProgressSaveManager.DeleteAllSavedData();
            TutorialSaveManager.DeleteAllSavedData();
            PrologueSaveManager.DeleteAllSavedData();
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            // 2. セーブデータをキャッシュしている static クラスをメモリ上でも初期化する。
            //    シーンを再読み込みしても static は自動では初期化されないため、
            //    これを呼ばないとステージ解放状況・強化値・所持金が残り続ける。
            StageData.ResetToDefault();
            PlayerSwordParameterHolder.ResetToDefault();
            EnhancementManager.ResetToDefault();

            SceneManager.LoadScene(0);
        }
    }
}