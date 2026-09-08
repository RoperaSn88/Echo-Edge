using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

using EchoEdge.Domain.Phase;

namespace EchoEdge.App.Battle
{
    public class PhaseManager : MonoBehaviour
    {
        async void Start()
        {
            try
            {
                await Phasing(destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                // PhaseManager が破棄されたためフェーズループをキャンセルしました
                Debug.Log("PhaseManager: フェーズループをキャンセルしました");
            }
        }

        async UniTask Phasing(CancellationToken cancellationToken)
        {
            IPhase phase = StartPhase.Instance;
            try
            {
                // 新しいバトル開始時に、前回のクリア演出シーケンスの状態をリセットする。
                // GameClearManager は static なのでシーンをまたいでフラグが残り、
                // これを消さないと下の while ループが即座に抜けて 2 回目のバトルが始まらない。
                GameClearManager.ResetGameClearSequenceState();

                // クリア演出シーケンスが始まったら、次のフェーズへ進めずループを抜ける。
                // これにより読み込んだシナリオシーンの裏でバトルサイクルが回り続けるのを防ぐ。
                while (!GameClearManager.IsGameClearSequenceRunning)
                {
                    phase = await phase.WaitPhase();
                }

                Debug.Log("PhaseManager: クリア演出シーケンス開始のためフェーズループを終了しました");
            }
            catch (OperationCanceledException)
            {
                // フェーズの待機中にキャンセルされた場合はループを抜ける
                Debug.Log("PhaseManager: フェーズの待機中にキャンセルされました");
            }
        }
    }
}
