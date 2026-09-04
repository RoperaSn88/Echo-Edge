using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

using EchoEdge.Domain.Battle;

namespace EchoEdge.Infra.Battle
{
    /// <summary>
    /// EnemyInfo.csv からエネミーのステータスを読み込むローダー
    /// </summary>
    public static class EnemyStatusLoader
    {
        private const string CsvPath = "Assets/Addressables/EnemyInfo.csv";

        // Size 列のインデックス（未記載の古い行との後方互換のため、存在チェックしてから読む）
        private const int SizeColumnIndex = 8;

        // CSV を初回読み込み時にキャッシュする (ID → 各列の値)
        private static Dictionary<int, string[]> _cache;

        private static async UniTask<Dictionary<int, string[]>> GetCacheAsync()
        {
            if (_cache != null) return _cache;

            _cache = new Dictionary<int, string[]>();

            var csv = await Addressables.LoadAssetAsync<TextAsset>(CsvPath);
            if (csv == null)
            {
                Debug.LogError("EnemyInfo.csv が見つかりません");
                return _cache;
            }

            // Windows (\r\n) と Unix (\n) の両改行に対応
            var lines = csv.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // 1行目はヘッダーなのでスキップ
            for (int i = 1; i < lines.Length; i++)
            {
                var cols = lines[i].Split(',');
                if (cols.Length < 8) continue;

                if (!int.TryParse(cols[0].Trim(), out int rowId)) continue;

                _cache[rowId] = cols;
            }

            return _cache;
        }

        /// <summary>
        /// 指定した ID に対応するパラメータを EnemyInfo.csv から読み取り、BattleStatus に反映する
        /// </summary>
        /// <param name="id">読み取る行の ID</param>
        /// <returns>読み取りに成功した場合は true</returns>
        public static async UniTask<BattleStatus> TryLoad(int id)
        {
            var cache = await GetCacheAsync();

            if (!cache.TryGetValue(id, out var cols))
            {
                Debug.LogWarning($"ID {id} のエネミーが EnemyInfo.csv に見つかりません");
                return null;
            }

            try
            {
                int hp          = int.Parse(cols[1].Trim());
                int attack      = int.Parse(cols[2].Trim());
                int defend      = int.Parse(cols[3].Trim());
                int parsedMove  = int.Parse(cols[4].Trim());
                byte move       = (byte)Mathf.Clamp(parsedMove, byte.MinValue, byte.MaxValue);
                var movePattern = (MovePattern)Enum.Parse(typeof(MovePattern), cols[5].Trim());
                int experience  = int.Parse(cols[6].Trim());
                int energy      = int.Parse(cols[7].Trim());
                var size        = ParseSize(cols);

                var status = new BattleStatus(hp, attack, defend, move, movePattern, experience, energy, size);
                return status;
            }
            catch (Exception e)
            {
                Debug.LogError($"EnemyInfo.csv の ID {id} の行を解析できませんでした: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 指定した ID のエネミーサイズだけを取得する。
        /// マップへの登録はステータス全体の読み込みより先に行う必要があるケース（複数マスを占有するエネミーの初期配置など）で使用する。
        /// </summary>
        /// <param name="id">読み取る行の ID</param>
        public static async UniTask<EnemySize> TryLoadSize(int id)
        {
            var cache = await GetCacheAsync();
            return cache.TryGetValue(id, out var cols) ? ParseSize(cols) : EnemySize.Default;
        }

        /// <summary>
        /// Size 列を読み取る。列が存在しない、もしくは不正な値の場合は Default とする（後方互換のため）。
        /// </summary>
        private static EnemySize ParseSize(string[] cols)
        {
            if (cols.Length <= SizeColumnIndex) return EnemySize.Default;

            return Enum.TryParse<EnemySize>(cols[SizeColumnIndex].Trim(), out var size)
                ? size
                : EnemySize.Default;
        }
    }
}
