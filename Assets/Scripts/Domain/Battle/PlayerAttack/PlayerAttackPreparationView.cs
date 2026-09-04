using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace EchoEdge.Domain.Battle
{
    public class PlayerAttackPreparationView: MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI _attackText;
        
        [SerializeField]
        private TextMeshProUGUI _attackDescriptionText;
        
        [SerializeField]
        private ParticleSystem _attackParticleSystem;
        
        private ParticleSystem.MainModule _main;
        
        private const float FadeInDuration = 0.25f;
        
        public async UniTask InitializeAsync(CancellationToken token)
        {
            // 初期状態ではテキストを非表示にする
            _attackText.alpha = 0f;
            _attackDescriptionText.alpha = 0f;
            _main = _attackParticleSystem.main;
            
            gameObject.SetActive(true);
            await UniTask.CompletedTask;
        }
        
        public async UniTask ShowAttackPreparationAsync(string attackName, string attackDescription, Color color, CancellationToken token)
        {
            _attackParticleSystem.Play();
            _attackText.text = attackName;
            _attackDescriptionText.text = attackDescription;
            _main.startColor = color;

            // フェードインアニメーションを実行
            _attackText.alpha = 0f;
            _attackDescriptionText.alpha = 0f;
            
            var fadeInTask1 = _attackText.DOFade(1f, FadeInDuration).ToUniTask(cancellationToken: token);
            var fadeInTask2 = _attackDescriptionText.DOFade(1f, FadeInDuration).ToUniTask(cancellationToken: token);

            await UniTask.WhenAll(fadeInTask1, fadeInTask2);
        }
        
        public async UniTask HideAttackPreparationAsync(CancellationToken token)
        {
            _attackParticleSystem.Stop();
            
            // フェードアウトアニメーションを実行
            var fadeOutTask1 = _attackText.DOFade(0f, FadeInDuration).ToUniTask(cancellationToken: token);
            var fadeOutTask2 = _attackDescriptionText.DOFade(0f, FadeInDuration).ToUniTask(cancellationToken: token);

            await UniTask.WhenAll(fadeOutTask1, fadeOutTask2);
            
            gameObject.SetActive(false);
        }
        
        public async UniTask UpdateAttackViewAsync(string attackName, string attackDescription, Color color, CancellationToken token)
        {
            _attackText.text = attackName;
            _attackDescriptionText.text = attackDescription;
            _main.startColor = color;

            // 拡大アニメーションを実行
            var originalScale = _attackText.transform.localScale;
            var enlargedScale = originalScale * 1.2f; // 20%拡大

            _attackText.transform.localScale = enlargedScale;
            _attackDescriptionText.transform.localScale = enlargedScale;

            // 元の大きさに戻すアニメーションを実行
            var scaleDownTask1 = _attackText.transform.DOScale(originalScale, 0.15f).ToUniTask(cancellationToken: token);
            var scaleDownTask2 = _attackDescriptionText.transform.DOScale(originalScale, 0.15f).ToUniTask(cancellationToken: token);

            try
            {
                await UniTask.WhenAll(scaleDownTask1, scaleDownTask2);
            }
            finally
            {
                _attackText.transform.localScale = originalScale;
                _attackDescriptionText.transform.localScale = originalScale;
            }
        }
    }
}
