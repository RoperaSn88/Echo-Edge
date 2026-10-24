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
    /// 貫通攻撃(めちゃくちゃ早い一閃)の実装。
    /// 一番初めに当たった壁を破壊しつつ貫通し、通常の貫通攻撃よりも素早く移動する。
    /// 破壊できない壁(外周など)や2枚目以降の壁は、通常の一閃と同じく法線で反射する。
    /// 移動中にOnTriggerEnterで敵に触れた場合はダメージを与える。
    /// </summary>
    public class PierceFlashAttackAction: IPlayerAttackAction
    {
        private static PierceFlashAttackAction _instance;

        public static PierceFlashAttackAction Instance => _instance ??= new PierceFlashAttackAction();

        /// <summary>
        /// 一閃版の移動速度。通常の貫通攻撃(PierceAttackAction.Speed = 39)より速くする。
        /// </summary>
        private const float Speed = 60f;

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

        /// <summary>
        /// ポインター方向へ素早く移動する。一番初めに当たった壁は破壊して貫通し、
        /// それ以外の壁では法線で反射する。反射回数の分だけ繰り返し、最後に元の位置へ戻る。
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

            // 一番初めの壁の処理(破壊 or 反射)を済ませたか。
            bool firstWallResolved = false;

            for (int i = 0; i <= reflectCount; i++)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(AwaitTime));

                Ray ray = new Ray(_pos, _direction);
                player.SetDebugRay(_pos, _direction);

                // このセグメントに到達するまでに発生した反射回数を記録する。
                // OverlapSphere・移動中の OnTriggerEnter によるダメージ計算で参照される。
                BattleManager.SetReflectionCount(i);
                _damagedEnemies.Clear();

                if (i > 0)
                {
                    var colliders = Physics.OverlapSphere(ray.origin, ReflectionDamageCheckRadius);
                    foreach (var collider in colliders)
                    {
                        OnTriggerEnter(collider);
                    }
                }

                // 始点からdirection方向にrayを飛ばし、当たった位置を新たな_posとする。
                if (!Physics.Raycast(ray, out RaycastHit hit, math.INFINITY, player.LayerMask))
                {
                    throw new System.Exception("当たってない...だと");
                }

                var distance = Vector3.Distance(ray.origin, hit.point);

                // プレイヤーを移動する
                Vector3 endVec = new Vector3(hit.point.x, hit.point.y, hit.point.z);
                endVec = Vector3.Lerp(player.transform.position, endVec, 0.99f);
                Vector3 startVec = player.transform.position;
                await player.MoveByVelocityAsync(endVec, distance / Speed);
                DamageEnemiesOnPath(startVec, endVec);

                // 一番初めに当たった壁は、破壊できるなら破壊して貫通する(反射しない)。
                if (!firstWallResolved)
                {
                    firstWallResolved = true;

                    // 壊すとプールへ返されて非アクティブになり、斜面かどうかを判定できなくなるので先に通り抜け先を求めておく
                    bool isSlope = SlopeWallView.TryGetPassThroughPosition(hit, _direction, out var slopePassPos);

                    if (TryDestroyWall(hit.collider))
                    {
                        // 破壊した壁の1マス先へ進み、同じ方向で移動を続ける。
                        // 法線は軸に平行な単位ベクトルなので、逆方向へ1マス分進めば貫通先の位置になる。
                        // (旧実装はcos(法線の偏角)で除算しており、法線がZ軸方向のときゼロ除算でNaN/Infinityになりプレイヤーが場外に飛ぶ不具合があった)
                        if (i != reflectCount)
                        {
                            // エナジーウォールの斜面は法線が斜めなので、進行方向へ板の厚さ分だけ進める
                            if (isSlope)
                            {
                                _pos = slopePassPos;
                            }
                            else
                            {
                                Vector3 moveVec = -hit.normal;
                                _pos = hit.point + moveVec;
                            }

                            player.transform.position = _pos;
                        }

                        continue;
                    }
                }

                // 破壊できない壁・2枚目以降の壁は法線で反射する。
                _direction = Vector3.Reflect(_direction, hit.normal);
                if (i != reflectCount) _pos = player.PlayerTransform.position;
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
        /// 当たったコライダーが破壊可能な壁(<see cref="BuildingView"/>)なら破壊してプールへ返す。
        /// 破壊した場合は true、対象外(外周壁など)や登録されていない壁なら false を返す。
        /// </summary>
        private static bool TryDestroyWall(Collider wallCollider)
        {
            if (wallCollider == null || BuildingManager.Instance == null)
            {
                return false;
            }

            var buildingView = wallCollider.GetComponentInParent<BuildingView>();
            if (buildingView == null)
            {
                return false;
            }

            // BuildingView.Set(h, w) は localPosition を (w, 0.25, h) にしている。
            Vector3 local = buildingView.transform.localPosition;
            int w = Mathf.RoundToInt(local.x);
            int h = Mathf.RoundToInt(local.z);

            // ステージの壁・ビルダーの壁・エナジーウォールのどれかとして登録されていれば壊す。
            // どれにも登録されていない場合は壊せないので、通常どおり反射させる
            return BuildingManager.Instance.TryRemoveWallAt(h, w) || EnergyWallManager.Instance.TryRemoveAt(h, w);
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
