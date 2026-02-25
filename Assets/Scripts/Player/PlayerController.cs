using Cysharp.Threading.Tasks;
using UnityEditor.Callbacks;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController: MonoBehaviour
{
    public static PlayerController Instance;

    [SerializeField]
    private Transform _playerTransform;
    public Transform PlayerTransform => _playerTransform;

    [SerializeField]
    private Rigidbody _rb;

    /// <summary>
    /// 現在のプレイヤーの位置
    /// </summary>
    private Vector3 _pos;

    /// <summary>
    /// うごかす方向のフィールド
    /// </summary>
    private Vector3 _direction;

    /// <summary>
    /// 反射回数
    /// </summary>
    int _reflectCount = 1;
    bool atatta = false;

    public void Start()
    {
        Instance = this;
    }

    /// <summary>
    /// ポインターとプレイヤーの角度を計算し、プレイヤーを進ませる。
    /// ポインターの位置とプレイヤーの位置の相違を計算する。ポインターとプレイヤーの位置のレイに敵が当たっていたら、その敵にダメージを与える。
    /// プレイヤーをポインターの位置にテレポートさせる。
    /// 壁についたらreflectCountを減らす。減らした後、1以上ならば反射角に対しておなじことを行う。
    /// </summary>
    /// <param name="targetPos">ポインターの先の位置</param>
    public async UniTask Move(Vector3 targetPos)
    {
        _pos = _playerTransform.position;
        _direction = new Vector3(targetPos.x - _pos.x, 0, targetPos.z - _pos.z).normalized;
        _rb.AddForce(_direction * 20, ForceMode.Impulse);
        await UniTask.WaitUntil(() => atatta == true || _rb.linearVelocity.magnitude == 0);
        _rb.linearVelocity = Vector3.zero;
        _playerTransform.position = new Vector3(-8, _pos.y, _playerTransform.position.z);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            atatta = true;
            Debug.Log("aaaaaaa");
        }
    }
}