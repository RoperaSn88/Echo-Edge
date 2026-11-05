using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
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

        [SerializeField, Tooltip("シーン全体をまとめる CanvasGroup。alpha 0→1 で明転、1→0 で暗転する（背後の MainGame は暗転済み）")]
        private CanvasGroup _sceneGroup;

        [SerializeField, Tooltip("起動時の明転にかける時間（秒）")]
        private float _fadeInDuration = 1.0f;

        [SerializeField, Tooltip("「タイトルへ」クリック後の暗転にかける時間（秒）")]
        private float _fadeOutDuration = 1.0f;

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
            // 最初のフレームで中身が見えないよう、明転前は透明（背後の暗転画面のまま）にしておく
            _sceneGroup.alpha = 0f;

            _titleButton.Initialize();
            _titleButton.Interactable = false;
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

            await _sceneGroup.DOFade(1f, _fadeInDuration).SetUpdate(true).ToUniTask(cancellationToken: token);

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

            _sceneGroup.DOKill();
            await _sceneGroup.DOFade(0f, _fadeOutDuration).SetUpdate(true).ToUniTask(cancellationToken: token);

            if (_videoPlayer != null)
            {
                _videoPlayer.Stop();
            }

            // アンロードされると PlayAsync（AdditiveLoadAndWait）の待機が終わる
            SceneLoader.Unload(GameScene.End);
        }

        private void OnDestroy()
        {
            if (_sceneGroup != null) _sceneGroup.DOKill();
        }
    }
}
