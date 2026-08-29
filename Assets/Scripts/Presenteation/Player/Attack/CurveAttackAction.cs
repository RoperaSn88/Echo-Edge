using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 曲線を描く攻撃の実装。
/// プレイヤーとポインター(targetPos)を端点とする2次ベジェ曲線を描いて移動する。
/// targetPos到達後は X軸の速度をそのままに、曲線が持っていた加速度(速度の加減)を
/// Z軸(作業メモ上の「y軸」)へ与え続けて放物線状に飛び続ける。
/// 壁に当たった場合は、当たった面の軸方向の速度成分を反転させて跳ね返る。
/// 曲線フェーズ・放物線フェーズとも移動区間を分割してレイキャストするため、1マスの壁でもすり抜けない。
/// 移動中にOnTriggerEnterで敵に触れた場合はダメージを与える。
/// めちゃくちゃ早い一閃(Flash)版は、とりあえず移動が速くなるだけで挙動は同じ。
/// </summary>
public class CurveAttackAction: IPlayerAttackAction
{
    private static CurveAttackAction _instance;

    public static CurveAttackAction Instance => _instance ??= new CurveAttackAction();

    /// <summary>
    /// 曲線移動の基準速度。端点間距離からおおよその移動時間を算出するのに使う。
    /// </summary>
    private const float Speed = 23f;

    /// <summary>
    /// めちゃくちゃ早い一閃(Flash)版の基準速度。挙動は通常版と同じで、移動だけ速くする。
    /// </summary>
    private const float FlashSpeed = 46f;

    /// <summary>
    /// 曲線の曲率。制御点を、端点の中点から垂直方向へ (Curvature × 端点間距離) だけずらす。
    /// </summary>
    private const float Curvature = 0.5f;

    /// <summary>
    /// 各フェーズ前後の待機時間。
    /// </summary>
    private const float AwaitTime = 0.5f;

    /// <summary>
    /// targetPos到達後の放物線移動を打ち切るまでの最大時間(保険)。
    /// </summary>
    private const float MaxProjectileDuration = 3f;

    /// <summary>
    /// 1回のレイキャストで判定する最大移動距離。これを超える移動は分割して判定し、
    /// 高速移動時に薄い壁(1マス)をすり抜けるのを防ぐ。
    /// </summary>
    private const float MaxStepDistance = 0.4f;

    /// <summary>
    /// 壁ヒット後に壁面から離す距離。次のレイを壁面ちょうどから飛ばして
    /// 取りこぼすのを防ぐためのマージンも兼ねる。
    /// </summary>
    private const float SkinWidth = 0.05f;

    /// <summary>
    /// この攻撃のダメージ倍率。反射攻撃(基準値1.0)より弱くするため0.7倍とする。
    /// </summary>
    private const float DamageRate = 0.7f;

    /// <summary>
    /// start と end を端点とする2次ベジェ曲線を描いて移動し、
    /// 到達後は X軸の速度を保ったまま曲線の加速度を Z軸へ与え続けて飛ばす。
    /// 壁に当たったらその軸の速度を反転して跳ね返す。
    /// </summary>
    /// <param name="targetPos">ポインターの先の位置</param>
    public async UniTask ExecuteAsync(Vector3 targetPos)
    {
        var player = PlayerController.Instance;

        UIPresenter.Instance.ResetFade();

        Vector3 start = player.PlayerTransform.position;
        // 移動はXZ平面上で行うため、端点の高さは始点に揃える。
        Vector3 end = new Vector3(targetPos.x, start.y, targetPos.z);

        Vector3 flatDir = end - start;
        float distance = flatDir.magnitude;

        PlayerView.Instance.Animator.SetBool("AttackingF", true);

        BattleManager.ResetReflectionCount();
        await UniTask.Delay(TimeSpan.FromSeconds(AwaitTime));

        // --- 要件1: start と end を端点とする2次ベジェ曲線を描いて移動する ---
        Vector3 forward = distance > Mathf.Epsilon ? flatDir / distance : Vector3.right;
        Vector3 perpendicular = Vector3.Cross(Vector3.up, forward);
        // 画面上方向(+Z)へふくらむように制御点を配置する。
        if (perpendicular.z < 0f) perpendicular = -perpendicular;
        Vector3 control = (start + end) * 0.5f + perpendicular * (Curvature * distance);

        // 一閃(Flash)版は移動を速くするだけ。曲線の形も加速度も duration 経由で自動的に速くなる。
        float speed = player.IsFlashAttack ? FlashSpeed : Speed;
        float duration = Mathf.Max(distance / speed, 0.01f);

        // 2次ベジェの2階微分は一定。これが「1の方法で演じた速度の加減」にあたる。
        float accelerationZ = (2f * (start - 2f * control + end) / (duration * duration)).z;

        player.ResetAfterimageAnchor();

        // targetPos到達後に使う速度(実時間基準)。曲線の途中で壁に当たった場合は
        // その時点の曲線速度を反射したものを引き継ぐ。
        Vector3 velocity = Vector3.zero;
        bool hitWallDuringCurve = false;

        float elapsed = 0f;
        Vector3 previousCurvePoint = start;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 nextCurvePoint = EvaluateBezier(start, control, end, t);

            // 1フレーム分の曲線移動を分割し、途中に壁があれば止めて反射する。
            if (TryMoveAlong(player, previousCurvePoint, nextCurvePoint, out RaycastHit curveHit))
            {
                // 曲線パラメータ基準の速度を実時間基準へ変換し、壁で反射させてフェーズ2へ引き継ぐ。
                velocity = ReflectVelocityOnAxis(
                    EvaluateBezierVelocity(start, control, end, t) / duration, curveHit.normal);
                player.SpawnAfterimageIfNeeded();
                hitWallDuringCurve = true;
                break;
            }

            player.SpawnAfterimageIfNeeded();
            previousCurvePoint = nextCurvePoint;
            await UniTask.Yield();
        }

        if (!hitWallDuringCurve)
        {
            player.PlayerTransform.position = end;
            // --- 要件2: 到達後は X軸の速度をそのままに、曲線の加速度を Z軸へ与え続ける ---
            // 曲線パラメータ t は duration 秒かけて 0→1 に進むので、実時間基準へ変換する。
            velocity = EvaluateBezierVelocity(start, control, end, 1f) / duration;
        }

        int maxBounces = BattleManager.PlayerStatus.Move;
        int bounces = 0;
        float projectileElapsed = 0f;

        while (bounces <= maxBounces && projectileElapsed < MaxProjectileDuration)
        {
            float dt = Time.deltaTime;
            projectileElapsed += dt;

            // X軸の動きはそのまま、Z軸(作業メモ上の「y軸」)だけ加速度を維持する。
            velocity.z += accelerationZ * dt;

            Vector3 current = player.PlayerTransform.position;
            Vector3 frameMove = new Vector3(velocity.x, 0f, velocity.z) * dt;
            float moveDistance = frameMove.magnitude;

            player.SetDebugRay(current, moveDistance > Mathf.Epsilon ? frameMove / moveDistance : velocity);

            if (moveDistance > Mathf.Epsilon &&
                TryMoveAlong(player, current, current + frameMove, out RaycastHit hit))
            {
                // --- 要件3: 当たった面の軸に適した速度成分を反転する ---
                velocity = ReflectVelocityOnAxis(velocity, hit.normal);
                bounces++;
                BattleManager.SetReflectionCount(bounces);
            }

            player.SpawnAfterimageIfNeeded();
            await UniTask.Yield();
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
    /// from から to へプレイヤーを移動させる。区間を <see cref="MaxStepDistance"/> ごとに分割して
    /// レイキャストし、壁に当たった場合はその手前(壁面から <see cref="SkinWidth"/> 離した位置)で
    /// 止めて true を返す。壁が無ければ to へ移動して false を返す。
    /// </summary>
    private static bool TryMoveAlong(PlayerController player, Vector3 from, Vector3 to, out RaycastHit hit)
    {
        hit = default;

        Vector3 delta = to - from;
        float totalDistance = delta.magnitude;
        if (totalDistance <= Mathf.Epsilon)
        {
            player.PlayerTransform.position = to;
            return false;
        }

        Vector3 direction = delta / totalDistance;
        int subSteps = Mathf.Max(1, Mathf.CeilToInt(totalDistance / MaxStepDistance));

        for (int i = 1; i <= subSteps; i++)
        {
            Vector3 subTarget = Vector3.Lerp(from, to, (float)i / subSteps);
            Vector3 origin = player.PlayerTransform.position;
            float subDistance = Vector3.Distance(origin, subTarget);

            // 壁面ちょうどから飛ばすとヒットを取りこぼすため、少し手前を起点にする。
            if (Physics.Raycast(origin - direction * SkinWidth, direction, out hit,
                    subDistance + SkinWidth, player.LayerMask))
            {
                player.PlayerTransform.position = hit.point + hit.normal * SkinWidth;
                return true;
            }

            player.PlayerTransform.position = subTarget;
        }

        return false;
    }

    /// <summary>
    /// 当たった面の法線の主軸に応じて、速度のその軸成分だけを反転する。
    /// </summary>
    private static Vector3 ReflectVelocityOnAxis(Vector3 velocity, Vector3 normal)
    {
        if (Mathf.Abs(normal.x) >= Mathf.Abs(normal.z))
        {
            velocity.x = -velocity.x;
        }
        else
        {
            velocity.z = -velocity.z;
        }

        return velocity;
    }

    /// <summary>
    /// 2次ベジェ曲線上の位置を求める。
    /// </summary>
    private static Vector3 EvaluateBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float u = 1f - t;
        return u * u * p0 + 2f * u * t * p1 + t * t * p2;
    }

    /// <summary>
    /// 2次ベジェ曲線の1階微分(パラメータ基準の速度)を求める。
    /// </summary>
    private static Vector3 EvaluateBezierVelocity(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float u = 1f - t;
        return 2f * u * (p1 - p0) + 2f * t * (p2 - p1);
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
