using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

using EchoEdge.App.Scene;
using EchoEdge.Domain.Battle;
using EchoEdge.Domain.Scene;
using EchoEdge.Infra.Audio;
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
        /// 現在のコンボ数
        /// </summary>
        private static int _combo = 0;

        /// <summary>
        /// このターンの攻撃時点までに発生した反射回数。
        /// 履歴ではなく、現在の攻撃で何回反射したかを表す。
        /// </summary>
        private static int _reflectionCount = 0;

        /// <summary>
        /// 反射1回あたりに加算されるダメージ倍率。(1.0 + 0.10 * n)
        /// </summary>
        private const float ReflectionDamageBonusPerCount = 0.10f;

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

        private static CancellationTokenSource _cancellationTokenSource;

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
        /// ピンチBGMに切り替えるHPの割合
        /// </summary>
        private const float PinchHpRate = 0.3f;

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

            // ピンチを脱した場合はピンチBGMを止める
            var playerHpRate = (float)_playerStatus.HP / _playerStatus.MaxHP;
            if (playerHpRate >= PinchHpRate && AudioManager.Instance != null)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource = new();
                AudioManager.Instance.FadeAddedBGMAsync(0.5f, _cancellationTokenSource.Token).Forget();
            }

            return healed;
        }

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
                _qteResult = await UIPresenter.Instance.AppearQTE(QTEKinds.Attack);
                _QTEFlug = true;
            }

            // コンボを増やしてテキストを更新する
            _combo++;
            float comboValue = 1.0f + (_combo - 1) * 0.1f;
            ComboPresenter.Instance?.SetCombo(_combo);

            // 反射回数に応じて基礎攻撃へ倍率を掛ける (1.0 + 0.10 * n)
            float reflectionValue = 1.0f + ReflectionDamageBonusPerCount * _reflectionCount;

            return await _enemyStatus.Damage((int)(_playerStatus.Attack * (comboValue * comboValue) * reflectionValue * _qteResult * attackTypeRate));
        }

        /// <summary>
        /// めちゃくちゃ早い一閃によるダメージを反映する。
        /// プレイヤーの攻撃パラメータの2.5倍を基礎ダメージとし、QTEやコンボ・反射倍率は乗算しない。
        /// </summary>
        /// <param name="attackTypeRate">攻撃種類ごとのダメージ倍率。反射攻撃を基準(1.0)とし、他の攻撃種類はこれより小さい値を渡して弱める。</param>
        public async static UniTask<(int damage, bool isDeath)> FlashAttackDamage(float attackTypeRate = 1.0f)
        {
            return await _enemyStatus.Damage((int)(_playerStatus.Attack * FlashAttackDamageMultiplier * attackTypeRate));
        }

        public async static UniTask<(int damage, bool isDeath)> PlayerDamage(float rate)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new();

            if (_isPlayerDeathStarted)
            {
                return (0, true);
            }

            if (!_QTEFlug)
            {
                _qteResult = await UIPresenter.Instance.AppearQTE(QTEKinds.Defend);
                _QTEFlug = true;
            } 

            // 防御中は、防御力で軽減した後の被ダメージを半減する
            var defendRate = _isDefending ? DefendDamageRate : 1.0f;

            // 防御QTE大成功で切り捨てにより0ダメージにならないよう、最低1ダメージは与える
            var result = await _playerStatus.Damage(Mathf.Max(1, (int)(_enemyStatus.Attack * rate * _qteResult)), defendRate);

            var playerHpRate = (float)_playerStatus.HP / _playerStatus.MaxHP;
            if (playerHpRate < PinchHpRate)
            {
                AudioManager.Instance.AddPlayBgmAsync(BgmAudioType.Pinch, true, _cancellationTokenSource.Token).Forget();
            }
            else
            {
                AudioManager.Instance.FadeAddedBGMAsync(0.5f, _cancellationTokenSource.Token).Forget();
            }


            PlayerStatusPresenter.Instance.SetPlayerHP(_playerStatus.HP, _playerStatus.MaxHP);
            PlayerView.Instance.Animator.SetTrigger("DamageT");
            AudioManager.Instance.PlaySe(BattleSeType.Damage);

            if (result.isDeath)
            {
                await StartPlayerDeathSequenceAsync();
            }

            return result;
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
