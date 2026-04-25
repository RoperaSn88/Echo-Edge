using Cysharp.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine;
using DG.Tweening;
using System.Threading.Tasks;

public class SelectManager : MonoBehaviour
{
    private const float FadeTime = 2.0f;
    [SerializeField]
    private Image _panel;

    /// <summary>
    /// 起動時
    /// </summary>
    async void Start()
    {
        _panel.gameObject.SetActive(true);
        var c = _panel.color;
        c.a = 1f;
        _panel.color = c;

        await _panel.DOFade(0f,FadeTime);
        _panel.gameObject.SetActive(false);
        Selecting().Forget();
    }

    /// <summary>
    /// 選択の管理をする
    /// </summary>
    /// <returns></returns>
    async UniTask Selecting()
    {
        var v = await RayCasterManager.Instance.Selecting();
        Debug.Log(v);
    }
}
