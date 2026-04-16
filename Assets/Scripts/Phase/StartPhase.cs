using Cysharp.Threading.Tasks;
using UnityEngine;

public class StartPhase : MonoBehaviour, IPhase
{
    private static StartPhase _instance;
    public static StartPhase Instance
    {
        get
        {
            if (_instance != null) return _instance;
            _instance = FindAnyObjectByType<StartPhase>();
            if (_instance != null) return _instance;

            var go = new GameObject(nameof(StartPhase));
            _instance = go.AddComponent<StartPhase>();
            return _instance;
        }
    }

    public async UniTask<IPhase> WaitPhase()
    {
        Debug.Log("StartPhase");
        await UniTask.WaitUntil(() => MapManager.Instance != null);
        await MapManager.Instance.BuildStageFromCsv();

        return PlayerPhase.Instance;
    }
}
