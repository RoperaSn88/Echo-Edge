using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

using EchoEdge.App.Battle;
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

        /// <summary>追加の反射ができるエリアの開始角度</summary>
        public readonly float AreaMin;

        /// <summary>追加の反射ができるエリアの終了角度</summary>
        public readonly float AreaMax;

        /// <summary>針の開始角度 (エリアの反対側の端)</summary>
        public readonly float NeedleStart;

        /// <summary>針が向かう角度 (NeedleStart の反対側の端)</summary>
        public readonly float NeedleEnd;

        public ReflectGaugeLayout(float rotation, float areaMin, float areaMax, float needleStart, float needleEnd)
        {
            Rotation = rotation;
            AreaMin = areaMin;
            AreaMax = areaMax;
            NeedleStart = needleStart;
            NeedleEnd = needleEnd;
        }

        public bool Contains(float angle) => angle >= AreaMin && angle <= AreaMax;
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
        /// 分度器QTEを行い、追加の反射を行うかを返す。
        /// 分度器が未配置の場合や、追加反射の上限に達している場合は false。
        /// </summary>
        /// <param name="incoming">壁に当たる直前の進行方向</param>
        /// <param name="wallNormal">当たった壁の法線</param>
        /// <param name="additionalCount">これまでに行った追加反射の回数</param>
        public static async UniTask<bool> TryAsync(Vector3 incoming, Vector3 wallNormal, int additionalCount,
            CancellationToken cancellationToken = default)
        {
            var gauge = ReflectGaugeView.Instance;
            if (gauge == null) return false;

            if (gauge.MaxAdditionalReflectCount > 0 && additionalCount >= gauge.MaxAdditionalReflectCount)
            {
                return false;
            }

            // 攻撃QTEとクリックが重ならないよう、表示中なら終わるまで待つ
            await UniTask.WaitWhile(() => BattleManager.IsAttackQTEPlaying, cancellationToken: cancellationToken);

            var layout = CreateLayout(incoming, wallNormal, gauge.AreaWidth);
            return await gauge.PlayAsync(layout, cancellationToken);
        }

        /// <summary>
        /// 入射方向と壁の法線から分度器の配置を求める。
        /// 分度器は弧が壁の法線方向を向くように回転し、エリアは反射角を中心に areaWidth 度の幅で表示する。
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

            // 弧の真ん中 (90度) が法線を向くように回転させる
            var rotation = Mathf.DeltaAngle(0f, ToAngle(normal) - 90f);

            // 分度器ローカルでの反射角 (0〜180)
            var localReflect = Mathf.Clamp(Mathf.Repeat(reflectAngle - rotation, 360f), 0f, HalfCircle);

            var halfWidth = Mathf.Clamp(areaWidth, 0f, HalfCircle) * 0.5f;
            var areaMin = Mathf.Clamp(localReflect - halfWidth, 0f, HalfCircle);
            var areaMax = Mathf.Clamp(localReflect + halfWidth, 0f, HalfCircle);

            // 針はエリアの反対側の端から回転を始める
            var needleStart = localReflect < 90f ? HalfCircle : 0f;
            var needleEnd = HalfCircle - needleStart;

            return new ReflectGaugeLayout(rotation, areaMin, areaMax, needleStart, needleEnd);
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
