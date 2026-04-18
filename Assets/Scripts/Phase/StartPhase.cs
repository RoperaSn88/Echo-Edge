using Cysharp.Threading.Tasks;
using UnityEngine;

public class StartPhase : IPhase
{
    private static StartPhase _instance;
    public static StartPhase Instance => _instance ??= new StartPhase();

    public async UniTask<IPhase> WaitPhase()
    {
        Debug.Log("StartPhase");
        await UniTask.WaitUntil(() => MapManager.Instance != null);
        await MapManager.Instance.BuildStageFromCsv();

        return PlayerPhase.Instance;
    }
}
