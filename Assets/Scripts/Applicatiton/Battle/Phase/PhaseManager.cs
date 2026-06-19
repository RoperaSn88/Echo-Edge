using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

public class PhaseManager : MonoBehaviour
{
    async void Start()
    {
        try
        {
            await Phasing(destroyCancellationToken);
        }
        catch (OperationCanceledException)
        {
            // PhaseManager が破棄されたためフェーズループをキャンセルしました
            Debug.Log("PhaseManager: フェーズループをキャンセルしました");
        }
    }

    async UniTask Phasing(CancellationToken cancellationToken)
    {
        IPhase phase = StartPhase.Instance;
        try
        {
            while (true)
        {
            phase = await phase.WaitPhase();
        }
        }
        catch (OperationCanceledException)
        {
            // フェーズの待機中にキャンセルされた場合はループを抜ける
            Debug.Log("PhaseManager: フェーズの待機中にキャンセルされました");
        }
    }
}
