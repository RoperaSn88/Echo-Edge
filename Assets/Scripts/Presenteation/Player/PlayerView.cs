using Cysharp.Threading.Tasks;
using System.Threading;
using DG.Tweening;
using UnityEngine;

namespace EchoEdge.Presenter.Player
{
    public class PlayerView: MonoBehaviour
    {
        public static PlayerView Instance;

        [SerializeField]
        private Transform _transform;
        public Transform Transform => _transform;

        [SerializeField]
        private Animator _animator;

        public Animator Animator => _animator; 

        [SerializeField]
        private SpriteRenderer _renderer;

        /// <summary>
        /// 現在のプレイヤーのスプライト
        /// </summary>
        public Sprite CurrentSprite => _renderer.sprite;

        /// <summary>
        /// 防御時に再生するパーティクル（ループしない設定にする）
        /// </summary>
        [SerializeField]
        private ParticleSystem _defendParticle;

        /// <summary>
        /// 回復時に再生するパーティクル（ループしない設定にする）
        /// </summary>
        [SerializeField]
        private ParticleSystem _healParticle;

        /// <summary>
        /// パーティクルがループ設定だった場合に打ち切るまでの最大待ち時間（秒）。ループしないものには適用しない
        /// </summary>
        private const float MaxParticleWaitTime = 5f;

        private readonly Vector3 SkillRotationAngle = new Vector3(0, -40f, 0);
        private const float AnimTime = 0.6f;

        /// <summary>
        /// 最後に選んだスキル番号を保持するフィールド
        /// </summary>
        private int _skillNumber;

        void Awake()
        {
            Instance = this;
        }
        public void SkillAnim()
        {
            _animator.SetBool($"Skill{_skillNumber}", true);
            _transform.DORotate(SkillRotationAngle, AnimTime).SetEase(Ease.OutQuad);

        }

        /// <summary>
        /// 防御のパーティクルを再生し、再生し終わるまで待つ。
        /// </summary>
        public UniTask PlayDefendParticle(CancellationToken ct = default)
        {
            return PlayParticleAndWait(_defendParticle, ct);
        }

        /// <summary>
        /// 回復のパーティクルを再生し、再生し終わるまで待つ。
        /// </summary>
        public UniTask PlayHealParticle(CancellationToken ct = default)
        {
            return PlayParticleAndWait(_healParticle, ct);
        }

        /// <summary>
        /// パーティクルを再生し、子も含めて全て消えるまで待つ。
        /// 未設定なら何もしない。ループ設定のものを含む場合だけ最大待ち時間で止める。
        /// PlayerView が破棄されたら待機をキャンセルする。
        /// </summary>
        private async UniTask PlayParticleAndWait(ParticleSystem particle, CancellationToken ct)
        {
            if (particle == null)
            {
                return;
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct, destroyCancellationToken);
            var token = cts.Token;

            bool hasLoop = false;
            foreach (var ps in particle.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (ps.main.loop)
                {
                    hasLoop = true;
                    break;
                }
            }

            particle.Play(true);

            float elapsed = 0f;
            while (particle.IsAlive(true))
            {
                if (hasLoop && elapsed >= MaxParticleWaitTime)
                {
                    particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    break;
                }

                await UniTask.Yield(PlayerLoopTiming.Update, token);
                elapsed += Time.deltaTime;
            }
        }

        public void ResetRotateAnim()
        {
            _animator.SetBool($"Skill{_skillNumber}", false);
            _transform.DORotate(Vector3.zero, AnimTime).SetEase(Ease.OutQuad);
        }
    }
}
