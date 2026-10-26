using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

using EchoEdge.App.Battle;
using EchoEdge.Domain.Battle;

namespace EchoEdge.Domain.Phase
{
    /// <summary>
    /// 「特定のボスを撃破する」をクリア条件とするタスク。
    /// 対象のボスはウェーブCSVの1行目の2列目で、EnemyInfo.csv の ID（<see cref="EnemyKinds"/> の値）で指定する。
    /// その ID の敵を倒した時点でクリアとなり、他の敵が残っていてもよい。
    /// </summary>
    public sealed class DefeatBossStageClearTask : IStageClearTask
    {
        public const StageClearConditionType ConditionType = StageClearConditionType.DefeatBoss;

        private const string BaseMessage = "残りのボスはあと";

        /// <summary>
        /// 倒す対象のボスの種別
        /// </summary>
        private EnemyKinds _targetKind = EnemyKinds.Invalid;

        /// <summary>
        /// 残りのボスの数。対象を倒すと 0 になる
        /// </summary>
        private int _remainingBossCount = 1;

        public string ObjectiveBaseText => BaseMessage;

        public string ObjectiveConditionValue => _remainingBossCount.ToString();

        public bool IsGameClearCondition => _remainingBossCount == 0;

        /// <summary>
        /// 倒す対象のボスを設定する。
        /// </summary>
        /// <param name="conditionValue">ボスの敵ID（EnemyInfo.csv の ID）</param>
        public void Initialize(int conditionValue)
        {
            _targetKind = ToEnemyKind(conditionValue);
            _remainingBossCount = 1;
            GameClearManager.UpdateText(ObjectiveBaseText, _remainingBossCount);
        }

        public void Subscribe()
        {
            DomainEventDispatcher.Register<EnemyDefeatedEvent>(OnEnemyDefeated);
        }

        public void Unsubscribe()
        {
            DomainEventDispatcher.Unregister<EnemyDefeatedEvent>(OnEnemyDefeated);
        }

        /// <summary>
        /// 敵IDをボスの種別に変換する。
        /// 不正な ID の場合は、クリアできないステージになるのを避けるため、どの敵を倒してもクリアとして扱う。
        /// </summary>
        private static EnemyKinds ToEnemyKind(int conditionValue)
        {
            if (conditionValue >= 0 && conditionValue <= byte.MaxValue
                && Enum.IsDefined(typeof(EnemyKinds), (byte)conditionValue)
                && (EnemyKinds)conditionValue != EnemyKinds.Invalid)
            {
                return (EnemyKinds)conditionValue;
            }

            Debug.LogWarning($"ウェーブCSVのボス撃破条件の敵IDが不正です: {conditionValue}。どの敵を倒してもクリアとして扱います。");
            return EnemyKinds.Invalid;
        }

        private async UniTask OnEnemyDefeated(EnemyDefeatedEvent e)
        {
            GameReward.UpdateLastEnemyPosition(e.Position.Height, e.Position.Width);
            GameReward.AddStageEarnedExperience(e.ExperienceReward);

            // 対象のボス以外を倒した場合は進捗を変えない
            if (_targetKind != EnemyKinds.Invalid && e.EnemyKind != _targetKind) return;

            await UpdateCondition();
        }

        private async UniTask UpdateCondition()
        {
            if (_remainingBossCount > 0)
            {
                _remainingBossCount--;
            }

            GameClearManager.UpdateText(ObjectiveBaseText, _remainingBossCount);

            if (!IsGameClearCondition) return;

            GameClearManager.SetStageClearCondition(true);

            if (!WaveManager.HasNextWave)
            {
                // クリア演出・シナリオ再生の完了まで await する。
                // ここを待つことでディスパッチ元（敵撃破処理）が止まり、
                // 演出中に後続のバトルサイクルが進むのを防ぐ。
                await GameClearManager.StartGameClearSequenceAsync();
            }
        }
    }
}
