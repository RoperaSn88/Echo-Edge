using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;

using EchoEdge.App.Battle;
using EchoEdge.Domain.Battle;
using EchoEdge.Presenter.UI;

namespace EchoEdge.Presenter.Player
{
    /// <summary>
    /// めちゃくちゃ早い一閃(反射)の実装。
    /// プレイヤーからポインター方向へ光線を飛ばし、当たった壁の位置へ瞬時に移動する。
    /// 光線上に敵がいる場合はプレイヤーに近い順に斬りつけたのち、壁の位置まで瞬時に移動する。
    /// 反射回数の分だけ壁の法線で反射しながら繰り返し、最後に元の位置へ戻る。
    /// 反射回数を使い切ったあとは、分度器QTEに成功するたびに追加で反射する。
    /// ダメージ判定は光線上の敵に対して直接行うため、OnTriggerEnterでは何もしない。
    /// </summary>
    public class ReflectFlashAttackAction: IPlayerAttackAction
    {
        private static ReflectFlashAttackAction _instance;

        public static ReflectFlashAttackAction Instance => _instance ??= new ReflectFlashAttackAction();

        /// <summary>
        /// めちゃくちゃ早い一閃で、敵を斬り抜ける際のトゥイーン時間
        /// </summary>
        private const float FlashAttackSlashDuration = 0.1f;

        /// <summary>
        /// この攻撃のダメージ倍率。反射攻撃をメインの攻撃として基準値(1.0)にする。
        /// </summary>
        private const float DamageRate = 1.0f;

        /// <summary>
        /// ダメージを与えたことのある敵のリスト。反射中に同じ敵に複数回ダメージを与えないようにするためのもの。
        /// </summary>
        private readonly List<IDamageActivator> _damagedEnemies = new();

        public async UniTask ExecuteAsync(Vector3 targetPos)
        {
            var player = PlayerController.Instance;

            int reflectCount = BattleManager.PlayerStatus.Move;

            // 【反射回数】を使い切ったあとに、分度器QTEで追加した反射の回数
            int additionalCount = 0;
            PlayerView.Instance.Animator.SetBool("AttackingF", true);

            Vector3 originalPosition = player.PlayerTransform.position;
            Vector3 pos = originalPosition;
            Vector3 direction = new Vector3(targetPos.x - pos.x, 0, targetPos.z - pos.z).normalized;

            await UniTask.Delay(TimeSpan.FromSeconds(0.5f));

            for (int i = 0; ; i++)
            {
                // 反射するたびに敵のリストをクリアする。
                _damagedEnemies.Clear();

                Ray ray = new Ray(pos, direction);
                player.SetDebugDirection(direction);
                if (!Physics.Raycast(ray, out RaycastHit wallHit, math.INFINITY, player.LayerMask))
                {
                    throw new System.Exception("当たってない...だと");
                }

                float wallDistance = Vector3.Distance(pos, wallHit.point);

                // 光線に触れた敵を、プレイヤーから近い順に並べる
                var enemyHits = Physics.RaycastAll(ray, wallDistance, ~0, QueryTriggerInteraction.Collide)
                    .Where(hit => hit.collider.CompareTag("Enemy"))
                    .OrderBy(hit => hit.distance)
                    .ToArray();

                foreach (var enemyHit in enemyHits)
                {
                    Vector3 enemyPos = enemyHit.collider.transform.position;
                    Vector3 slashPos = new Vector3(enemyPos.x, player.PlayerTransform.position.y, enemyPos.z);

                    // 敵の位置から光線の単位ベクトル分マイナスした位置へ瞬時に移動し、プラスした位置へ斬り抜ける
                    // 疑似的にダメージ与える
                    player.PlayerTransform.position = slashPos - direction * 0.5f;

                    // 移動はするが、攻撃が終了したタイミングで次の敵の位置に移動するように
                    CancellationTokenSource cts = new CancellationTokenSource();
                    var tween = player.PlayerTransform.DOMove(slashPos, FlashAttackSlashDuration).ToUniTask(cancellationToken: cts.Token);
                    PlayerView.Instance.Animator.SetTrigger("AttackT");
                    await TryFlashDamageEnemy(enemyHit.collider);
                    cts.Cancel();
                }

                // 壁の位置まで瞬時に移動する
                player.PlayerTransform.position = wallHit.point - direction * 0.5f;
                await player.PlayerTransform.DOMove(wallHit.point, FlashAttackSlashDuration);

                // 【反射回数】分は QTE なしで反射し、それ以上は分度器QTEに成功したときだけ反射する
                if (i >= reflectCount)
                {
                    if (!await AdditionalReflection.TryAsync(direction, wallHit.normal, additionalCount))
                    {
                        await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
                        break;
                    }
                    additionalCount++;
                }

                pos = wallHit.point;
                direction = Vector3.Reflect(direction, wallHit.normal);

                await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
            }

            BattleManager.ResetQTE();
            BattleManager.ResetCombo();
            BattleManager.ResetReflectionCount();
            UIPresenter.Instance.FadeTexts();

            PlayerView.Instance.Animator.SetBool("AttackingF", false);

            // z軸を含めて元の位置へ戻す
            player.PlayerTransform.position = originalPosition;
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
                await status.FlashDamage(DamageRate);
            }
        }

        /// <summary>
        /// めちゃくちゃ早い一閃中のダメージ判定は光線上の敵に対して直接行うため、ここでは何もしない。
        /// </summary>
        /// <param name="other">相手の当たり判定</param>
        public void OnTriggerEnter(Collider other)
        {
            // 何もしない。ダメージ判定はExecuteAsync内でTryFlashDamageEnemyにより行う。
        }

        public void OnCollisionEnter(Collision collision)
        {
            throw new NotImplementedException();
        }
    }
}
