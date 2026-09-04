using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
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
        /// 一閃版の移動速度。通常の貫通攻撃(PierceAttackAction.Speed = 23)より速くする。
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
                player.ResetAfterimageAnchor();
                await player.transform.DOMove(endVec, distance / Speed)
                    .OnUpdate(player.SpawnAfterimageIfNeeded);

                // 一番初めに当たった壁は、破壊できるなら破壊して貫通する(反射しない)。
                if (!firstWallResolved)
                {
                    firstWallResolved = true;

                    if (TryDestroyWall(hit.collider))
                    {
                        // 破壊した壁の1マス先へ進み、同じ方向で移動を続ける。
                        if (i != reflectCount)
                        {
                            float rad = Mathf.Atan2(hit.normal.z, hit.normal.x);
                            var rate = 1f / Mathf.Cos(rad);
                            var moveVec = new Vector3(hit.normal.x * rate, 0, hit.normal.z * rate);

                            _pos = hit.point + moveVec;
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
        /// 破壊した場合は true、対象外(外周壁など)なら false を返す。
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

            BuildingManager.Instance.TryRemoveWallAt(h, w);
            return true;
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
}
