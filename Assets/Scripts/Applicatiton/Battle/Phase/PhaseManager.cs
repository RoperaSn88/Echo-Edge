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
