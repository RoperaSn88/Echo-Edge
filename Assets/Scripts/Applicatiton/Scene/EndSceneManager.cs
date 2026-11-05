using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

using EchoEdge.Domain.Scene;
using EchoEdge.Presenter.UI;

namespace EchoEdge.App.Scene
{
    /// <summary>
    /// エンディング（EndScene）を管理するクラス。
    /// 起動時に動画を再生しながら画面全体を明転し、「タイトルへ」がクリックされたら暗転して自分のシーンをアンロードする。
    /// 呼び出し側は <see cref="PlayAsync"/>（<see cref="SceneLoader.AdditiveLoadAndWait(GameScene)"/>）で
    /// アンロード（＝暗転完了）まで待機する。
    /// </summary>
    public class EndSceneManager : MonoBehaviour
    {
        [SerializeField, Tooltip("起動時に再生する動画")]
        private VideoPlayer _videoPlayer;

        [SerializeField, Tooltip("「タイトルへ」ボタン")]
        private PhaseButton _titleButton;

        [SerializeField, Tooltip("画面全体を覆う黒いパネル（明転・暗転に使う）")]
        private Image _fadePanel;

        [SerializeField, Tooltip("起動時の明転にかける時間（秒）")]
        private float _fadeInDuration = 1.0f;

        [SerializeField, Tooltip("「タイトルへ」クリック後の暗転にかける時間（秒）")]
        private float _fadeOutDuration = 1.0f;

        [SerializeField, Tooltip("「タイトルへ」ボタンの表示テキスト")]
        private string _titleButtonText = "タイトルへ";

        private bool _isClosing;

        /// <summary>
        /// EndScene を追加ロードし、「タイトルへ」で暗転してシーンがアンロードされるまで待機する。
        /// </summary>
        public static UniTask PlayAsync()
        {
            return SceneLoader.AdditiveLoadAndWait(GameScene.End);
        }

        private void Awake()
        {
            // 最初のフレームで中身が見えないよう、明転前は真っ黒にしておく
            SetFadeAlpha(1f);
            _fadePanel.gameObject.SetActive(true);

            _titleButton.Initialize();
            _titleButton.Interactable = false;
            if (_titleButtonText.Length > 0)
            {
                _titleButton.Text.text = _titleButtonText;
            }
            _titleButton.OnClick = OnClickTitle;
        }

        private void Start()
        {
            StartAsync(destroyCancellationToken).Forget();
        }

        private async UniTask StartAsync(CancellationToken token)
        {
            if (_videoPlayer != null)
            {
                _videoPlayer.Play();
            }

            await _fadePanel.DOFade(0f, _fadeInDuration).SetUpdate(true).ToUniTask(cancellationToken: token);
            _fadePanel.gameObject.SetActive(false);

            if (!_isClosing)
            {
                _titleButton.Interactable = true;
            }
        }

        private void OnClickTitle()
        {
            if (_isClosing) return;
            _isClosing = true;
            CloseAsync(destroyCancellationToken).Forget();
        }

        private async UniTask CloseAsync(CancellationToken token)
        {
            _titleButton.Interactable = false;

            _fadePanel.DOKill();
            _fadePanel.gameObject.SetActive(true);
            await _fadePanel.DOFade(1f, _fadeOutDuration).SetUpdate(true).ToUniTask(cancellationToken: token);

            if (_videoPlayer != null)
            {
                _videoPlayer.Stop();
            }

            // アンロードされると PlayAsync（AdditiveLoadAndWait）の待機が終わる
            SceneLoader.Unload(GameScene.End);
        }

        private void SetFadeAlpha(float alpha)
        {
            var c = _fadePanel.color;
            c.a = alpha;
            _fadePanel.color = c;
        }

        private void OnDestroy()
        {
            if (_fadePanel != null) _fadePanel.DOKill();
        }
    }
}
