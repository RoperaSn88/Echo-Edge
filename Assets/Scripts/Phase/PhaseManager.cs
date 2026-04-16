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
        IPhase phase = StartPhase.Instance;
        while (true)
        {
            phase = await phase.WaitPhase();
        }
    }
}
