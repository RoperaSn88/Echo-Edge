using UnityEngine;

namespace EchoEdge.Infra.Scenario
{
    /// <summary>
    /// プロローグシナリオを再生済みかどうかを PlayerPrefs で保存・読み込みするマネージャー。
    /// 初回起動時にのみ Scenario シーンでプロローグを再生し、以降は Preparing シーンを
    /// 直接起動するかどうかの判断に使う。
    /// </summary>
    public static class PrologueSaveManager
    {
        private const string ProloguePlayedKey = "Prologue.Played";

        /// <summary>
        /// プロローグシナリオを再生済みか確認する。未再生（初回起動）の場合は false を返す。
        /// </summary>
        public static bool HasPlayedPrologue()
        {
            return PlayerPrefs.GetInt(ProloguePlayedKey, 0) != 0;
        }

        /// <summary>
        /// プロローグシナリオを再生済みとして保存する。
        /// </summary>
        public static void SaveProloguePlayed()
        {
            PlayerPrefs.SetInt(ProloguePlayedKey, 1);
            PlayerPrefs.Save();
        }

        public static void DeleteAllSavedData()
        {
            PlayerPrefs.DeleteKey(ProloguePlayedKey);
        }
    }
}
