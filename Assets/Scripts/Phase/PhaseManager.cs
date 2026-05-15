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
        while (true)
        {
            phase = await phase.WaitPhase().AttachExternalCancellation(cancellationToken);
        }
    }
}
