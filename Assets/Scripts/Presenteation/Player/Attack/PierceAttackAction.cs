using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// 貫通攻撃(通常)の実装。
/// TODO: 貫通攻撃の仕様が決まり次第実装する。
/// </summary>
public class PierceAttackAction: IPlayerAttackAction
{
    private static PierceAttackAction _instance;

    public static PierceAttackAction Instance => _instance ??= new PierceAttackAction();
    
    private const float Speed = 23;
    private const float ReflectionDamageCheckRadius = 0.5f;
    private const float AwaitTime = 0.5f;

    /// <summary>
    /// 現在のプレイヤーの位置
    /// </summary>
    private Vector3 _pos;

    /// <summary>
    /// うごかす方向のフィールド
    /// </summary>
    private Vector3 _direction;

    public async UniTask ExecuteAsync(Vector3 targetPos)
    {
        var player = PlayerController.Instance;

        UIPresenter.Instance.ResetFade();

        _pos = player.PlayerTransform.position;

        _direction.Set(targetPos.x - _pos.x, 0, targetPos.z - _pos.z);
        _direction = _direction.normalized;

        PlayerView.Instance.Animator.SetBool("AttackingF", true);

        byte reflectCount = BattleManager.PlayerStatus.Move;

        BattleManager.ResetReflectionCount();
        await UniTask.Delay(TimeSpan.FromSeconds(AwaitTime));

        for (byte i = 0; i <= reflectCount; i++)
        {
            Ray ray = new Ray(_pos, _direction);

            // このセグメントに到達するまでに発生した反射回数を記録する。
            // OverlapSphere・移動中の OnTriggerEnter によるダメージ計算で参照される。
            BattleManager.SetReflectionCount(i);

            if (i > 0)
            {
                var colliders = Physics.OverlapSphere(ray.origin, ReflectionDamageCheckRadius);
                foreach (var collider in colliders)
                {
                    OnTriggerEnter(collider);
                }
            }

            // 始点からdirection方向にrayを飛ばし、当たった位置を新たな_posとする。
            if (Physics.Raycast(ray, out RaycastHit hit, math.INFINITY, player.LayerMask))
            {
                var distance = Vector3.Distance(ray.origin, hit.point);
                
                // プレイヤーを移動する
                Vector3 endVec = new Vector3(hit.point.x, hit.point.y, hit.point.z);
                endVec = Vector3.Lerp(player.transform.position, endVec, 0.99f);
                player.ResetAfterimageAnchor();
                await player.transform.DOMove(endVec, distance / Speed)
                    .OnUpdate(player.SpawnAfterimageIfNeeded);
                
                // 1つのブロックオブジェクトならば、横方向に1つ動いた先の位置に移動する。
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Block") && i != reflectCount)
                {
                    // Todo: 壁が2つ以上ならば、その先の距離まで貫通するか考える
                    float rad = Mathf.Atan2(hit.normal.z, hit.normal.x);
                    var rate = 1 / Mathf.Cos(rad);
                    
                    var moveVec = new Vector3(hit.normal.x * rate, 0, hit.normal.z * rate);
                    
                    _pos = hit.point + moveVec;
                    player.transform.position = _pos;
                }
                // 壁なら普通に反射
                else
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(AwaitTime));
                    _direction = Vector3.Reflect(_direction, hit.normal);
                    if (i != reflectCount) _pos = player.PlayerTransform.position;
                }
            }
            else
            {
                throw new System.Exception("当たってない...だと");
            }
        }

        await UniTask.Delay(TimeSpan.FromSeconds(AwaitTime));

        Vector3 returnPos = new Vector3(-8, player.PlayerTransform.position.y, player.PlayerTransform.position.z);

        BattleManager.ResetQTE();
        BattleManager.ResetCombo();
        BattleManager.ResetReflectionCount();
        UIPresenter.Instance.FadeTexts();

        PlayerView.Instance.Animator.SetBool("AttackingF", false);

        player.PlayerTransform.position = returnPos;
        await UniTask.Delay(TimeSpan.FromSeconds(0.6f));
    }

    /// <summary>
    /// ダメージ処理
    /// </summary>
    /// <param name="other">相手の当たり判定</param>
    public void OnTriggerEnter(Collider other)
    {
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

    public void OnCollisionEnter(Collision collision)
    {
        
    }
}
