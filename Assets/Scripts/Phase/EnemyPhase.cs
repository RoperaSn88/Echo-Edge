using Cysharp.Threading.Tasks;
using UnityEngine.EventSystems;
using UnityEngine;
using System;

public class EnemyPhase: IPhase
{
    /// <summary>
    /// クリックされたか検知するブール
    /// </summary>
    [SerializeField]
    bool _clickFlug;

    /// <summary>
    /// エネミーフェーズのインスタンス
    /// </summary>
    private static EnemyPhase _instance;

    /// <summary>
    /// 他のスクリプトから干渉するプロパティ
    /// </summary>
    public static EnemyPhase Instance => _instance ??= new EnemyPhase();

    public async UniTask<IPhase> WaitPhase()
    {
        _clickFlug = false;
        Debug.Log("EnemyPhase");
        await UniTask.Delay(TimeSpan.FromSeconds(1f));
        Debug.Log("おわり");

        return PlayerPhase.Instance;
    }
}