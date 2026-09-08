using EchoEdge.Infra.Audio;
using EchoEdge.Infra.Battle;
using EchoEdge.Infra.Preparing;
using EchoEdge.Infra.Tutorial;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoEdge.Domain.Battle
{
    public static class EventResetService
    {
        public static void Reset()
        {
            PlayerSwordParameterSaveManager.DeleteAllSavedData();
            AudioVolumeSaveManager.DeleteAllSavedData();
            StageProgressSaveManager.DeleteAllSavedData();
            TutorialSaveManager.DeleteAllSavedData();
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            
            SceneManager.LoadScene(0);
        }
    }
}