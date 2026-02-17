using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class PlayerSkillPhase : IPhase
{

    /// <summary>
    /// スキルフェーズのインスタンス
    /// </summary>
    private static PlayerSkillPhase _instance;

    /// <summary>
    /// 他のスクリプトから干渉するプロパティ
    /// </summary>
    public static PlayerSkillPhase Instance => _instance ??= new PlayerSkillPhase();
    public async UniTask<IPhase> WaitPhase()
    {
        Debug.Log("SkillPhase");
        await UniTask.Delay(TimeSpan.FromSeconds(1f));
        return PlayerPhase.Instance;
    }
}