using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using TMPro;
using System;

public class TurnChangeView : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _turnChangeText;

    [SerializeField]
    private CanvasGroup _playerCanvasGroup;

    [SerializeField]
    private CanvasGroup _enemyCanvasGroup;


    private void Start()
    {
        _playerCanvasGroup.alpha = 0f;
        _enemyCanvasGroup.alpha = 0f;
        _playerCanvasGroup.gameObject.SetActive(false);
        _enemyCanvasGroup.gameObject.SetActive(false);
        _turnChangeText.gameObject.SetActive(false);
    }

    public async UniTask ShowTurnChange(TurnChangeKinds turnChangeKind)
    {
        AudioManager.Instance.PlaySe(SeAudioType.TurnChange);
        _turnChangeText.gameObject.SetActive(true);
        _turnChangeText.text = turnChangeKind == TurnChangeKinds.PlayerTurn ? "Player Phase" : "Enemy Phase";
        _turnChangeText.transform.localScale = Vector3.one * 2f;

        switch (turnChangeKind)
        {
            case TurnChangeKinds.PlayerTurn:
                _enemyCanvasGroup.gameObject.SetActive(false);
                _playerCanvasGroup.gameObject.SetActive(true);
                await UniTask.WhenAll(
                    _playerCanvasGroup.DOFade(1f, 0.5f).ToUniTask(),
                    _turnChangeText.DOFade(1f, 0.5f).ToUniTask(),
                    _turnChangeText.transform.DOScale(1f, 0.5f).SetEase(Ease.OutQuad).ToUniTask()
                );
                await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
                await UniTask.WhenAll(
                    _playerCanvasGroup.DOFade(0f, 0.5f).ToUniTask(),
                    _turnChangeText.DOFade(0f, 0.5f).ToUniTask(),
                    _turnChangeText.transform.DOScale(0.5f, 0.5f).SetEase(Ease.InQuad).ToUniTask()
                );
                _playerCanvasGroup.gameObject.SetActive(false);
                break;

            case TurnChangeKinds.EnemyTurn:
                _playerCanvasGroup.gameObject.SetActive(false);
                _enemyCanvasGroup.gameObject.SetActive(true);
                await UniTask.WhenAll(
                    _enemyCanvasGroup.DOFade(1f, 0.5f).ToUniTask(),
                    _turnChangeText.DOFade(1f, 0.5f).ToUniTask(),
                    _turnChangeText.transform.DOScale(1f, 0.5f).SetEase(Ease.OutQuad).ToUniTask()
                );
                await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
                await UniTask.WhenAll(
                    _enemyCanvasGroup.DOFade(0f, 0.5f).ToUniTask(),
                    _turnChangeText.DOFade(0f, 0.5f).ToUniTask(),
                    _turnChangeText.transform.DOScale(0.5f, 0.5f).SetEase(Ease.InQuad).ToUniTask()
                );
                _enemyCanvasGroup.gameObject.SetActive(false);
                break;
        }
        _turnChangeText.gameObject.SetActive(false);
    }
}