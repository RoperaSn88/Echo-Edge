using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

public class PhaseManager : MonoBehaviour
{

    /// <summary>
    /// クリックされた判定を取るパネル
    /// </summary>
    public static ClickPanel clickPanel;

    /// <summary>
    /// 登録用
    /// </summary>
    [SerializeField]
    private ClickPanel _clickPanel;

    async void Start()
    {
        if(_clickPanel != null)
        {
            clickPanel = _clickPanel;
            Debug.Log("設定したよ");
        }
        else
        {
            throw new System.Exception("パネルが設定されてないぞ");
        }
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
