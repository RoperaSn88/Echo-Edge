using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class PhaseManager : MonoBehaviour
{
    async void Start()
    {
        await Phasing();
    }

    async UniTask Phasing()
    {
        await UniTask.WaitUntil(() => UIPresenter.Instance != null);
        IPhase phase = StartPhase.Instance;
        while (true)
        {
            await UIPresenter.Instance.AppearPhaseText(phase.GetType().Name);
            phase = await phase.WaitPhase();
            await UniTask.Delay(TimeSpan.FromSeconds(0.2f));
            await UIPresenter.Instance.DisappearPhaseText();
        }
    }
}
