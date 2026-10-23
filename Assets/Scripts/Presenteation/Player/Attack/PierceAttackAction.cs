using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

using EchoEdge.App.Battle;
using EchoEdge.Domain.Battle;
using EchoEdge.Presenter.Battle;
using EchoEdge.Presenter.UI;

namespace EchoEdge.Presenter.Player
{
    /// <summary>
    /// 貫通攻撃(通常)の実装。
    /// TODO: 貫通攻撃の仕様が決まり次第実装する。
    /// </summary>
    public class PierceAttackAction: IPlayerAttackAction
    {
        private static PierceAttackAction _instance;

        public static PierceAttackAction Instance => _instance ??= new PierceAttackAction();

        private const float Speed = 39;
        private const float ReflectionDamageCheckRadius = 0.5f;
        private const float AwaitTime = 0.5f;

        /// <summary>
        /// この攻撃のダメージ倍率。反射攻撃(基準値1.0)より弱くするため0.7倍とする。
        /// </summary>
        private const float DamageRate = 0.7f;

        /// <summary>
        /// 現在のプレイヤーの位置
        /// </summary>
        private Vector3 _pos;

        /// <summary>
        /// うごかす方向のフィールド
        /// </summary>
        private Vector3 _direction;

        /// <summary>
        /// 現在のセグメント(壁から壁までの移動)でダメージを与えた敵。
        /// OnTriggerEnter と移動経路の判定(<see cref="DamageEnemiesOnPath"/>)で同じ敵に二重にダメージを与えないようにする。
        /// </summary>
        private readonly HashSet<IDamageActivator> _damagedEnemies = new();

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
                _damagedEnemies.Clear();

                // 始点からdirection方向にrayを飛ばし、当たった位置を新たな_posとする。
                if (Physics.Raycast(ray, out RaycastHit hit, math.INFINITY, player.LayerMask))
                {
                    var distance = Vector3.Distance(ray.origin, hit.point);

                    // プレイヤーを移動する
                    Vector3 endVec = new Vector3(hit.point.x, hit.point.y, hit.point.z);
                    endVec = Vector3.Lerp(player.transform.position, endVec, 0.99f);
                    Vector3 startVec = player.transform.position;
                    await player.MoveByVelocityAsync(endVec, distance / Speed);
                    DamageEnemiesOnPath(startVec, endVec);

                    // 1つのブロックオブジェクトならば、横方向に1つ動いた先の位置に移動する。
                    if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Block") && i != reflectCount)
                    {
                        // Todo: 壁が2つ以上ならば、その先の距離まで貫通するか考える
                        // 法線は軸に平行な単位ベクトルなので、逆方向へ1マス分進めば貫通先の位置になる。
                        // (旧実装はcos(法線の偏角)で除算しており、法線がZ軸方向のときゼロ除算でNaN/Infinityになりプレイヤーが場外に飛ぶ不具合があった)
                        // エナジーウォールの斜面は法線が斜めなので、進行方向へ板の厚さ分だけ進める
                        if (!SlopeWallView.TryGetPassThroughPosition(hit, _direction, out _pos))
                        {
                            Vector3 moveVec = -hit.normal;
                            _pos = hit.point + moveVec;
                        }

                        player.transform.position = _pos;
                    }
                    // 壁なら普通に反射
                    else
                    {
                        await UniTask.Delay(TimeSpan.FromSeconds(AwaitTime));
                        _direction = Vector3.Reflect(_direction, hit.normal);
                        if (i != reflectCount) _pos = player.PlayerTransform.position;
                        
                        if (i > 0)
                        {
                            var colliders = Physics.OverlapSphere(ray.origin, ReflectionDamageCheckRadius);
                            foreach (var collider in colliders)
                            {
                                OnTriggerEnter(collider);
                            }
                        }
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
        /// 移動した経路上の敵にダメージを与える。
        /// 貫通時の瞬間移動の直後などは OnTriggerEnter が発生せず敵を素通りしてしまうことがあるため、
        /// 移動後に経路を SphereCast で調べ、まだダメージを与えていない敵にダメージを与える。
        /// </summary>
        /// <param name="start">移動の始点</param>
        /// <param name="end">移動の終点</param>
        private void DamageEnemiesOnPath(Vector3 start, Vector3 end)
        {
            Vector3 path = end - start;
            path.y = 0;
            float distance = path.magnitude;
            if (distance <= 0f)
            {
                return;
            }

            var hits = Physics.SphereCastAll(start, ReflectionDamageCheckRadius, path / distance, distance, ~0, QueryTriggerInteraction.Collide);
            foreach (var hit in hits)
            {
                OnTriggerEnter(hit.collider);
            }
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

            if (!other.TryGetComponent<IDamageActivator>(out var status) || !_damagedEnemies.Add(status))
            {
                return;
            }

            PlayerView.Instance.Animator.SetTrigger("AttackT");
            status.Damage(DamageRate).Forget();
        }

        public void OnCollisionEnter(Collision collision)
        {

        }
    }
}
