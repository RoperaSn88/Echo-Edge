using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

using EchoEdge.App.Scene;
using EchoEdge.Domain.Battle;
using EchoEdge.Domain.Scene;
using EchoEdge.Infra.Audio;
using EchoEdge.Presenter.Battle;
using EchoEdge.Presenter.Player;
using EchoEdge.Presenter.UI;

namespace EchoEdge.App.Battle
{
    public class BattleManager : MonoBehaviour
    {
        [SerializeField]
        private static BattleStatus _playerStatus;

        public static BattleStatus PlayerStatus => _playerStatus;

        [SerializeField]
        private static BattleStatus _enemyStatus;

        public static BattleStatus EnemyStatus => _enemyStatus;

        /// <summary>
        /// QTEを実行するためのフラグ
        /// 1回の攻撃につき一度だけ発動する。
        /// </summary>
        private static bool _QTEFlug = false;

        private static float _qteResult;

        /// <summary>
        /// 敵の攻撃を完全に防御したときに表示する文言
        /// </summary>
        private const string PerfectGuardText = "Guard!!";

        /// <summary>
        /// 現在のコンボ数
        /// </summary>
        private static int _combo = 0;

        /// <summary>
        /// このターンの攻撃時点までに発生した反射回数。
        /// 履歴ではなく、現在の攻撃で何回反射したかを表す。
        /// </summary>
        private static int _reflectionCount = 0;

        /// <summary>
        /// 反射1回あたりに基礎攻撃へ加算される値。(攻撃力 + 3 * n)
        /// </summary>
        private const int ReflectionAttackBonusPerCount = 3;

        /// <summary>
        /// 2コンボ目以降、コンボ1回あたりに基礎攻撃へ加算される値。(攻撃力 + 1 * (コンボ数 - 1))
        /// </summary>
        private const int ComboAttackBonusPerCount = 1;

        /// <summary>
        /// めちゃくちゃ早い一閃のダメージ倍率。プレイヤーの攻撃力の何倍を基礎ダメージとするか。
        /// </summary>
        private const float FlashAttackDamageMultiplier = 2.5f;

        private const float PlayerDeathHitStopTimeScale = 0.05f;
        private const float PlayerDeathHitStopDurationSeconds = 0.2f;
        private const float PlayerDeathFadeDurationSeconds = 2.0f;

        private static bool _isPlayerDeathStarted;

        public static int Combo => _combo;

        /// <summary>
        /// このターンの攻撃時点までに発生した反射回数。
        /// </summary>
        public static int ReflectionCount => _reflectionCount;

        /// <summary>
        /// 防御中の被ダメージ倍率
        /// </summary>
        private const float DefendDamageRate = 0.5f;

        /// <summary>
        /// 防御した次のプレイヤーフェーズで増える反射回数
        /// </summary>
        private const int DefendReflectBonus = 1;

        /// <summary>
        /// 1回の戦闘（ステージ）で回復できる回数
        /// </summary>
        private const int MaxHealCount = 2;

        /// <summary>
        /// 防御中か。防御したターンのエネミーフェーズの間だけ true になり、被ダメージを半減する。
        /// </summary>
        private static bool _isDefending;
        public static bool IsDefending => _isDefending;

        /// <summary>
        /// 次のプレイヤーフェーズで反射回数を増やす予約があるか
        /// </summary>
        private static bool _isReflectBonusPending;

        /// <summary>
        /// 現在、防御による反射回数の増加がプレイヤーのステータスに反映されているか
        /// </summary>
        private static bool _isReflectBonusActive;

        /// <summary>
        /// 回復の残り回数
        /// </summary>
        private static int _healRemaining = MaxHealCount;
        public static int HealRemaining => _healRemaining;

        public static void RegisterEnemy(BattleStatus targetStatus)
        {
            _enemyStatus = targetStatus;
        }

        public static void RegisterPlayer(BattleStatus targetStatus)
        {
            _playerStatus = targetStatus;
            _isPlayerDeathStarted = false;

            // 新しい戦闘の開始時に、防御・回復の状態を初期化する
            _isDefending = false;
            _isReflectBonusPending = false;
            _isReflectBonusActive = false;
            _healRemaining = MaxHealCount;
        }

        /// <summary>
        /// 防御を開始する。このターンのエネミーフェーズの被ダメージを半減し、次のプレイヤーフェーズの反射回数を増やす予約をする。
        /// </summary>
        public static void StartDefend()
        {
            _isDefending = true;
            _isReflectBonusPending = true;
        }

        /// <summary>
        /// 防御による被ダメージ半減を終了する。エネミーフェーズの終了時に呼ぶ。
        /// </summary>
        public static void EndDefend()
        {
            _isDefending = false;
        }

        /// <summary>
        /// 予約されている反射回数の増加をプレイヤーのステータスに反映する。プレイヤーフェーズの開始時に呼ぶ。
        /// 同じターン内で何度呼ばれても1回だけ反映する。
        /// </summary>
        public static void ApplyPendingReflectBonus()
        {
            if (!_isReflectBonusPending || _isReflectBonusActive || _playerStatus == null) return;

            _playerStatus.ChangeMove(DefendReflectBonus);
            _isReflectBonusPending = false;
            _isReflectBonusActive = true;
        }

        /// <summary>
        /// 反映中の反射回数の増加を元に戻す。プレイヤーのターンが終わったとき（エネミーフェーズ開始時・次ウェーブ移行時）に呼ぶ。
        /// </summary>
        public static void ClearReflectBonus()
        {
            if (!_isReflectBonusActive || _playerStatus == null) return;

            _playerStatus.ChangeMove(-DefendReflectBonus);
            _isReflectBonusActive = false;
        }

        /// <summary>
        /// プレイヤーのバフの残りターンを減らす。プレイヤーのターンが終わったとき（エネミーフェーズ開始時・次ウェーブ移行時）に呼ぶ。
        /// </summary>
        public static void TickPlayerBuffs()
        {
            _playerStatus?.TickBuffs();
        }

        /// <summary>
        /// 回復の回数を1回消費する。
        /// </summary>
        /// <returns>消費できたか（残り回数が0なら false）</returns>
        public static bool TryConsumeHeal()
        {
            if (_healRemaining <= 0) return false;

            _healRemaining--;
            return true;
        }

        /// <summary>
        /// プレイヤーのHPを最大HPに対する割合で回復し、HPの表示とピンチBGMを更新する。
        /// </summary>
        /// <param name="rate">最大HPに対する回復量の割合</param>
        /// <returns>実際に回復した量</returns>
        public static int HealPlayer(float rate)
        {
            var before = _playerStatus.HP;
            _playerStatus.Heal((int)(_playerStatus.MaxHP * rate));
            var healed = _playerStatus.HP - before;

            PlayerStatusPresenter.Instance.SetPlayerHP(_playerStatus.HP, _playerStatus.MaxHP);

            // ピンチを脱した場合はピンチBGMを止める（ボス戦では通常のボスBGMを最初から再生する）
            BattleBgmController.UpdatePinch((float)_playerStatus.HP / _playerStatus.MaxHP);

            return healed;
        }

        /// <summary>
        /// 攻撃QTEを表示中か。追加反射の分度器QTEと入力が重ならないように参照する
        /// </summary>
        public static bool IsAttackQTEPlaying { get; private set; }

        public static void ResetQTE()
        {
            _QTEFlug = false;
        }

        /// <summary>
        /// コンボをリセットしてコンボテキストを非表示にする
        /// </summary>
        public static void ResetCombo()
        {
            _combo = 0;
            ComboPresenter.Instance?.ResetCombo();
        }

        /// <summary>
        /// 現在の攻撃で発生した反射回数を設定する。
        /// プレイヤーの移動ループから、反射段階ごとに呼び出す。
        /// </summary>
        /// <param name="count">攻撃時点までに発生した反射回数 (0以上)</param>
        public static void SetReflectionCount(int count)
        {
            _reflectionCount = count < 0 ? 0 : count;
        }

        /// <summary>
        /// 反射回数をリセットする。
        /// </summary>
        public static void ResetReflectionCount()
        {
            _reflectionCount = 0;
        }

        /// <param name="attackTypeRate">攻撃種類ごとのダメージ倍率。反射攻撃を基準(1.0)とし、他の攻撃種類はこれより小さい値を渡して弱める。</param>
        public static async UniTask<(int damage, bool isDeath)> EnemyDamage(float attackTypeRate = 1.0f)
        {
            if (!_QTEFlug)
            {
                IsAttackQTEPlaying = true;
                try
                {
                    _qteResult = await UIPresenter.Instance.AppearQTE(QTEKinds.Attack);
                }
                finally
                {
                    IsAttackQTEPlaying = false;
                }
                _QTEFlug = true;
            }

            // コンボを増やしてテキストを更新する
            _combo++;
            float comboMultiplier = DamageCalculationService.CalculateComboMultiplier(_combo);
            ComboPresenter.Instance?.SetCombo(_combo);

            // 反射回数とコンボ数に応じて基礎攻撃へ加算する (攻撃力 + 3 * 反射回数 + 1 * (コンボ数 - 1))
            int baseAttack = _playerStatus.Attack
                + ReflectionAttackBonusPerCount * _reflectionCount
                + ComboAttackBonusPerCount * (_combo - 1);

            return await _enemyStatus.Damage((int)(baseAttack * comboMultiplier * _qteResult * attackTypeRate));
        }

        /// <summary>
        /// めちゃくちゃ早い一閃によるダメージを反映する。
        /// プレイヤーの攻撃パラメータの2.5倍を基礎ダメージとし、QTEやコンボ・反射の補正は適用しない。
        /// </summary>
        /// <param name="attackTypeRate">攻撃種類ごとのダメージ倍率。反射攻撃を基準(1.0)とし、他の攻撃種類はこれより小さい値を渡して弱める。</param>
        public async static UniTask<(int damage, bool isDeath)> FlashAttackDamage(float attackTypeRate = 1.0f)
        {
            return await _enemyStatus.Damage((int)(_playerStatus.Attack * FlashAttackDamageMultiplier * attackTypeRate));
        }

        /// <summary>
        /// 敵の攻撃によるプレイヤーへのダメージを反映する。
        /// 攻撃の瞬間（敵の3度目の点滅）にプレイヤーが防御状態だった場合は完全に防御し、ダメージ 0 を返す。
        /// </summary>
        /// <param name="rate">攻撃ごとのダメージ倍率</param>
        public async static UniTask<(int damage, bool isDeath)> PlayerDamage(float rate)
        {
            if (_isPlayerDeathStarted)
            {
                return (0, true);
            }

            if (EnemyAttackGuard.ConsumeResult())
            {
                PlayPerfectGuard();
                return (0, false);
            }

            // 防御中は、防御力で軽減した後の被ダメージを半減する
            var defendRate = _isDefending ? DefendDamageRate : 1.0f;

            // ピンチ状態でなければ、致死ダメージを受けてもHPを1残す（食いしばり）
            var isPinchBeforeDamage = BattleBgmController.IsPinch((float)_playerStatus.HP / _playerStatus.MaxHP);

            // 切り捨てにより0ダメージにならないよう、最低1ダメージは与える（0 は完全防御のときだけ）
            var result = await _playerStatus.Damage(Mathf.Max(1, (int)(_enemyStatus.Attack * rate)), defendRate, !isPinchBeforeDamage);

            BattleBgmController.UpdatePinch((float)_playerStatus.HP / _playerStatus.MaxHP);

            PlayerStatusPresenter.Instance.SetPlayerHP(_playerStatus.HP, _playerStatus.MaxHP);
            PlayerView.Instance.Animator.SetTrigger("DamageT");

            // 防御行動中（被ダメージ半減）は軽めの SE にする
            AudioManager.Instance.PlaySe(_isDefending ? BattleSeType.ModerateDamage : BattleSeType.HeavyDamage);

            if (result.isDeath)
            {
                await StartPlayerDeathSequenceAsync();
            }

            return result;
        }

        /// <summary>
        /// <see cref="PlayerDamage"/> を行い、プレイヤーの位置にダメージテキストを出す。
        /// 完全に防御したときはダメージテキストを出さない（代わりに「Guard!!」が出る）。
        /// </summary>
        /// <param name="rate">攻撃ごとのダメージ倍率</param>
        public async static UniTask<(int damage, bool isDeath)> PlayerDamageWithText(float rate)
        {
            var result = await PlayerDamage(rate);

            if (result.damage > 0 && UIPresenter.Instance != null && PlayerController.Instance != null)
            {
                UIPresenter.Instance.AppearDamageText($"{result.damage}", PlayerController.Instance.transform.position).Forget();
            }

            return result;
        }

        /// <summary>
        /// 敵の攻撃を完全に防御したときの演出
        /// </summary>
        private static void PlayPerfectGuard()
        {
            AudioManager.Instance.PlaySe(BattleSeType.QTE_Perfect);

            if (UIPresenter.Instance != null)
            {
                UIPresenter.Instance.AppearQTEResult(PerfectGuardText).Forget();
            }

            if (PlayerView.Instance != null)
            {
                PlayerView.Instance.PlayDefendParticle().Forget();
            }
        }

        private static async UniTask StartPlayerDeathSequenceAsync()
        {
            if (_isPlayerDeathStarted)
            {
                return;
            }

            _isPlayerDeathStarted = true;
            ResetQTE();
            ResetCombo();

            Time.timeScale = PlayerDeathHitStopTimeScale;
            await UniTask.Delay(TimeSpan.FromSeconds(PlayerDeathHitStopDurationSeconds), ignoreTimeScale: true);
            Time.timeScale = 1.0f;

            var fadeTask = UIPresenter.Instance != null
                ? UIPresenter.Instance.FadeOutAsync(PlayerDeathFadeDurationSeconds)
                : UniTask.CompletedTask;
            var fadeBgmTask = AudioManager.Instance != null
                ? AudioManager.Instance.FadeBGMAsync(PlayerDeathFadeDurationSeconds, CancellationToken.None)
                : UniTask.CompletedTask;

            await UniTask.WhenAll(fadeTask, fadeBgmTask);

            await SceneLoader.AdditiveLoadAsync(GameScene.Preparing);

            SceneLoader.Unload(GameScene.MainGame);
        }
    }
}
