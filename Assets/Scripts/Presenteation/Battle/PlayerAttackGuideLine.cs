using System.Collections.Generic;
using UnityEngine;

using EchoEdge.Domain.Battle;
using EchoEdge.Presenter.Player;

namespace EchoEdge.Presenter.Battle
{
    /// <summary>
    /// 一閃の角度選択中に表示する予測線。
    /// 実際の一閃(<see cref="IPlayerAttackAction"/>の各実装)と同じレイキャストで壁への当たりを求め、
    /// 反射回数の分だけ反射(貫通の剣は貫通も)を追った経路を描く。
    /// 線分(0回目, 1回目, …)ごとに LineRenderer を分け、反射回数を N として i 回目の線を
    /// 不透明度 1 - i / (N + 1) で描く(例: N = 1 なら 100%, 50%)。
    /// </summary>
    public class PlayerAttackGuideLine
    {
        private const float HeightOffset = 0.49f;
        private const float LineWidth = 0.08f;

        /// <summary>
        /// 壁に当たらなかった場合に線を伸ばす長さ(保険)。
        /// </summary>
        private const float MaxSegmentDistance = 50f;

        /// <summary>
        /// 通常の一閃は壁の手前(移動距離の99%)で止まってから次の方向へ進むため、それに合わせる。
        /// </summary>
        private const float NormalAttackStopRate = 0.99f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private readonly List<Vector3> _points = new();

        /// <summary>
        /// 各線分(i 回目の線)が始まる <see cref="_points"/> のインデックス。
        /// 線分 i は _segmentStarts[i] から次の線分の始点(最後の線分は末尾)までの点を結ぶ。
        /// </summary>
        private readonly List<int> _segmentStarts = new();

        private readonly List<LineRenderer> _lineRenderers = new();
        private MaterialPropertyBlock _propertyBlock;
        private Material _lineMaterial;
        private GameObject _lineObject;

        /// <summary>
        /// 予測線を更新する。
        /// </summary>
        /// <param name="playerPosition">プレイヤーの位置(一閃の始点)</param>
        /// <param name="pointerPosition">ポインターの先の位置</param>
        /// <param name="attackKind">剣の種類</param>
        /// <param name="isFlash">めちゃくちゃ早い一閃かどうか</param>
        /// <param name="reflectCount">反射回数</param>
        /// <param name="wallLayerMask">壁判定用のレイヤーマスク(<see cref="PlayerController.LayerMask"/>)</param>
        public void Update(Vector3 playerPosition, Vector3 pointerPosition, PlayerAttackKinds attackKind, bool isFlash,
            int reflectCount, LayerMask wallLayerMask)
        {
            Vector3 direction = new Vector3(pointerPosition.x - playerPosition.x, 0f, pointerPosition.z - playerPosition.z);
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                Hide();
                return;
            }

            _points.Clear();
            _segmentStarts.Clear();

            if (attackKind == PlayerAttackKinds.Curve)
            {
                // 弧曲の軌道は反射の追跡と別物なので、ひとまず従来どおりポインターまでの直線を描く
                _segmentStarts.Add(0);
                _points.Add(playerPosition);
                _points.Add(new Vector3(pointerPosition.x, playerPosition.y, pointerPosition.z));
            }
            else
            {
                TraceStraightAttack(playerPosition, direction.normalized, attackKind, isFlash, reflectCount, wallLayerMask);
            }

            EnsureLineObject();
            int segmentCount = _segmentStarts.Count;
            for (int i = 0; i < segmentCount; i++)
            {
                LineRenderer lineRenderer = GetLineRenderer(i);
                int start = _segmentStarts[i];
                int end = i + 1 < segmentCount ? _segmentStarts[i + 1] : _points.Count - 1;

                lineRenderer.enabled = true;
                lineRenderer.positionCount = end - start + 1;
                for (int j = start; j <= end; j++)
                {
                    Vector3 point = _points[j];
                    point.y -= HeightOffset;
                    lineRenderer.SetPosition(j - start, point);
                }

                ApplyAlpha(lineRenderer, GetSegmentAlpha(i, reflectCount));
            }

            for (int i = segmentCount; i < _lineRenderers.Count; i++)
            {
                _lineRenderers[i].enabled = false;
            }
        }

        /// <summary>
        /// i 回目の線の不透明度。反射回数 N のとき 1 - i / (N + 1)。
        /// </summary>
        private static float GetSegmentAlpha(int segmentIndex, int reflectCount)
        {
            if (reflectCount < 0) reflectCount = 0;
            return Mathf.Clamp01(1f - (float)segmentIndex / (reflectCount + 1));
        }

        /// <summary>
        /// 直進する一閃(反射・貫通・爆発)の経路を、反射回数 + 1 本の線分として求める。
        /// 各 <see cref="IPlayerAttackAction"/> 実装の ExecuteAsync と同じ規則で次の始点・方向を決める。
        /// </summary>
        private void TraceStraightAttack(Vector3 origin, Vector3 direction, PlayerAttackKinds attackKind, bool isFlash,
            int reflectCount, LayerMask wallLayerMask)
        {
            _points.Add(origin);
            bool firstWallResolved = false;

            for (int i = 0; i <= reflectCount; i++)
            {
                // i 回目の線は、直前の線の終点(壁に当たった点・貫通した先)から始まる
                _segmentStarts.Add(_points.Count - 1);

                var ray = new Ray(origin, direction);
                if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, wallLayerMask))
                {
                    _points.Add(origin + direction * MaxSegmentDistance);
                    return;
                }

                _points.Add(hit.point);
                if (i == reflectCount) return;

                // 貫通できる壁なら、壁の向こう側から同じ方向で進む
                if (attackKind == PlayerAttackKinds.Pierce && TryGetPierceExit(hit, direction, isFlash, ref firstWallResolved, out Vector3 exit))
                {
                    _points.Add(exit);
                    origin = exit;
                    continue;
                }

                // めちゃくちゃ早い一閃(反射)は壁面から、それ以外は壁の手前で止まった位置から反射する
                origin = attackKind == PlayerAttackKinds.Reflect && isFlash
                    ? hit.point
                    : Vector3.Lerp(origin, hit.point, NormalAttackStopRate);
                direction = Vector3.Reflect(direction, hit.normal);
            }
        }

        /// <summary>
        /// 貫通の剣で、当たった壁を通り抜けられるかを判定し、通り抜けた先の位置を返す。
        /// 通常版は Block レイヤーの壁を毎回貫通し(<see cref="PierceAttackAction"/>)、
        /// めちゃくちゃ早い一閃版は最初に当たった壁だけを、壊せる壁なら壊して貫通する(<see cref="PierceFlashAttackAction"/>)。
        /// 予測線では実際には壊さないので、壊せる壁かどうかは <see cref="BuildingView"/> を持つかで判定する。
        /// </summary>
        private static bool TryGetPierceExit(RaycastHit hit, Vector3 direction, bool isFlash, ref bool firstWallResolved, out Vector3 exit)
        {
            exit = hit.point;

            if (isFlash)
            {
                if (firstWallResolved) return false;
                firstWallResolved = true;
                if (hit.collider.GetComponentInParent<BuildingView>() == null) return false;
            }
            else if (hit.collider.gameObject.layer != LayerMask.NameToLayer("Block"))
            {
                return false;
            }

            if (!SlopeWallView.TryGetPassThroughPosition(hit, direction, out exit))
            {
                exit = hit.point - hit.normal;
            }
            return true;
        }

        /// <summary>
        /// 線分ごとの不透明度を反映する。マテリアルは共有なので MaterialPropertyBlock で個別に上書きする。
        /// 線のマテリアル(URP Lit, Additive)は発光色が不透明度で弱まらないため、発光色にも不透明度を掛ける。
        /// </summary>
        private void ApplyAlpha(LineRenderer lineRenderer, float alpha)
        {
            // 頂点カラーを使うシェーダー向け
            Color vertexColor = new Color(1f, 1f, 1f, alpha);
            lineRenderer.startColor = vertexColor;
            lineRenderer.endColor = vertexColor;

            Material material = lineRenderer.sharedMaterial;
            if (material == null) return;

            _propertyBlock ??= new MaterialPropertyBlock();
            _propertyBlock.Clear();
            if (material.HasProperty(BaseColorId))
            {
                Color baseColor = material.GetColor(BaseColorId);
                baseColor.a *= alpha;
                _propertyBlock.SetColor(BaseColorId, baseColor);
            }
            if (material.HasProperty(EmissionColorId))
            {
                Color emissionColor = material.GetColor(EmissionColorId);
                _propertyBlock.SetColor(EmissionColorId, new Color(emissionColor.r * alpha, emissionColor.g * alpha, emissionColor.b * alpha, emissionColor.a));
            }
            lineRenderer.SetPropertyBlock(_propertyBlock);
        }

        public void Hide()
        {
            foreach (LineRenderer lineRenderer in _lineRenderers)
            {
                if (lineRenderer != null) lineRenderer.enabled = false;
            }
        }

        public void Destroy()
        {
            _lineRenderers.Clear();
            if (_lineObject == null) return;
            Object.Destroy(_lineObject);
            _lineObject = null;
        }

        private void EnsureLineObject()
        {
            if (_lineObject != null) return;

            _lineRenderers.Clear();
            _lineObject = new GameObject("PlayerAttackGuideLine");
            if (_lineMaterial == null && PlayerController.Instance != null)
            {
                _lineMaterial = PlayerController.Instance.LineMaterial;
            }
        }

        /// <summary>
        /// i 回目の線を描く LineRenderer を返す。足りなければ子オブジェクトとして作る。
        /// </summary>
        private LineRenderer GetLineRenderer(int segmentIndex)
        {
            while (_lineRenderers.Count <= segmentIndex)
            {
                var segmentObject = new GameObject($"Segment{_lineRenderers.Count}");
                segmentObject.transform.SetParent(_lineObject.transform, false);
                var lineRenderer = segmentObject.AddComponent<LineRenderer>();
                lineRenderer.positionCount = 2;
                lineRenderer.startWidth = LineWidth;
                lineRenderer.endWidth = LineWidth;
                lineRenderer.numCornerVertices = 2;
                lineRenderer.useWorldSpace = true;
                lineRenderer.enabled = false;
                lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lineRenderer.receiveShadows = false;
                if (_lineMaterial != null)
                {
                    lineRenderer.sharedMaterial = _lineMaterial;
                }
                _lineRenderers.Add(lineRenderer);
            }
            return _lineRenderers[segmentIndex];
        }

        public void SetMaterial(Material material)
        {
            _lineMaterial = material;
            foreach (LineRenderer lineRenderer in _lineRenderers)
            {
                if (lineRenderer != null) lineRenderer.sharedMaterial = _lineMaterial;
            }
        }
    }
}
