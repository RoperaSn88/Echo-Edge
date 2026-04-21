using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class MessageManager : MonoBehaviour
{
    public static MessageManager Instance { get; private set; }
    public const string EnemyAttackMessage = "敵の攻撃";
    public const string BuilderAttackMessage = "ビルダーの攻撃";

    [SerializeField] private RectTransform _messageRectTransform;
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private float _moveOffset = 150f;
    [SerializeField] private float _moveDuration = 0.2f;

    private Vector2 _visiblePosition;

    private Vector2 HiddenPosition => _visiblePosition + new Vector2(0, _moveOffset);

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (_messageRectTransform != null)
        {
            _visiblePosition = _messageRectTransform.anchoredPosition;
            _messageRectTransform.anchoredPosition = HiddenPosition;
        }

        if (_messageText != null)
        {
            _messageText.gameObject.SetActive(false);
        }
    }

    public async UniTask AppearMessage(string text)
    {
        if (_messageText == null || _messageRectTransform == null)
        {
            return;
        }

        _messageRectTransform.DOKill();
        _messageText.text = text;
        _messageText.gameObject.SetActive(true);
        _messageRectTransform.anchoredPosition = HiddenPosition;
        await _messageRectTransform.DOAnchorPos(_visiblePosition, _moveDuration).SetEase(Ease.OutQuad).SetUpdate(true)
            .SetLink(gameObject);
    }

    public async UniTask DisappearMessage()
    {
        if (_messageText == null || _messageRectTransform == null)
        {
            return;
        }

        _messageRectTransform.DOKill();
        await _messageRectTransform.DOAnchorPos(HiddenPosition, _moveDuration).SetEase(Ease.InQuad).SetUpdate(true)
            .SetLink(gameObject);
        _messageText.gameObject.SetActive(false);
    }

    public async UniTask ShowMessage(string text, float staySeconds, CancellationToken cancellationToken = default)
    {
        try
        {
            await AppearMessage(text);
            using var linkedTokenSource = CancellationTokenSource
                .CreateLinkedTokenSource(cancellationToken, this.GetCancellationTokenOnDestroy());
            await UniTask.Delay(TimeSpan.FromSeconds(staySeconds), cancellationToken: linkedTokenSource.Token);
            await DisappearMessage();
        }
        catch (OperationCanceledException)
        {
            // do nothing
        }
    }
}
