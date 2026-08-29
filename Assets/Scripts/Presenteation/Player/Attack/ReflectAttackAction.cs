using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// 反射攻撃(通常)の実装。
/// ポインターとプレイヤーの角度を計算し、壁に反射させながらプレイヤーを進ませる。
/// 移動中にOnTriggerEnterで敵に触れた場合はダメージを与える。
/// </summary>
public class ReflectAttackAction: IPlayerAttackAction
{
    private static ReflectAttackAction _instance;

    public static ReflectAttackAction Instance => _instance ??= new ReflectAttackAction();

    private const float Speed = 23;
    private const float ReflectionDamageCheckRadius = 0.5f;

    /// <summary>
    /// この攻撃のダメージ倍率。反射攻撃をメインの攻撃として基準値(1.0)にする。
    /// </summary>
    private const float DamageRate = 1.0f;

    /// <summary>
    /// 現在のプレイヤーの位置
    /// </summary>
    private Vector3 _pos;

    /// <summary>
    /// うごかす方向のフィールド
    /// </summary>
    private Vector3 _direction;

    /// <summary>
    /// ポインターの位置とプレイヤーの位置の相違を計算する。ポインターとプレイヤーの位置のレイに敵が当たっていたら、その敵にダメージを与える。
    /// プレイヤーをポインターの位置にテレポートさせる。
    /// 壁についたらreflectCountを減らす。減らした後、1以上ならば反射角に対しておなじことを行う
    /// </summary>
    /// <param name="targetPos">ポインターの先の位置</param>
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

        for (int i = 0; i <= reflectCount; i++)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f));

            Ray ray = new Ray(_pos, _direction);
            player.SetDebugRay(_pos, _direction);

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

                _direction = Vector3.Reflect(_direction, hit.normal);
                if (i != reflectCount) _pos = player.PlayerTransform.position;
            }
            else
            {
                throw new System.Exception("当たってない...だと");
            }
        }

        await UniTask.Delay(TimeSpan.FromSeconds(0.5f));

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
            status.Damage(DamageRate).Forget();
        }
    }

    public void OnCollisionEnter(Collision collision)
    {
        
    }
}
