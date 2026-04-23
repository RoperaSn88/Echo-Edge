using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void Load(GameScene scene)
    {
        SceneManager.LoadScene((int)scene);
    }
}
