using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// TextMeshPro用のISelectInterface実装クラス。
/// 選択時に文字サイズをDOTweenでアニメーションさせ、選択解除時に元のサイズに戻す。
/// </summary>
public abstract class TMPSelectObject : MonoBehaviour, ISelectable
{
    private const float SelectedFontSize = 150f;
    private const float DeselectedFontSize = 90f;
    private const float TweenDuration = 0.2f;

    private readonly Vector2 DefaultSize = new Vector2(450.01f,120.01f);
    private readonly Vector2 SelectSize = new Vector2(750.01f,150.01f);

    [SerializeField]
    private TextMeshProUGUI _text;
    
    [NonSerialized]
    public RectTransform RectTransform;

    private CancellationTokenSource _cts;
    private bool _isDecided = false;

    /// <summary>
    /// 決定済みフラグを設定する。trueの場合はDeSelectによるサイズリセットを行わない。
    /// </summary>
    public void SetDecided(bool decided)
    {
        _isDecided = decided;
    }

    private void Awake()
    {
        RectTransform = GetComponent<RectTransform>();
        if (_text == null)
        {
            _text = GetComponent<TextMeshProUGUI>();
        }
        _cts = new CancellationTokenSource();
    }

    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    /// <summary>
    /// 現在進行中のトゥイーンをキャンセルし、新しいCancellationTokenを返す。
    /// </summary>
    private CancellationToken ResetCancellationToken()
    {
        _cts.Cancel();
        _cts.Dispose();
        _cts = new CancellationTokenSource();
        return _cts.Token;
    }

    /// <summary>
    /// カーソルで選択された場合の処理。文字サイズを90から150にDOTweenでアニメーションさせ、rect.heightも調整する。
    /// </summary>
    public async void OnSelect()
    {
        var ct = ResetCancellationToken();
        await UniTask.WhenAll(
            DOTween.To(() => _text.fontSize, x => _text.fontSize = x, SelectedFontSize, TweenDuration).ToUniTask(cancellationToken: ct),
            RectTransform.DOSizeDelta(SelectSize, TweenDuration).ToUniTask(cancellationToken: ct)
        );
    }

    /// <summary>
    /// クリックで決定された場合の処理。
    /// </summary>
    public abstract UniTask OnDecide();

    /// <summary>
    /// カーソルの選択が外れた場合の処理。文字サイズを150から90にDOTweenでアニメーションさせ、rect.heightも調整する。
    /// 決定済みの場合はサイズをリセットしない。
    /// </summary>
    public async void OnDeselect()
    {
        if (_isDecided) return;
        var ct = ResetCancellationToken();
        await UniTask.WhenAll(
            DOTween.To(() => _text.fontSize, x => _text.fontSize = x, DeselectedFontSize, TweenDuration).ToUniTask(cancellationToken: ct),
            RectTransform.DOSizeDelta(DefaultSize, TweenDuration).ToUniTask(cancellationToken: ct)
        );
    }
}