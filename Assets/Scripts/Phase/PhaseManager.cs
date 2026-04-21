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
        while (UIPresenter.Instance == null)
        {
            await UniTask.Yield();
        }

        IPhase phase = StartPhase.Instance;
        while (true)
        {
            await UIPresenter.Instance.AppearPhaseText(GetPhaseDisplayText(phase));
            phase = await phase.WaitPhase();
            await UIPresenter.Instance.DisappearPhaseText();
        }
    }

    private static string GetPhaseDisplayText(IPhase phase)
    {
        return phase switch
        {
            StartPhase => "ゲーム開始",
            PlayerPhase => "プレイヤーフェーズ",
            PlayerAttackPhase => "攻撃フェーズ",
            PlayerWeaponPhase => "武器選択フェーズ",
            EnemyPhase => "エネミーフェーズ",
            _ => string.Empty
        };
    }
}
