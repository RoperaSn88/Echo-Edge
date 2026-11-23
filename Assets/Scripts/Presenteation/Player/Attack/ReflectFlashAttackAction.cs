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
using EchoEdge.Presenter.Battle;
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

            // 開始時の待機が終わったので、ここから敵へのダメージ判定を有効にする
            player.BeginAttackHit();

            for (int i = 0; ; i++)
            {
                // 反射するたびに敵のリストをクリアする。
                _damagedEnemies.Clear();

                // 壁までの光線上の敵を斬りながら壁へ移動する。
                // 途中に床ギミック(ワープホール・リエイムパッド)があれば、そこまでの敵を斬ってから処理し、続きを進む(反射回数は消費しない)。
                RaycastHit wallHit;
                bool isCleared = false;
                while (true)
                {
                    Ray ray = new Ray(pos, direction);
                    player.SetDebugDirection(direction);
                    if (!Physics.Raycast(ray, out wallHit, math.INFINITY, player.LayerMask))
                    {
                        throw new System.Exception("当たってない...だと");
                    }

                    float wallDistance = Vector3.Distance(pos, wallHit.point);
                    bool hasGimmick = FloorGimmickManager.TryFindNext(pos, direction, wallDistance, null, out var gimmick, out var gimmickDistance);
                    float stopDistance = hasGimmick ? gimmickDistance : wallDistance;

                    // 光線に触れた敵を、プレイヤーから近い順に並べる
                    var enemyHits = Physics.RaycastAll(ray, stopDistance, ~0, QueryTriggerInteraction.Collide)
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

                    // 敵の撃破でステージクリアになったら、壁まで進まず反射もせずに攻撃を終える
                    if (GameClearManager.IsGameClearSequenceRunning)
                    {
                        isCleared = true;
                        break;
                    }

                    if (hasGimmick)
                    {
                        // 床ギミックに入る位置まで瞬時に移動する
                        Vector3 entry = pos + direction * gimmickDistance;
                        entry.y = player.PlayerTransform.position.y;
                        player.PlayerTransform.position = entry - direction * 0.5f;
                        await player.PlayerTransform.DOMove(entry, FlashAttackSlashDuration);

                        if (gimmick is WarpHoleView warpHole)
                        {
                            // 対のワープホールへ移り、同じ向きで進み続ける
                            pos = warpHole.Warp(player.PlayerTransform);
                        }
                        else if (gimmick is ReAimPadView reAimPad)
                        {
                            // パッドの中心へ寄せてから、そこから向きを選び直させる(残りの反射回数はそのまま)
                            reAimPad.SetUsed(true);
                            Vector3 center = reAimPad.CellCenter;
                            center.y = player.PlayerTransform.position.y;
                            await player.PlayerTransform.DOMove(center, FlashAttackSlashDuration);

                            direction = await ReAimDirectionSelector.SelectAsync(center, Mathf.Max(0, reflectCount - i));
                            pos = player.PlayerTransform.position;
                        }
                        else
                        {
                            pos = player.PlayerTransform.position;
                        }
                        continue;
                    }

                    // 壁の位置まで瞬時に移動する
                    player.PlayerTransform.position = wallHit.point - direction * 0.5f;
                    await player.PlayerTransform.DOMove(wallHit.point, FlashAttackSlashDuration);
                    break;
                }

                if (isCleared) break;

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

            // 戻り先に敵がいてもダメージを与えないよう、攻撃判定を切ってから戻す
            player.EndAttackHit();
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
