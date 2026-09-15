using System.Collections.Generic;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;

namespace EchoEdge.Infra.Map
{
    /// <summary>
    /// Addressables のカタログから、登録済みの StageWaveSet（ウェーブ定義）の数を数える
    /// </summary>
    public static class StageWaveSetAvailabilityLoader
    {
        private static readonly Regex WaveSetAddressPattern =
            new Regex(@"^Assets/Addressables/StageWaves/WaveSet/StageWaveSet_(\d+)\.asset$");

        /// <summary>
        /// Addressables のカタログ（<see cref="Addressables.ResourceLocators"/>）を走査し、
        /// "Assets/Addressables/StageWaves/WaveSet/StageWaveSet_数字.asset" の形式で
        /// 登録されているアドレスの数（＝用意されているステージ数）を数える。
        /// カタログが未初期化の場合は内部で初期化してから数える。
        /// </summary>
        public static async UniTask<int> CountRegisteredStagesAsync()
        {
            await Addressables.InitializeAsync().ToUniTask();

            var foundStageNumbers = new HashSet<int>();
            foreach (var locator in Addressables.ResourceLocators)
            {
                foreach (var key in locator.Keys)
                {
                    if (key is string address)
                    {
                        var match = WaveSetAddressPattern.Match(address);
                        if (match.Success)
                        {
                            foundStageNumbers.Add(int.Parse(match.Groups[1].Value));
                        }
                    }
                }
            }

            return foundStageNumbers.Count;
        }
    }
}
