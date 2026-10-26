using System.Threading;
using Cysharp.Threading.Tasks;

using EchoEdge.Domain.Phase;
using EchoEdge.Infra.Audio;

namespace EchoEdge.App.Battle
{
    /// <summary>
    /// バトル中の BGM（通常 / ボス）とピンチ演出の切り替えを管理するstaticクラス。
    /// 通常 BGM: ピンチになると追加 BGM（Pinch）を同じ再生位置からフェードインで重ね、脱するとフェードアウトする。
    /// ボス BGM: ピンチになると BossPinch に切り替え、脱すると Boss を最初から再生し直す。
    /// クリア条件が「ボス撃破」のウェーブをボス戦として扱う。
    /// </summary>
    public static class BattleBgmController
    {
        /// <summary>
        /// ピンチ BGM に切り替えるHPの割合
        /// </summary>
        private const float PinchHpRate = 0.3f;

        /// <summary>
        /// 通常 BGM でピンチを脱したときに、追加 BGM をフェードアウトする時間
        /// </summary>
        private const float PinchLayerFadeOutSeconds = 0.5f;

        private static bool _isBossBgm;
        private static bool _isPinch;
        private static CancellationTokenSource _pinchLayerCancellation;

        /// <summary>
        /// 現在ボス BGM を流しているか
        /// </summary>
        public static bool IsBossBgm => _isBossBgm;

        /// <summary>
        /// 現在のウェーブがボス戦か（クリア条件が「ボス撃破」か）
        /// </summary>
        public static bool IsBossWave => GameClearManager.CurrentConditionType == StageClearConditionType.DefeatBoss;

        /// <summary>
        /// ステージ開始時に呼ぶ。ピンチ状態をリセットし、最初のウェーブに合わせた BGM を再生する。
        /// </summary>
        public static async UniTask PlayStageStartBgmAsync()
        {
            _isPinch = false;
            _isBossBgm = IsBossWave;
            CancelPinchLayerFade();

            if (AudioManager.Instance == null) return;

            if (_isBossBgm)
            {
                AudioManager.Instance.PreloadBgmAsync(BgmAudioType.BossPinch).Forget();
                await AudioManager.Instance.PlayBgm(BgmAudioType.Boss, true);
            }
            else
            {
                await AudioManager.Instance.PlayBgm(BgmAudioType.Battle, true);
            }
        }

        /// <summary>
        /// 次のウェーブの読み込み後に呼ぶ。通常 / ボスが切り替わる場合だけ BGM を変える（同じ種類なら流し続ける）。
        /// </summary>
        public static async UniTask ChangeWaveBgmAsync()
        {
            var isBossWave = IsBossWave;
            if (isBossWave == _isBossBgm || AudioManager.Instance == null) return;

            _isBossBgm = isBossWave;

            // 通常 BGM のピンチ用に重ねていた追加 BGM は、モードが変わるときに止める
            CancelPinchLayerFade();
            await AudioManager.Instance.FadeAddedBGMAsync(0.0f, CancellationToken.None);

            if (_isBossBgm)
            {
                AudioManager.Instance.PreloadBgmAsync(BgmAudioType.BossPinch).Forget();
                await AudioManager.Instance.PlayBgm(_isPinch ? BgmAudioType.BossPinch : BgmAudioType.Boss, true);
            }
            else
            {
                await AudioManager.Instance.PlayBgm(BgmAudioType.Battle, true);
                if (_isPinch)
                {
                    StartPinchLayer();
                }
            }
        }

        /// <summary>
        /// プレイヤーのHPが変わったときに呼ぶ。ピンチに入った / 脱したときだけ BGM を切り替える。
        /// </summary>
        /// <param name="hpRate">最大HPに対する現在HPの割合</param>
        public static void UpdatePinch(float hpRate)
        {
            var isPinch = hpRate < PinchHpRate;
            if (isPinch == _isPinch || AudioManager.Instance == null) return;

            _isPinch = isPinch;

            if (_isBossBgm)
            {
                // ボス BGM はフェードで重ねず曲ごと切り替える。回復時は Boss を最初から再生する
                AudioManager.Instance.PlayBgm(_isPinch ? BgmAudioType.BossPinch : BgmAudioType.Boss, true).Forget();
                return;
            }

            if (_isPinch)
            {
                StartPinchLayer();
            }
            else
            {
                CancelPinchLayerFade();
                _pinchLayerCancellation = new();
                AudioManager.Instance.FadeAddedBGMAsync(PinchLayerFadeOutSeconds, _pinchLayerCancellation.Token).Forget();
            }
        }

        private static void StartPinchLayer()
        {
            CancelPinchLayerFade();
            _pinchLayerCancellation = new();
            AudioManager.Instance.AddPlayBgmAsync(BgmAudioType.Pinch, true, _pinchLayerCancellation.Token).Forget();
        }

        private static void CancelPinchLayerFade()
        {
            _pinchLayerCancellation?.Cancel();
            _pinchLayerCancellation?.Dispose();
            _pinchLayerCancellation = null;
        }
    }
}
