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
        IPhase phase = new PlayerPhase();
        while (true)
        {
            phase = await phase.WaitPhase();
        }
    }
}
