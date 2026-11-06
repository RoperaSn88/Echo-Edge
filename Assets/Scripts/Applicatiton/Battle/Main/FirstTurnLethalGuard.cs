using System.Collections.Generic;

using EchoEdge.Domain.Battle;
using EchoEdge.Domain.Preparing;

namespace EchoEdge.App.Battle
{
    /// <summary>
    /// 特定のステージで、特定の敵が最初のプレイヤーフェーズだけ致死ダメージを受けてもHPを1残す（食いしばり）よう管理するstaticクラス。
    /// 現在はステージ1のブースターが対象。
    /// ステージ開始時に <see cref="ResetForStage"/>、最初のプレイヤーフェーズが終わったら <see cref="EndFirstTurn"/> を呼ぶ。
    /// </summary>
    public static class FirstTurnLethalGuard
    {
        /// <summary>
        /// 食いしばりを付ける対象 (ステージ番号, 敵の種類)
        /// </summary>
        private static readonly (int stage, EnemyKinds enemyKind)[] Targets =
        {
            (1, EnemyKinds.Booster),
        };

        /// <summary>
        /// 食いしばりを付けたステータス。最初のプレイヤーフェーズが終わったら解除する
        /// </summary>
        private static readonly List<BattleStatus> _guardedStatuses = new();

        /// <summary>
        /// 最初のプレイヤーフェーズがすでに終わったか
        /// </summary>
        private static bool _isFirstTurnEnded;

        /// <summary>
        /// ステージ開始時に呼び、状態を初期化する
        /// </summary>
        public static void ResetForStage()
        {
            ReleaseAll();
            _isFirstTurnEnded = false;
        }

        /// <summary>
        /// 敵のステータス読み込み時に呼ぶ。対象なら食いしばりを付ける
        /// </summary>
        public static void Apply(EnemyKinds enemyKind, BattleStatus status)
        {
            if (status == null || _isFirstTurnEnded || !IsTarget(StageData.Level, enemyKind))
            {
                return;
            }

            status.SurviveLethal = true;
            _guardedStatuses.Add(status);
        }

        /// <summary>
        /// 最初のプレイヤーフェーズが終わったら呼び、付けた食いしばりを解除する
        /// </summary>
        public static void EndFirstTurn()
        {
            if (_isFirstTurnEnded)
            {
                return;
            }

            _isFirstTurnEnded = true;
            ReleaseAll();
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

        private static void ReleaseAll()
        {
            foreach (var status in _guardedStatuses)
            {
                status.SurviveLethal = false;
            }
            _guardedStatuses.Clear();
        }
    }
}
