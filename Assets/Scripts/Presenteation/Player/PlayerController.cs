using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

using EchoEdge.Domain.Battle;

namespace EchoEdge.Presenter.Player
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(BattleStatus))]
    public class PlayerController: MonoBehaviour
    {
        public static PlayerController Instance;

        private BattleStatus _status;

        /// <summary>
        /// プレイヤーステータスへのアクセス。
        /// </summary>
        public BattleStatus Status => _status;


        [SerializeField]
        private Transform _playerTransform;
        public Transform PlayerTransform => _playerTransform;

        [SerializeField]
        private Rigidbody _rb;

        [SerializeField]
        private LayerMask _layerMask;

        /// <summary>
        /// 攻撃固有の実装(<see cref="IPlayerAttackAction"/>)から参照する、壁判定用のレイヤーマスク。
        /// </summary>
        internal LayerMask LayerMask => _layerMask;

        /// <summary>
        /// Update内でのデバッグ描画用のray。実行中の攻撃(<see cref="IPlayerAttackAction"/>)が
        /// <see cref="SetDebugRay"/>で更新する。
        /// </summary>
        private Ray _ray;

        /// <summary>
        /// Update内でのデバッグ描画用の方向。実行中の攻撃が<see cref="SetDebugDirection"/>で更新する。
        /// </summary>
        private Vector3 _direction;

        /// <summary>
        /// 残像オブジェクトプール
        /// </summary>
        [SerializeField]
        private AfterimagePool _afterimagePool;

        /// <summary>
        /// 残像を出現させる移動距離の間隔
        /// </summary>
        [SerializeField, Tooltip("残像を出現させる移動距離の間隔")]
        private float _afterimageInterval = 1f;

        [SerializeField, Tooltip("ヤケクソの線マテリアル")]
        private Material _lineMaterial;

        public Material LineMaterial => _lineMaterial;

        /// <summary>
        /// 最後に残像を出現させた位置
        /// </summary>
        private Vector3 _lastAfterimagePosition;

        /// <summary>
        /// 現在実行中の攻撃が、めちゃくちゃ早い一閃(Flash)かどうか。
        /// </summary>
        private bool _isFlashAttack;

        /// <summary>
        /// 現在の攻撃が、めちゃくちゃ早い一閃(Flash)かどうか。攻撃固有の実装から参照する。
        /// </summary>
        public bool IsFlashAttack => _isFlashAttack;

        private PlayerAttackKinds _attackKind = PlayerAttackKinds.Reflect;

        public PlayerAttackKinds AttackKind => _attackKind;

        /// <summary>
        /// 現在実行中(または直近に実行した)攻撃。OnTriggerEnter発生時のダメージ判定の委譲先として使う。
        /// </summary>
        private IPlayerAttackAction _currentAttackAction;

        public void Start()
        {
            Instance = this;
            _ray = new Ray();
        }

        public void SetAttackKind(PlayerAttackKinds kind)
        {
            _attackKind = kind;
        }

        public void SetFlashAttack(bool isFlash)
        {
            _isFlashAttack = isFlash;
        }

        /// <summary>
        /// 攻撃の実行エントリーポイント。
        /// PlayerControllerが保持する攻撃パラメータ(<see cref="AttackKind"/>と<see cref="_isFlashAttack"/>)の
        /// 組み合わせによって、適切な<see cref="IPlayerAttackAction"/>実装に処理を委譲する。
        /// 攻撃の種類ごとの実装は、攻撃種類ごとに用意された<see cref="IPlayerAttackAction"/>実装クラスを参照。
        /// </summary>
        /// <param name="targetPos">ポインターの先の位置</param>
        public async UniTask ExecuteAttack(Vector3 targetPos)
        {
            _currentAttackAction = ResolveAttackAction();
            await _currentAttackAction.ExecuteAsync(targetPos);
        }

        private IPlayerAttackAction ResolveAttackAction()
        {
            return (_attackKind, _isFlashAttack) switch
            {
                (PlayerAttackKinds.Reflect, false) => ReflectAttackAction.Instance,
                (PlayerAttackKinds.Reflect, true) => ReflectFlashAttackAction.Instance,
                (PlayerAttackKinds.Pierce, false) => PierceAttackAction.Instance,
                (PlayerAttackKinds.Pierce, true) => PierceFlashAttackAction.Instance,
                (PlayerAttackKinds.Bomb, false) => BombAttackAction.Instance,
                (PlayerAttackKinds.Bomb, true) => BombFlashAttackAction.Instance,
                // 曲線攻撃は通常・一閃とも同じクラス。一閃版は CurveAttackAction 内で移動速度だけ上げる。
                (PlayerAttackKinds.Curve, false) => CurveAttackAction.Instance,
                (PlayerAttackKinds.Curve, true) => CurveAttackAction.Instance,
                _ => throw new InvalidOperationException(
                    $"未対応の攻撃の組み合わせです。AttackKind: {_attackKind}, IsFlashAttack: {_isFlashAttack}")
            };
        }

    #if UNITY_EDITOR
        void Update()
        {
            Debug.DrawLine(_ray.origin, _ray.origin + _direction * 100, Color.yellow);
        }
    #endif

        /// <summary>
        /// デバッグ描画用のrayを更新する。<see cref="IPlayerAttackAction"/>実装から呼ばれる。
        /// </summary>
        internal void SetDebugRay(Vector3 origin, Vector3 direction)
        {
            #if unity_editor
            _ray.origin = origin;
            _ray.direction = direction;
            _direction = direction;
            #endif
        }

        /// <summary>
        /// デバッグ描画用の方向のみを更新する。<see cref="IPlayerAttackAction"/>実装から呼ばれる。
        /// </summary>
        internal void SetDebugDirection(Vector3 direction)
        {
            _direction = direction;
        }

        /// <summary>
        /// 残像の出現判定の基準位置を、現在位置にリセットする。トゥイーン移動を開始する直前に呼ぶ。
        /// </summary>
        internal void ResetAfterimageAnchor()
        {
            _lastAfterimagePosition = transform.position;
        }

        /// <summary>
        /// 一定距離移動した場合に残像を出現させる。DOTweenのOnUpdateコールバックから呼ばれる。
        /// </summary>
        internal void SpawnAfterimageIfNeeded()
        {
            if (_afterimagePool == null) return;
            if (Vector3.Distance(transform.position, _lastAfterimagePosition) >= _afterimageInterval)
            {
                _lastAfterimagePosition = transform.position;
                SpawnAfterimage(transform.position, transform.rotation).Forget();
            }
        }

        /// <summary>
        /// 残像をオブジェクトプールから取り出し、指定した位置・回転で出現させる。
        /// </summary>
        private async UniTaskVoid SpawnAfterimage(Vector3 position, Quaternion rotation)
        {
            var pooledObject = await _afterimagePool.GetPooledObject();
            if (pooledObject is AfterimageObject afterimage)
            {
                afterimage.AfterimageAppear(
                    PlayerView.Instance.CurrentSprite,
                    position,
                    rotation
                ).Forget();
            }
        }

        /// <summary>
        /// ダメージ処理。実行中の攻撃固有の判定に委譲する。
        /// </summary>
        /// <param name="other">相手の当たり判定</param>
        private void OnTriggerEnter(Collider other)
        {
            _currentAttackAction?.OnTriggerEnter(other);
        }

        /// <summary>
        /// 壁の処理
        /// </summary>
        /// <param name="collision"></param>
        void OnCollisionEnter(Collision collision)
        {
            _currentAttackAction?.OnCollisionEnter(collision);
        }
    }
}
