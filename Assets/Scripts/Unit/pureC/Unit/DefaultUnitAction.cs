using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class DefaultUnitAction : IUnitAction
{
    private const float PlayerDamageRate = 1.0f;
    private const float MessageTime = 0.6f;
    
    /// <inheritdoc/>
    public async UniTask Attack()
    {
        if (MessageManager.Instance != null)
        {
            await MessageManager.Instance.ShowMessage(MessageManager.EnemyAttackMessage, MessageTime);
        }

        Time.timeScale = 0.001f;
        var damageValue = await BattleManager.PlayerDamage(PlayerDamageRate);
        Time.timeScale = 1.0f;

        UIPresenter.Instance.AppearDamageText($"{damageValue.damage}", PlayerController.Instance.transform.position).Forget();

        await UniTask.Delay(TimeSpan.FromSeconds(1.0f));

        BattleManager.ResetQTE();
        await CameraManager.Instance.ActResetCameraTarget();
    }
    
    /// <inheritdoc/>
    public async UniTask Dead()
    {
        // 死亡時と同時に発生する処理
    }
    
    /// <inheritdoc/>
    public async UniTask Specific()
    {

    }

    /// <inheritdoc/>
    public UniTask OnTurnStart()
    {
        return UniTask.CompletedTask;
    }

    /// <inheritdoc/>
    public UniTask OnTurnEnd()
    {
        return UniTask.CompletedTask;
    }
    
    /// <inheritdoc/>
    public async UniTask Damage()
    {
        
    }
}
