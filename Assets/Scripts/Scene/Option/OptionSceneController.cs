using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// オプションシーンのコントローラー。
/// <para>
/// AdditiveロードされたOptionシーンにアタッチし、<see cref="OpenAsync"/> を呼び出して使用する。
/// </para>
/// </summary>
public class OptionSceneController : MonoBehaviour
{
    private const float TweenDuration = 1.0f;
    private const float RetireDisabledAlpha = 0.05f;
    
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
    /// マスター音量スライダー
    /// </summary>
    [SerializeField]
    private Slider _masterVolumeSlider;

    /// <summary>
    /// BGM音量スライダー
    /// </summary>
    [SerializeField]
    private Slider _bgmVolumeSlider;

    /// <summary>
    /// SE音量スライダー
    /// </summary>
    [SerializeField]
    private Slider _seVolumeSlider;

    /// <summary>
    /// リタイアテキスト（表示用）
    /// </summary>
    [SerializeField]
    private TextMeshProUGUI _retireText;

    /// <summary>
    /// リタイアボタン（クリック検知用）
    /// </summary>
    [SerializeField]
    private Button _retireButton;

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

    private void Awake()
    {
        _uiCamera = GameObject.FindGameObjectWithTag("UICamera").GetComponent<Camera>();
        _canvas.worldCamera = _uiCamera;
        
        // 画面上のデザイン位置を記録する
        _onScreenAnchoredPosition = _group.anchoredPosition;
        OpenAsync(false).Forget();
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
    public async UniTask<OptionResult> OpenAsync(bool canRetire)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        // リタイアテキストの有効・無効設定
        SetRetireInteractable(canRetire);

        // スライダーを現在の音量で初期化
        InitializeSliders();

        // スライダーのリスナー設定
        _masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        _bgmVolumeSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
        _seVolumeSlider.onValueChanged.AddListener(OnSeVolumeChanged);

        // ボタンのリスナー設定
        _closeRequested = false;
        _retireRequested = false;
        _closeButton.onClick.AddListener(OnCloseClicked);
        if (canRetire) _retireButton.onClick.AddListener(OnRetireClicked);

        // グループを画面外（左）からスライドイン
        var offScreenPosition = GetOffScreenAnchoredPosition();
        _group.anchoredPosition = offScreenPosition;

        await _group.DOAnchorPos(_onScreenAnchoredPosition, TweenDuration)
            .SetEase(Ease.OutQuad)
            .ToUniTask(cancellationToken: _cts.Token);

        // 閉じる / リタイア / Escキーを待機
        await WaitForInteractionAsync(_cts.Token);

        // リスナーの解除
        _masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
        _bgmVolumeSlider.onValueChanged.RemoveListener(OnBgmVolumeChanged);
        _seVolumeSlider.onValueChanged.RemoveListener(OnSeVolumeChanged);
        _closeButton.onClick.RemoveListener(OnCloseClicked);
        if (canRetire) _retireButton.onClick.RemoveListener(OnRetireClicked);

        var result = _retireRequested ? OptionResult.Retire : OptionResult.Close;

        // グループを画面外へスライドアウト
        await _group.DOAnchorPos(offScreenPosition, TweenDuration)
            .SetEase(Ease.InQuad)
            .ToUniTask(cancellationToken: _cts.Token);

        // オプションシーンのアンロード
        SceneLoader.Unload(GameScene.Option);

        return result;
    }

    /// <summary>
    /// リタイアテキスト・ボタンの選択可否を設定する。
    /// </summary>
    private void SetRetireInteractable(bool canRetire)
    {
        _retireButton.interactable = canRetire;
        if (!canRetire)
        {
            if (_retireText != null)
            {
                var textColor = _retireText.color;
                textColor.a = RetireDisabledAlpha;
                _retireText.color = textColor;
            }
        }
    }

    /// <summary>
    /// スライダーを AudioManager の現在の音量で初期化する。
    /// </summary>
    private void InitializeSliders()
    {
        if (AudioManager.Instance == null) return;

        _masterVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.MasterVolume);
        _bgmVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.BgmVolume);
        _seVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.SeVolume);
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

    private void OnCloseClicked() => _closeRequested = true;
    private void OnRetireClicked() => _retireRequested = true;

    private static void OnMasterVolumeChanged(float value)
    {
        AudioManager.Instance?.SetMasterVolume(value);
    }

    private static void OnBgmVolumeChanged(float value)
    {
        AudioManager.Instance?.SetBgmVolume(value);
    }

    private static void OnSeVolumeChanged(float value)
    {
        AudioManager.Instance?.SetSeVolume(value);
    }
}
