using EchoEdge.Domain.Battle;
using EchoEdge.Domain.Preparing;

namespace EchoEdge.App.Battle
{
    /// <summary>
    /// ステージ開始直後の最初のプレイヤーフェーズだけ、食いしばり（<see cref="SurviveLethalBuff"/>）を付ける敵を決める。
    /// 現在はステージ1のブースターが対象。
    /// <see cref="EnemyInitializeActivator"/> に登録して使う。
    /// </summary>
    public class FirstTurnSurviveLethal : IEnemyInitializeAction
    {
        private static FirstTurnSurviveLethal _instance;
        public static FirstTurnSurviveLethal Instance => _instance ??= new FirstTurnSurviveLethal();

        private FirstTurnSurviveLethal() { }

        /// <summary>
        /// バフの効果ターン数。エネミーフェーズ開始時の TickBuffs で消える
        /// </summary>
        private const int DurationTurns = 1;

        /// <summary>
        /// 食いしばりを付ける対象 (ステージ番号, 敵の種類)
        /// </summary>
        private static readonly (int stage, EnemyKinds enemyKind)[] Targets =
        {
            (1, EnemyKinds.Booster),
        };

        /// <summary>
        /// 敵のステータス初期化時に呼ばれる。対象なら1ターンの食いしばりバフを付ける。
        /// ステージ開始時に配置される最初のウェーブの敵だけが対象。
        /// </summary>
        public void OnEnemyInitialize(EnemyKinds enemyKind, BattleStatus status)
        {
            if (status == null || WaveManager.CurrentWaveIndex != 0 || !IsTarget(StageData.Level, enemyKind))
            {
                return;
            }

            status.AddBuff(new SurviveLethalBuff(), DurationTurns);
        }

        private static bool IsTarget(int stage, EnemyKinds enemyKind)
        {
            foreach (var target in Targets)
            {
                if (target.stage == stage && target.enemyKind == enemyKind)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
