using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

using EchoEdge.App.Battle;
using EchoEdge.Domain.Battle;
using EchoEdge.Infra.Audio;
using EchoEdge.Infra.Battle;
using EchoEdge.Infra.Camera;
using EchoEdge.Presenter.Player;
using EchoEdge.Presenter.UI;
using EchoEdge.Presenter.VFX;

namespace EchoEdge.Presenter.Battle
{
    public class BaseUnitView: MonoBehaviour, IDamageActivator, IUnitView, IDamageReflectableView, IDisposable
    {
        private const string EnemyAnimPath = "Assets/Addressables/Animator/";

        private int height;

        private int width;

        /// <summary>
        /// HPゲージのための最大HP
        /// </summary>
        private int _maxHP;

        [SerializeField]
        private Animator _animator;

        public Animator Animator => _animator;

        [SerializeField]
        private SpriteRenderer _renderer;

        /// <summary>
        /// prefab 時点の sprite のローカル座標。Offset はこの値を基準に適用する（プール再利用で累積させないため）。
        /// </summary>
        private Vector3 _rendererBaseLocalPosition;
        private bool _rendererBaseLocalPositionCaptured;

        /// <summary>
        /// prefab 時点の sprite のローカル回転。真上視点用に寝かせた後はこの値へ戻す。
        /// </summary>
        private Quaternion _rendererBaseLocalRotation = Quaternion.identity;
        private bool _rendererBaseLocalRotationCaptured;

        /// <summary>
        /// 真上視点(一閃準備フェーズ)で敵を見やすくするために sprite を寝かせる角度。
        /// </summary>
        private static readonly Quaternion TopDownSpriteLocalRotation = Quaternion.Euler(90f, 0f, 0f);

        /// <summary>
        /// 真上視点で寝かせる際、併せて画面下方向へずらす固定オフセット。
        /// 真上カメラでは Y 移動は視線軸方向で見えないためローカル Z（画面の上下）で下げる。
        /// また sprite 子の localPosition は Animator(Write Defaults)に毎フレーム固定されるため、
        /// この移動はルート Transform 側に掛ける。
        /// </summary>
        private const float TopDownDownwardOffset = 0.59f;

        /// <summary>
        /// 真上視点の寝かせ＋下げをトゥイーンさせる時間。
        /// </summary>
        private const float TopDownPoseTweenTime = 0.25f;

        /// <summary>
        /// 真上視点の寝かせ＋下げのトゥイーン（sprite 回転とルート Z 移動を同時再生）。切り替え時に前のものを止める。
        /// </summary>
        private Tween _topDownPoseTween;

        /// <summary>
        /// 真上視点へ入る直前のルート localPosition.z。抜けるときはここへ戻す。
        /// </summary>
        private float _rootPreTopDownLocalY;

        /// <summary>
        /// 真上視点用の寝かせ＋下げが適用中かどうか。
        /// </summary>
        private bool _isTopDownSpriteActive;

        [SerializeField]
        private CanvasGroup _canvasGroup;

        [SerializeField]
        private Image _healthBar;

        /// <summary>
        ///  移動に使用するベクトル
        /// </summary>
        private Vector3 _moveVec;

        /// <summary>
        /// アニメ中に攻撃を行うフラグ
        /// </summary>
        private bool _animationFlag;

        public bool AnimationFlag => _animationFlag;

        /// <summary>
        /// 死んだか
        /// </summary>
        private bool _isDeath;

        private const float MoveTime = 0.15f;
        private const float DeadFadeTime = 0.5f;

        /// <summary>
        /// 近距離攻撃時にプレイヤーの手前へ踏み込む際、プレイヤーとの間に空けるワールド距離
        /// </summary>
        private const float ApproachGap = 1.2f;

        /// <summary>
        /// 踏み込み・後退の移動時間
        /// </summary>
        private const float ApproachMoveTime = 0.25f;

        /// <summary>
        /// 踏み込む直前のワールド座標。攻撃終了後にここへ戻る。
        /// </summary>
        private Vector3 _preApproachPosition;

        /// <summary>
        /// 現在プレイヤーの手前へ踏み込んでいるか
        /// </summary>
        private bool _hasApproachedForAttack;

        /// <summary>
        /// ダメージ反映中のヒットストップに使う TimeScale
        /// </summary>
        private const float DamageHitStopTimeScale = 0.001f;

        /// <summary>
        /// HPゲージを追従させるアニメーション時間
        /// </summary>
        private const float HealthBarTweenTime = 0.5f;

        /// <summary>
        /// エナジー演出なしで死亡演出を再生する際の待機時間
        /// </summary>
        private const float DeathEffectTime = 0.7f;

        /// <summary>
        /// エナジー演出なしで被弾演出を再生する際の待機時間
        /// </summary>
        private const float DamageEffectTime = 0.5f;

        /// <summary>
        /// 回復演出の待機時間
        /// </summary>
        private const float HealEffectTime = 0.5f;

        public async UniTask SetAnimator(EnemyKinds enemyID)
        {
            var data = await Addressables.LoadAssetAsync<RuntimeAnimatorController>(EnemyAnimPath + enemyID + ".controller").ToUniTask();
            if (data != null)
            {
                _animator.runtimeAnimatorController = data;
            }
            else
            {
                Debug.LogWarning($"EnemyAnimPath {EnemyAnimPath + enemyID + ".controller"} のアニメーターを読み込めませんでした。");
            }
        }

        /// <summary>
        /// 表示位置を初期化する。UnitSpawner から呼び出す。
        /// </summary>
        /// <param name="h">配置する縦座標</param>
        /// <param name="w">配置する横座標</param>
        public virtual async UniTask Setup(int h, int w, EnemyKinds enemyID)
        {
            height = h;
            width = w;
            _isDeath = false;
            _animationFlag = false;
            _hasApproachedForAttack = false;
            if (_renderer != null)
            {
                var color = _renderer.color;
                color.a = 1f;
                _renderer.color = color;

                // プール再利用時に、前回の真上視点の寝かせ・下げが残らないよう prefab 時点の向きへ戻す
                // （下げオフセットの座標は直後の ApplySpriteOffset で上書きされる）
                _topDownPoseTween?.Kill();
                _isTopDownSpriteActive = false;
                CaptureRendererBaseLocalRotation();
                _renderer.transform.localRotation = _rendererBaseLocalRotation;
            }

            _healthBar.fillAmount = 1f;

            transform.localPosition = new Vector3(w, 0, h);

            // 2x2など複数マスを占有するエネミーは、専用モデルが用意されるまでの暫定対応として見た目を拡大する
            var size = await EnemyStatusLoader.TryLoadSize((int)enemyID);
            transform.localScale = Vector3.one * (int)size;

            // EnemyInfo.csv の Offset 分だけ sprite の高さをズラす（基本値 0）
            ApplySpriteOffset(await EnemyStatusLoader.TryLoadOffset((int)enemyID));

            await SetAnimator(enemyID);
            gameObject.SetActive(true);
        }

        /// <summary>
        /// EnemyInfo.csv の Offset 値だけ sprite のローカルY座標をズラす。
        /// prefab 時点の座標を基準に絶対値で設定するため、プール再利用でも累積しない。
        /// </summary>
        /// <param name="offset">ズラす高さ（基本値 0）</param>
        private void ApplySpriteOffset(float offset)
        {
            if (_renderer == null) return;

            var spriteTransform = _renderer.transform;
            if (!_rendererBaseLocalPositionCaptured)
            {
                _rendererBaseLocalPosition = spriteTransform.localPosition;
                _rendererBaseLocalPositionCaptured = true;
            }

            spriteTransform.localPosition = _rendererBaseLocalPosition + new Vector3(0f, offset, 0f);
        }

        /// <summary>
        /// prefab 時点の sprite ローカル回転を一度だけ記録する。
        /// </summary>
        private void CaptureRendererBaseLocalRotation()
        {
            if (_renderer == null || _rendererBaseLocalRotationCaptured) return;

            _rendererBaseLocalRotation = _renderer.transform.localRotation;
            _rendererBaseLocalRotationCaptured = true;
        }

        /// <summary>
        /// 真上視点フェーズ(一閃準備)用に sprite を寝かせる／prefab 時点の向きへ戻す。
        /// sprite の回転（X=90°）と、ルート Transform を固定オフセット分ローカル Z 方向へ下げる移動を、
        /// 同時にトゥイーンで行う。
        /// </summary>
        /// <param name="enable">true で寝かせて下げる、false で元の向き・位置へ戻す</param>
        public void SetTopDownSpritePose(bool enable)
        {
            if (_renderer == null || _isTopDownSpriteActive == enable) return;
            _isTopDownSpriteActive = enable;

            CaptureRendererBaseLocalRotation();
            var spriteTransform = _renderer.transform;
            _topDownPoseTween?.Kill();

            Quaternion targetRotation;
            float targetRootLocalY;
            if (enable)
            {
                targetRotation = TopDownSpriteLocalRotation;
                _rootPreTopDownLocalY = transform.localPosition.y;
                targetRootLocalY = _rootPreTopDownLocalY - TopDownDownwardOffset;
            }
            else
            {
                targetRotation = _rendererBaseLocalRotation;
                targetRootLocalY = _rootPreTopDownLocalY;
            }

            _topDownPoseTween = DOTween.Sequence()
                .Join(spriteTransform.DOLocalRotateQuaternion(targetRotation, TopDownPoseTweenTime).SetEase(Ease.OutQuad))
                .Join(transform.DOLocalMoveY(targetRootLocalY, TopDownPoseTweenTime).SetEase(Ease.OutQuad));
        }

        /// <summary>
        /// 左に移動するのでマイナス
        /// </summary>
        /// <param name="y">縦方向の移動量</param>
        /// <param name="x">横方向の移動量</param>
        public async UniTask Move(int y, int x)
        {
            // 横方向はマイナス方向に進めるため、負の値にする
            _moveVec = new Vector3(x, 0, y);

            // 移動をする
            await transform.DOLocalMove(_moveVec, MoveTime).SetEase(Ease.OutQuad);
            await UniTask.Delay(TimeSpan.FromSeconds(MoveTime * 3f));

            // 位置を更新する
            height = y;
            width = x;
        }

        /// <summary>
        /// 近距離(Width==0)攻撃時に、自分の行・高さは保ったままプレイヤーの手前まで踏み込む。
        /// </summary>
        public virtual async UniTask ApproachPlayerForAttack()
        {
            var player = PlayerController.Instance;
            if (player == null || player.PlayerTransform == null) return;

            _preApproachPosition = transform.position;
            _hasApproachedForAttack = true;

            var playerPos = player.PlayerTransform.position;
            var target = new Vector3(playerPos.x + ApproachGap, _preApproachPosition.y, _preApproachPosition.z);

            await transform.DOMove(target, ApproachMoveTime).SetEase(Ease.OutQuad);
        }

        /// <summary>
        /// <see cref="ApproachPlayerForAttack"/> で踏み込む前の位置へ戻る。踏み込んでいなければ何もしない。
        /// </summary>
        public virtual async UniTask ReturnFromApproach()
        {
            if (!_hasApproachedForAttack) return;
            _hasApproachedForAttack = false;

            await transform.DOMove(_preApproachPosition, ApproachMoveTime).SetEase(Ease.OutQuad);
        }

        /// <summary>
        /// 攻撃アニメーションを実行する前のカメラの移動を行う
        /// </summary>
        public async UniTask WaitToCameraZoom()
        {
            await CameraManager.Instance.ActSetCameraTarget(transform.position);
        }

        /// <summary>
        /// 攻撃のアニメーションを開始
        /// </summary>
        public async UniTask WaitAttackAnim()
        {
            _animator.SetTrigger("AttackT");
            _animationFlag = false;

            await UniTask.WaitUntil(() => _animationFlag);
        }

        /// <summary>
        /// 攻撃のアニメーションを開始
        /// </summary>
        public async UniTask WaitSpecificAnim()
        {
            _animator.SetTrigger("SkillT");
            _animationFlag = false;

            await UniTask.WaitUntil(() => _animationFlag);
        }

        public void ActiveAttack()
        {
            _animationFlag = true;
        }

        public async UniTask Attack()
        {

        }

        /// <inheritdoc/>
        public async UniTask FadeGauge(float value)
        {
            _canvasGroup.DOFade(value, 0.5f).SetEase(Ease.OutQuad).ToUniTask().Forget();
        }

        public async UniTask Damage(float rate)
        {
            await ApplyDamage(() => BattleManager.EnemyDamage(rate));
        }

        /// <summary>
        /// めちゃくちゃ早い一閃によるダメージ処理
        /// </summary>
        public async UniTask FlashDamage(float rate)
        {
            await ApplyDamage(() => BattleManager.FlashAttackDamage(rate));
        }

        private async UniTask ApplyDamage(Func<UniTask<(int damage, bool isDeath)>> calculateDamage)
        {
            Time.timeScale = 0.001f;
            CameraManager.Instance.ActSetCameraTarget(transform.position).Forget();

            var targetUnit = MapManager.Instance.GetUnitAt(height, width);
            if (targetUnit == null)
            {
                Time.timeScale = 1.0f;
                return;
            }

            var targetStatus = targetUnit.GetStatus();
            BattleManager.RegisterEnemy(targetStatus);
            var damageValue = await calculateDamage();

            UIPresenter.Instance.AppearDamageText($"{damageValue.damage}", transform.position).Forget();
            DOTween.To(() => _healthBar.fillAmount, x => _healthBar.fillAmount = x, (float)targetStatus.HP / targetStatus.MaxHP, 0.5f).SetEase(Ease.OutQuad).ToUniTask().Forget();

            if (damageValue.isDeath)
            {
                await Death(targetStatus);
            }
            else
            {
                await Damage(targetStatus);
            }

            Time.timeScale = 1.0f;

            // CameraManager.Instance.ActResetCameraTarget().Forget();

            if (damageValue.isDeath)
            {
                MapManager.Instance.RemoveUnitAt(height, width);
                if (targetUnit is IEnemyUnit)
                {
                    // クリア条件成立時はクリア演出・シナリオ再生の完了まで await する。
                    await DomainEventDispatcher.Dispatch(new EnemyDefeatedEvent(new UnitPosition(height, width), targetStatus.Experience));
                }
                //Destroyするが、後でオブジェクトプールにする
                Dispose();
                if (UnitSpawner.Instance != null)
                {
                    UnitSpawner.Instance.ReturnView(this);
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
        }

        public async UniTask Death(BattleStatus targetStatus)
        {
            _animator.SetTrigger("DeadT");
            UIPresenter.Instance.AppearEnergy(transform.position, targetStatus.Energy).Forget();
            AudioManager.Instance.PlaySe(SeAudioType.Attack);
            VFXEmitter.Instance.Emit(VFXKinds.Attack, transform.position);
            await UniTask.Delay(TimeSpan.FromSeconds(0.7f), ignoreTimeScale:true);
        }

        public async UniTask Damage(BattleStatus targetStatus)
        {
            _animator.SetTrigger("DamageT");
            UIPresenter.Instance.AppearEnergy(transform.position, targetStatus.Energy / 2).Forget();
            AudioManager.Instance.PlaySe(SeAudioType.Kill);
            VFXEmitter.Instance.Emit(VFXKinds.Defeat, transform.position);
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f), ignoreTimeScale:true);
        }
        
        /// <inheritdoc/>
        /// <remarks>
        /// BaseUnit 側で発火・計算済みのダメージ結果を反映するための経路。
        /// View の OnTriggerEnter から発火する ApplyDamage とは別経路のため、
        /// ここではダメージ計算やマップからの除去は行わず、見た目の更新だけを担当する。
        /// </remarks>
        public async UniTask ReflectDamage(int damage, bool isDeath, BattleStatus status, bool showEnergy = true)
        {
            if (status == null) return;

            Time.timeScale = DamageHitStopTimeScale;
            try
            {
                if (CameraManager.Instance != null)
                {
                    CameraManager.Instance.ActSetCameraTarget(transform.position).Forget();
                }

                if (UIPresenter.Instance != null)
                {
                    UIPresenter.Instance.AppearDamageText($"{damage}", transform.position).Forget();
                }

                ReflectHealthBar(status);

                if (showEnergy)
                {
                    if (isDeath)
                    {
                        await Death(status);
                    }
                    else
                    {
                        await Damage(status);
                    }
                }
                else
                {
                    await PlayDamageEffect(isDeath);
                }
            }
            finally
            {
                Time.timeScale = 1.0f;
            }

            if (isDeath)
            {
                ReleaseView();
            }
        }

        /// <inheritdoc/>
        public async UniTask ReflectHeal(int amount, BattleStatus status)
        {
            if (status == null || amount <= 0) return;

            if (UIPresenter.Instance != null)
            {
                UIPresenter.Instance.AppearDamageText($"+{amount}", transform.position).Forget();
            }

            ReflectHealthBar(status);

            await UniTask.Delay(TimeSpan.FromSeconds(HealEffectTime), ignoreTimeScale: true);
        }

        /// <summary>
        /// エナジー獲得演出を伴わない被弾／死亡演出を再生する。
        /// 犠牲のように、撃破報酬が発生しない死に方で使う。
        /// </summary>
        /// <param name="isDeath">死亡演出を再生するか</param>
        private async UniTask PlayDamageEffect(bool isDeath)
        {
            _animator.SetTrigger(isDeath ? "DeadT" : "DamageT");
            AudioManager.Instance.PlaySe(isDeath ? SeAudioType.Attack : SeAudioType.Kill);
            VFXEmitter.Instance.Emit(isDeath ? VFXKinds.Attack : VFXKinds.Defeat, transform.position);
            await UniTask.Delay(TimeSpan.FromSeconds(isDeath ? DeathEffectTime : DamageEffectTime), ignoreTimeScale: true);
        }

        /// <summary>
        /// 現在HPの割合に合わせてHPゲージを追従させる
        /// </summary>
        /// <param name="status">反映対象ユニットのステータス</param>
        private void ReflectHealthBar(BattleStatus status)
        {
            if (_healthBar == null || status.MaxHP <= 0) return;

            DOTween.To(() => _healthBar.fillAmount, x => _healthBar.fillAmount = x, (float)status.HP / status.MaxHP, HealthBarTweenTime)
                .SetEase(Ease.OutQuad)
                .ToUniTask()
                .Forget();
        }

        /// <summary>
        /// View をオブジェクトプールへ返却する（プールが無ければ非表示にする）
        /// </summary>
        private void ReleaseView()
        {
            Dispose();
            if (UnitSpawner.Instance != null)
            {
                UnitSpawner.Instance.ReturnView(this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// アニメーションイベント以外にも発火できるように。
        /// 犠牲処理も対象。
        /// </summary>
        public void Dead()
        {
            _isDeath = true;
            _renderer.DOFade(0f, DeadFadeTime);
        }

        public void Dispose()
        {
        }
    }
}
