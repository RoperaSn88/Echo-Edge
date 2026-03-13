using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Unity.Mathematics;
using UnityEditor.Callbacks;
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

    [SerializeField, Tooltip("反射回数")]
    private int _reflectCount = 1;
    bool atatta = false;

    private const float Speed = 30;

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
        _pos = _playerTransform.position;

        _direction.Set(targetPos.x - _pos.x, 0, targetPos.z - _pos.z);
        _direction = _direction.normalized;

        for(int i = 0; i < _reflectCount; i++)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f));

            _ray.origin = _pos;
            _ray.direction = _direction;

            // 始点からdirection方向にrayを飛ばし、当たった位置を新たな_posとする。
            if(Physics.Raycast(_ray, out _hit, math.INFINITY, _layerMask))
            {
                var distance = Vector3.Distance(_ray.origin, _hit.point);
                // プレイヤーを移動する
                _vec.Set(_hit.point.x, _hit.point.y, _hit.point.z);
                await transform.DOMove(_vec, distance / Speed);

                _direction = Vector3.Reflect(_direction, _hit.normal);
                _pos = _playerTransform.position;
            }
            else
            {
                throw new System.Exception("当たってない...だと");
            }
        }
        
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f));

        _vec.Set(-8, _playerTransform.position.y, _playerTransform.position.z);
        _playerTransform.position = _vec;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            atatta = true;
            Debug.Log("aaaaaaa");
        }
    }

    /// <summary>
    /// ダメージ処理
    /// </summary>
    /// <param name="other">相手の当たり判定</param>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            Debug.Log("damage");
            other.TryGetComponent<IDamageActivator>(out var status);
            status.Damage();
        }
    }
}