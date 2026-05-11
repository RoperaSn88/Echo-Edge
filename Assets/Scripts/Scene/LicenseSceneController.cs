using System.Threading;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class LicenseSceneController : MonoBehaviour
{
    private const float TweenDuration = 1.0f;
    
    [SerializeField]
    private Canvas _canvas;

    /// <summary>
    /// 画面外に配置するための画面幅に対する追加オフセット
    /// </summary>
    [SerializeField]
    [Tooltip("スライドイン/アウト時の画面外X位置オフセット（正の値）")]
    private float _offScreenXOffset = 200f;

    /// <summary>
    /// 音量スライダー・テキストなどをまとめたグループ
    /// </summary>
    [SerializeField]
    private RectTransform _group;
    
    /// <summary>
    /// クレジットテキスト
    /// </summary>
    [SerializeField]
    private TextMeshProUGUI _creditText;
    
    /// <summary>
    /// クレジットテキストをもつcontentのRectTransform。スクロールに使用する。
    /// </summary>
    [SerializeField]
    private RectTransform _contentRect;

    /// <summary>
    /// 閉じるボタン（クリック検知用）
    /// </summary>
    [SerializeField]
    private Button _closeButton;

    private Vector2 _onScreenAnchoredPosition;
    private bool _closeRequested;
    private bool _retireRequested;
    private CancellationTokenSource _cts;
    private Camera _uiCamera;

    [SerializeField] private LicenseComposer[] _credits; 

    private void Awake()
    {
        _uiCamera = GameObject.FindGameObjectWithTag("UICamera").GetComponent<Camera>();
        _canvas.worldCamera = _uiCamera;
        
        // 画面上のデザイン位置を記録する
        _onScreenAnchoredPosition = _group.anchoredPosition;
        OpenAsync().Forget();
    }

    private Vector2 GetOffScreenAnchoredPosition()
    {
        float offScreenX = _onScreenAnchoredPosition.x - _group.rect.width - _offScreenXOffset;
        return new Vector2(offScreenX, _onScreenAnchoredPosition.y);
    }

    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    /// <summary>
    /// オプションシーンを開く。
    /// </summary>
    /// <param name="canRetire">リタイアできる場合は <c>true</c>。<c>false</c> の場合、リタイアテキストは選択不可かつ半透明になる。</param>
    /// <returns>
    /// ユーザー操作の結果。リタイアテキストが押された場合は <see cref="OptionResult.Retire"/>、
    /// 閉じるテキストまたはEscキーの場合は <see cref="OptionResult.Close"/>。
    /// </returns>
    public async UniTask OpenAsync()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        
        // テキストの設定
        SetText();
        SetSize();
        
        // ボタンのリスナー設定
        _closeRequested = false;
        _closeButton.onClick.AddListener(OnCloseClicked);

        // グループを画面外（左）からスライドイン
        var offScreenPosition = GetOffScreenAnchoredPosition();
        _group.anchoredPosition = offScreenPosition;

        await _group.DOAnchorPos(_onScreenAnchoredPosition, TweenDuration)
            .SetEase(Ease.OutQuad)
            .ToUniTask(cancellationToken: _cts.Token);

        // 閉じる / リタイア / Escキーを待機
        await WaitForInteractionAsync(_cts.Token);

        // リスナーの解除
        _closeButton.onClick.RemoveListener(OnCloseClicked);

        // グループを画面外へスライドアウト
        await _group.DOAnchorPos(offScreenPosition, TweenDuration)
            .SetEase(Ease.InQuad)
            .ToUniTask(cancellationToken: _cts.Token);

        // オプションシーンのアンロード
        SceneLoader.Unload(GameScene.License);
    }
    
    /// <summary>
    /// 閉じる・リタイア・Escキーのいずれかが実行されるまで待機する。
    /// </summary>
    private async UniTask WaitForInteractionAsync(CancellationToken cancellationToken)
    {
        while (!_closeRequested && !_retireRequested)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                _closeRequested = true;
            }
        }
    }

    private void SetText()
    {
        string text = "";
        foreach (var tmp in _credits)
        {
            text = ZString.Concat(text,"-------------------------" ,'\n', tmp.LicenseName, '\n', tmp.LicenseIntroduction, '\n');
        }
        _creditText.text = text;
    }

    private void SetSize()
    {
        _contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _creditText.rectTransform.rect.height);
    }

    private void OnCloseClicked() => _closeRequested = true;
}


[System.Serializable, CreateAssetMenu(menuName = "License")]
public class LicenseComposer:ScriptableObject
{
    [SerializeField]
    private string licenseName;
    
    public string LicenseName => licenseName;
    
    [SerializeField,TextArea]
    private string licenseIntroduction;
    public string LicenseIntroduction => licenseIntroduction;
}