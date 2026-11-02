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

        private readonly List<Vector3> _points = new();
        private Material _lineMaterial;
        private GameObject _lineObject;
        private LineRenderer _lineRenderer;

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

            if (attackKind == PlayerAttackKinds.Curve)
            {
                // 弧曲の軌道は反射の追跡と別物なので、ひとまず従来どおりポインターまでの直線を描く
                _points.Add(playerPosition);
                _points.Add(new Vector3(pointerPosition.x, playerPosition.y, pointerPosition.z));
            }
            else
            {
                TraceStraightAttack(playerPosition, direction.normalized, attackKind, isFlash, reflectCount, wallLayerMask);
            }

            EnsureLineRenderer();
            _lineRenderer.enabled = true;
            _lineRenderer.positionCount = _points.Count;
            for (int i = 0; i < _points.Count; i++)
            {
                Vector3 point = _points[i];
                point.y -= HeightOffset;
                _lineRenderer.SetPosition(i, point);
            }
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

        public void Hide()
        {
            if (_lineRenderer == null) return;
            _lineRenderer.enabled = false;
        }

        public void Destroy()
        {
            if (_lineObject == null) return;
            Object.Destroy(_lineObject);
            _lineObject = null;
            _lineRenderer = null;
        }

        private void EnsureLineRenderer()
        {
            if (_lineRenderer != null) return;

            _lineObject = new GameObject("PlayerAttackGuideLine");
            _lineRenderer = _lineObject.AddComponent<LineRenderer>();
            _lineRenderer.positionCount = 2;
            _lineRenderer.startWidth = LineWidth;
            _lineRenderer.endWidth = LineWidth;
            _lineRenderer.numCornerVertices = 2;
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.enabled = false;
            _lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _lineRenderer.receiveShadows = false;
            if (_lineMaterial == null && PlayerController.Instance != null)
            {
                _lineMaterial = PlayerController.Instance.LineMaterial;
            }
            if (_lineMaterial != null)
            {
                _lineRenderer.sharedMaterial = _lineMaterial;
            }
        }

        public void SetMaterial(Material material)
        {
            _lineMaterial = material;
            if (_lineRenderer == null) return;
            _lineRenderer.sharedMaterial = _lineMaterial;
        }
    }
}
