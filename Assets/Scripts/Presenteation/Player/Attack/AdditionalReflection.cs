using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

using EchoEdge.App.Battle;
using EchoEdge.Infra.Camera;
using EchoEdge.Presenter.UI;

namespace EchoEdge.Presenter.Player
{
    /// <summary>
    /// 分度器QTEの配置。角度はすべて度数法。
    /// Rotation 以外は分度器ローカルの角度で、0 度 = 分度器の右端、90 度 = 真ん中、180 度 = 左端 (反時計回り)。
    /// </summary>
    public readonly struct ReflectGaugeLayout
    {
        /// <summary>分度器全体の回転 (画面上で反時計回り)。0 で弧が上を向く</summary>
        public readonly float Rotation;

        /// <summary>表示するエリアの開始角度</summary>
        public readonly float AreaMin;

        /// <summary>表示するエリアの終了角度</summary>
        public readonly float AreaMax;

        /// <summary>成功と判定する範囲の開始角度 (表示より少し広い)</summary>
        public readonly float JudgeMin;

        /// <summary>成功と判定する範囲の終了角度 (表示より少し広い)</summary>
        public readonly float JudgeMax;

        /// <summary>針の開始角度 (エリアの反対側の端)</summary>
        public readonly float NeedleStart;

        /// <summary>針が向かう角度 (NeedleStart の反対側の端)</summary>
        public readonly float NeedleEnd;

        public ReflectGaugeLayout(float rotation, float areaMin, float areaMax, float judgeMin, float judgeMax,
            float needleStart, float needleEnd)
        {
            Rotation = rotation;
            AreaMin = areaMin;
            AreaMax = areaMax;
            JudgeMin = judgeMin;
            JudgeMax = judgeMax;
            NeedleStart = needleStart;
            NeedleEnd = needleEnd;
        }

        /// <summary>針の角度が成功の判定範囲に入っているか</summary>
        public bool Contains(float angle) => angle >= JudgeMin && angle <= JudgeMax;
    }

    /// <summary>
    /// 追加反射 (仕様: 1-1-5. 反射)。
    /// 【反射回数】を使い切ったあと、分度器QTEに成功するたびに1回ずつ追加で反射する。
    /// 盤面の x を画面の右、z を画面の上として角度を計算する。
    /// </summary>
    public static class AdditionalReflection
    {
        private const float HalfCircle = 180f;

        /// <summary>
        /// 判定を表示エリアより左右それぞれ何度広く取るか (度)。表示の境目ぎりぎりで止めても成功しやすくする
        /// </summary>
        private const float AdditionalAngle = 2f;

        /// <summary>
        /// 分度器QTEを行い、追加の反射を行うかを返す。
        /// 分度器が未配置の場合、追加反射の上限に達している場合、ステージクリア後の場合は false。
        /// </summary>
        /// <param name="incoming">壁に当たる直前の進行方向</param>
        /// <param name="wallNormal">当たった壁の法線</param>
        /// <param name="additionalCount">これまでに行った追加反射の回数</param>
        public static async UniTask<bool> TryAsync(Vector3 incoming, Vector3 wallNormal, int additionalCount,
            CancellationToken cancellationToken = default)
        {
            // ステージクリア後は分度器QTEを出さず、攻撃をそこで終える
            if (GameClearManager.IsGameClearSequenceRunning) return false;

            var gauge = ReflectGaugeView.Instance;
            if (gauge == null) return false;

            if (gauge.MaxAdditionalReflectCount > 0 && additionalCount >= gauge.MaxAdditionalReflectCount)
            {
                return false;
            }

            // 攻撃QTEとクリックが重ならないよう、表示中なら終わるまで待つ
            await UniTask.WaitWhile(() => BattleManager.IsAttackQTEPlaying, cancellationToken: cancellationToken);

            // 攻撃QTEの結果でステージクリアになった場合も分度器QTEは出さない
            if (GameClearManager.IsGameClearSequenceRunning) return false;

            // エリアは追加反射のたびに半分にする (45 → 22.5 → 11.25 ...)
            var areaWidth = gauge.InitialAreaWidth / Mathf.Pow(2f, additionalCount);
            var layout = CreateLayout(incoming, wallNormal, areaWidth);

            // 入射角は「外側から中心へ向かう」向きなので、進行方向の逆向きの角度で表す
            var incidentAngle = ToAngle(-new Vector2(incoming.x, incoming.z));
            var reflectAngle = ToAngle(Vector2.Reflect(new Vector2(incoming.x, incoming.z),
                new Vector2(wallNormal.x, wallNormal.z).normalized));

            // QTE 中はカメラをプレイヤーへ近づける
            var camera = CameraManager.Instance;
            if (camera != null) camera.ActReflectGaugeZoomIn(PlayerController.Instance.PlayerTransform.position).Forget();
            // 分度器QTEの途中でステージクリアになったら (敵の撃破処理が遅れて届いた場合など)、
            // QTEを打ち切って追加反射しない。打ち切らないと、クリア後の選択のあとに分度器が残る
            using var clearCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            UniTask.WaitUntil(() => GameClearManager.IsGameClearSequenceRunning, cancellationToken: clearCts.Token)
                .ContinueWith(() => clearCts.Cancel())
                .SuppressCancellationThrow()
                .Forget();
            try
            {
                return await gauge.PlayAsync(layout, clearCts.Token);
            }
            catch (OperationCanceledException) when (GameClearManager.IsGameClearSequenceRunning
                                                     && !cancellationToken.IsCancellationRequested)
            {
                return false;
            }
            finally
            {
                clearCts.Cancel();
                // クリア後はクリア演出のカメラに任せ、ズーム前へ戻さない
                if (camera != null && !GameClearManager.IsGameClearSequenceRunning)
                {
                    camera.ActReflectGaugeZoomOut().Forget();
                }
            }
        }

        /// <summary>
        /// 分度器の向きの刻み (度)。壁の面 (上下左右・斜め45度) に合わせる
        /// </summary>
        private const float RotationStep = 45f;

        /// <summary>
        /// 入射方向と壁の法線から分度器の配置を求める。
        /// 分度器は弧の真ん中が壁の法線 (45度刻み) を向くように回転し、エリアは反射角を中心に areaWidth 度の幅で表示する。
        /// 反射角が分度器の端に近くエリアがはみ出す場合は、分度器を反射する側へ90度回して収める。
        /// </summary>
        public static ReflectGaugeLayout CreateLayout(Vector3 incoming, Vector3 wallNormal, float areaWidth)
        {
            var dir = new Vector2(incoming.x, incoming.z);
            var normal = new Vector2(wallNormal.x, wallNormal.z);
            if (normal.sqrMagnitude < 1e-6f) normal = -dir;
            normal.Normalize();

            // 反射角: 中心から外側へ向かうベクトルの向き
            var reflected = Vector2.Reflect(dir, normal);
            var reflectAngle = ToAngle(reflected);

            // 弧の真ん中 (90度) が壁の面の法線を向くように回転させる (Unity の Z 回転、反時計回りが正)
            var normalAngle = Mathf.Round(ToAngle(normal) / RotationStep) * RotationStep;
            var rotation = Mathf.DeltaAngle(0f, normalAngle - 90f);

            var halfWidth = Mathf.Clamp(areaWidth, 0f, HalfCircle) * 0.5f;

            // 分度器ローカルでの反射角 (-180〜180、0〜180 が分度器の範囲)
            var localReflect = Mathf.DeltaAngle(0f, reflectAngle - rotation);

            // 端に寄ってエリアがはみ出すなら、左右 (反射する側) を中心にした分度器にする
            if (localReflect - halfWidth < 0f && localReflect < 90f)
            {
                rotation -= 90f;
            }
            else if (localReflect + halfWidth > HalfCircle)
            {
                rotation += 90f;
            }
            rotation = Mathf.DeltaAngle(0f, rotation);
            localReflect = Mathf.Clamp(Mathf.Repeat(reflectAngle - rotation, 360f), 0f, HalfCircle);

            var areaMin = Mathf.Clamp(localReflect - halfWidth, 0f, HalfCircle);
            var areaMax = Mathf.Clamp(localReflect + halfWidth, 0f, HalfCircle);

            // 針はエリアの反対側の端から、片道だけ回転する
            var needleStart = localReflect < 90f ? HalfCircle : 0f;
            var needleEnd = HalfCircle - needleStart;

            // 判定は表示より AdditionalAngle だけ広げる
            var judgeMin = Mathf.Max(areaMin - AdditionalAngle, 0f);
            var judgeMax = Mathf.Min(areaMax + AdditionalAngle, HalfCircle);

            return new ReflectGaugeLayout(rotation, areaMin, areaMax, judgeMin, judgeMax, needleStart, needleEnd);
        }

        /// <summary>
        /// ベクトルの向きを、右を 0 度として反時計回りの角度 (0〜360) にする
        /// </summary>
        private static float ToAngle(Vector2 v)
        {
            return Mathf.Repeat(Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg, 360f);
        }
    }
}
