using System;

using UnityEngine;

namespace EchoEdge.Infra.Tutorial
{
    /// <summary>
    /// 各フェーズで再生されるチュートリアル（<see cref="TutorialKinds"/>）を
    /// 再生済みかどうか PlayerPrefs で保存・読み込みするマネージャー。
    ///
    /// 保存キーは「TutorialCompleted + <see cref="TutorialKinds"/> の名前」で統一しており、
    /// 各フェーズが個別に文字列キーを保持せずに済むよう一元管理する。
    /// （例: <see cref="TutorialKinds.FirstBattle"/> → "TutorialCompletedFirstBattle"）
    /// 既存フェーズが使用しているキー文字列と互換のため、保存済みデータはそのまま利用できる。
    /// </summary>
    public static class TutorialSaveManager
    {
        private const string KeyPrefix = "TutorialCompleted";

        /// <summary>
        /// 指定したチュートリアルの保存キーを取得する。
        /// </summary>
        public static string GetKey(TutorialKinds kind)
        {
            return KeyPrefix + kind;
        }

        /// <summary>
        /// 指定したチュートリアルが再生済みかどうかを返す。
        /// 保存データがない（初回）の場合は false を返す。
        /// </summary>
        public static bool IsCompleted(TutorialKinds kind)
        {
            return PlayerPrefs.GetInt(GetKey(kind), 0) != 0;
        }

        /// <summary>
        /// 指定したチュートリアルを再生済みとして保存する。
        /// </summary>
        public static void MarkCompleted(TutorialKinds kind)
        {
            PlayerPrefs.SetInt(GetKey(kind), 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// チュートリアルをこれから再生すべきかどうかを判定する。
        /// 未再生の場合は再生済みフラグを先に立てたうえで true を返し、
        /// 再生済みの場合は何もせず false を返す。
        ///
        /// 中断（例外）されても再度再生されないよう、既存フェーズの実装に合わせて
        /// 再生前にフラグを保存する。
        /// </summary>
        /// <returns>これからチュートリアルを再生する場合は true。</returns>
        public static bool TryBeginTutorial(TutorialKinds kind)
        {
            if (IsCompleted(kind))
            {
                return false;
            }

            MarkCompleted(kind);
            return true;
        }

        /// <summary>
        /// 指定したチュートリアルの保存データを削除する。
        /// </summary>
        public static void DeleteSavedData(TutorialKinds kind)
        {
            PlayerPrefs.DeleteKey(GetKey(kind));
            PlayerPrefs.Save();
        }

        /// <summary>
        /// すべてのチュートリアルの保存データを削除する。
        /// </summary>
        public static void DeleteAllSavedData()
        {
            foreach (TutorialKinds kind in Enum.GetValues(typeof(TutorialKinds)))
            {
                PlayerPrefs.DeleteKey(GetKey(kind));
            }
            PlayerPrefs.Save();
        }
    }
}
