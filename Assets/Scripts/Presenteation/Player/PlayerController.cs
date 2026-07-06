using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;

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
    /// 現在のプレイヤーの位置
    /// </summary>
    private Vector3 _pos;

    /// <summary>
    /// うごかす方向のフィールド
    /// </summary>
    private Vector3 _direction;

    /// <summary>
    /// rayをキャッシュする
    /// </summary>
    private Ray _ray;

    /// <summary>
    /// RaycastHitをキャッシュする
    /// </summary>
    private RaycastHit _hit;

    /// <summary>
    /// ベクトルをキャッシュする
    /// </summary>
    private Vector3 _vec;
    
    bool atatta = false;

    private const float Speed = 23;
    private const float ReflectionDamageCheckRadius = 0.5f;

    /// <summary>
    /// めちゃくちゃ早い一閃で、敵を斬り抜ける際のトゥイーン時間
    /// </summary>
    private const float FlashAttackSlashDuration = 0.1f;

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
    /// ダメージを与えたことのある敵のリスト。反射中に同じ敵に複数回ダメージを与えないようにするためのもの。
    /// </summary>
    /// <returns></returns>
    private List<IDamageActivator> _damagedEnemies = new();

    private PlayerAttackKind _attackKind;

    public void Start()
    {
        Instance = this;
        _ray = new Ray();
    }

    /// <summary>
    /// ポインターとプレイヤーの角度を計算し、プレイヤーを進ませる。
    /// ポインターの位置とプレイヤーの位置の相違を計算する。ポインターとプレイヤーの位置のレイに敵が当たっていたら、その敵にダメージを与える。
    /// プレイヤーをポインターの位置にテレポートさせる。
    /// 壁についたらreflectCountを減らす。減らした後、1以上ならば反射角に対しておなじことを行う
    /// </summary>
    /// <param name="targetPos">ポインターの先の位置</param>
    public async UniTask Move(Vector3 targetPos)
    {
        // 通常の反射攻撃
        _attackKind = PlayerAttackKind.ReflectAttack;

        UIPresenter.Instance.ResetFade();
        
        _pos = _playerTransform.position;

        _direction.Set(targetPos.x - _pos.x, 0, targetPos.z - _pos.z);
        _direction = _direction.normalized;
        
        PlayerView.Instance.Animator.SetBool("AttackingF", true);
        
        
        byte reflectCount = BattleManager.PlayerStatus.Move;

        BattleManager.ResetReflectionCount();

        for(int i = 0; i <= reflectCount; i++)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f));

            _ray.origin = _pos;
            _ray.direction = _direction;

            // このセグメントに到達するまでに発生した反射回数を記録する。
            // OverlapSphere・移動中の OnTriggerEnter によるダメージ計算で参照される。
            BattleManager.SetReflectionCount(i);

            if (i > 0)
            {
                var colliders = Physics.OverlapSphere(_ray.origin, ReflectionDamageCheckRadius);
                foreach (var collider in colliders)
                {
                    TryDamageEnemy(collider).Forget();
                }
            }

            // 始点からdirection方向にrayを飛ばし、当たった位置を新たな_posとする。
            if(Physics.Raycast(_ray, out _hit, math.INFINITY, _layerMask))
            {
                var distance = Vector3.Distance(_ray.origin, _hit.point);
                // プレイヤーを移動する
                _vec.Set(_hit.point.x, _hit.point.y, _hit.point.z);
                _vec = Vector3.Lerp(transform.position, _vec, 0.99f);
                _lastAfterimagePosition = transform.position;
                await transform.DOMove(_vec, distance / Speed)
                    .OnUpdate(SpawnAfterimageIfNeeded);

                _direction = Vector3.Reflect(_direction, _hit.normal);
                if(i != reflectCount) _pos = _playerTransform.position;
            }
            else
            {
                throw new System.Exception("当たってない...だと");
            }
        }
        
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f));

        _vec.Set(-8, _playerTransform.position.y, _playerTransform.position.z);

        BattleManager.ResetQTE();
        BattleManager.ResetCombo();
        BattleManager.ResetReflectionCount();
        UIPresenter.Instance.FadeTexts();
        
        PlayerView.Instance.Animator.SetBool("AttackingF", false);

        _playerTransform.position = _vec;
        await UniTask.Delay(TimeSpan.FromSeconds(0.6f));
    }

    /// <summary>
    /// めちゃくちゃ早い一閃。
    /// プレイヤーからポインター方向へ光線を飛ばし、当たった壁の位置へ瞬時に移動する。
    /// 光線上に敵がいる場合はプレイヤーに近い順に斬りつけたのち、壁の位置まで瞬時に移動する。
    /// 反射回数の分だけ壁の法線で反射しながら繰り返し、最後に元の位置へ戻る。
    /// </summary>
    /// <param name="targetPos">ポインターの先の位置</param>
    public async UniTask FlashMove(Vector3 targetPos)
    {
        _attackKind = PlayerAttackKind.FlashReflectAttack;
        byte reflectCount = BattleManager.PlayerStatus.Move;
        PlayerView.Instance.Animator.SetBool("AttackingF", true);
        
        // CameraManager.Instance.ActSetCameraTarget(transform.position).Forget();

        Vector3 originalPosition = _playerTransform.position;
        _pos = originalPosition;
        _direction = new Vector3(targetPos.x - _pos.x, 0, targetPos.z - _pos.z).normalized;

        await UniTask.Delay(TimeSpan.FromSeconds(0.5f));

        for (int i = 0; i <= reflectCount; i++)
        {
            // 反射するたびに敵のリストをクリアする。
            _damagedEnemies.Clear();

            Ray ray = new Ray(_pos, _direction);
            if (!Physics.Raycast(ray, out RaycastHit wallHit, math.INFINITY, _layerMask))
            {
                throw new System.Exception("当たってない...だと");
            }

            float wallDistance = Vector3.Distance(_pos, wallHit.point);

            // 光線に触れた敵を、プレイヤーから近い順に並べる
            var enemyHits = Physics.RaycastAll(ray, wallDistance, ~0, QueryTriggerInteraction.Collide)
                .Where(hit => hit.collider.CompareTag("Enemy"))
                .OrderBy(hit => hit.distance)
                .ToArray();

            foreach (var enemyHit in enemyHits)
            {
                Vector3 enemyPos = enemyHit.collider.transform.position;
                targetPos = new Vector3(enemyPos.x, _playerTransform.position.y, enemyPos.z);

                // 敵の位置から光線の単位ベクトル分マイナスした位置へ瞬時に移動し、プラスした位置へ斬り抜ける
                // 疑似的にダメージ与える
                _playerTransform.position = targetPos - _direction * 0.5f;

                // 移動はするが、攻撃が終了したタイミングで次の敵の位置に移動するように
                CancellationTokenSource cts = new CancellationTokenSource();
                var tween = _playerTransform.DOMove(targetPos, FlashAttackSlashDuration).ToUniTask(cancellationToken: cts.Token);
                PlayerView.Instance.Animator.SetTrigger("AttackT");
                await TryFlashDamageEnemy(enemyHit.collider);
                cts.Cancel();
            }

            // 壁の位置まで瞬時に移動する
            _playerTransform.position = wallHit.point - _direction * 0.5f;
            await _playerTransform.DOMove(wallHit.point, FlashAttackSlashDuration);

            _pos = wallHit.point;
            _direction = Vector3.Reflect(_direction, wallHit.normal);
            
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
        }
        
        BattleManager.ResetQTE();
        BattleManager.ResetCombo();
        BattleManager.ResetReflectionCount();
        UIPresenter.Instance.FadeTexts();

        PlayerView.Instance.Animator.SetBool("AttackingF", false);

        // z軸を含めて元の位置へ戻す
        _playerTransform.position = originalPosition;
    }

    void Update()
    {
        Debug.DrawLine(_ray.origin, _ray.origin + _direction * 100, Color.yellow);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            atatta = true;
        }
    }

    /// <summary>
    /// 一定距離移動した場合に残像を出現させる。DOTweenのOnUpdateコールバックから呼ばれる。
    /// </summary>
    private void SpawnAfterimageIfNeeded()
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
    /// ダメージ処理
    /// </summary>
    /// <param name="other">相手の当たり判定</param>
    private void OnTriggerEnter(Collider other)
    {
        TryDamageEnemy(other).Forget();
    }

    private async UniTask TryFlashDamageEnemy(Collider other)
    {
        if (!other.CompareTag("Enemy"))
        {
            return;
        }

        if (_damagedEnemies.Contains(other.GetComponent<IDamageActivator>()))
        {
            return;
        }

        PlayerView.Instance.Animator.SetTrigger("AttackT");
        if (other.TryGetComponent<IDamageActivator>(out var status))
        {
            _damagedEnemies.Add(status);
            await status.FlashDamage();
        }
    }   

    private async UniTask TryDamageEnemy(Collider other)
    {
        // 高速な一閃の場合、TryFlashDamageEnemyで処理するため、ここでは処理しない
        if(_attackKind == PlayerAttackKind.FlashReflectAttack)
        {
            return;
        }

        if (!other.CompareTag("Enemy"))
        {
            return;
        }

        PlayerView.Instance.Animator.SetTrigger("AttackT");
        if (other.TryGetComponent<IDamageActivator>(out var status))
        {
            status.Damage().Forget();
        }
    }
}
