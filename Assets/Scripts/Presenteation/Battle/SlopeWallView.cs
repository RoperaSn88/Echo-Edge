using UnityEngine;

using EchoEdge.Domain.Battle;

namespace EchoEdge.Presenter.Battle
{
    /// <summary>
    /// 壁(BuildingView)の見た目と当たり判定を、斜面の板に切り替えるクラス。
    /// 壁のプールはビルダーの壁と共有しているため、プールへ返す前に必ず <see cref="Restore"/> で元に戻す。
    /// プレハブに付けなくても、<see cref="GetOrAdd"/> で実行時に追加して使える。
    /// </summary>
    public class SlopeWallView : MonoBehaviour
    {
        private const string SlopeObjectName = "EnergyWallSlope";

        /// <summary>
        /// プレビュー中に乗算する透明度
        /// </summary>
        private const float PreviewAlpha = 0.5f;

        /// <summary>
        /// 貫通時、板を通り抜けた先にさらに空ける距離
        /// </summary>
        private const float PassThroughMargin = 0.1f;

        /// <summary>
        /// 浅い角度で当たったときに通り抜け距離が大きくなりすぎないようにする下限
        /// </summary>
        private const float MinPassThroughCos = 0.2f;

        private Renderer[] _originalRenderers;
        private Collider[] _originalColliders;

        private GameObject _slopeObject;
        private MeshFilter _slopeMeshFilter;
        private MeshRenderer _slopeRenderer;
        private MeshCollider _slopeCollider;
        private MaterialPropertyBlock _propertyBlock;

        public bool IsSlope { get; private set; }

        public EnergyWallDirection Direction { get; private set; }

        /// <summary>
        /// 貫通攻撃で斜面の壁を通り抜けたあとの位置を求める。
        /// 斜面は法線が斜めなので、従来の「法線の逆方向へ1マス進む」処理ではなく、進行方向に板の厚さ分だけ進める。
        /// </summary>
        /// <returns>当たった壁が斜面なら true</returns>
        public static bool TryGetPassThroughPosition(RaycastHit hit, Vector3 direction, out Vector3 position)
        {
            position = hit.point;
            if (hit.collider == null) return false;

            var slopeView = hit.collider.GetComponentInParent<SlopeWallView>();
            if (slopeView == null || !slopeView.IsSlope) return false;

            Vector3 flatDirection = new Vector3(direction.x, 0f, direction.z).normalized;

            // 板の端（木口）や浅い角度で当たった場合も確実に抜けられるよう、
            // 板の外側から逆向きにレイを飛ばして実際の出口を求める
            float reach = hit.collider.bounds.size.magnitude + 1f;
            var backRay = new Ray(hit.point + flatDirection * reach, -flatDirection);
            if (hit.collider.Raycast(backRay, out RaycastHit exitHit, reach))
            {
                position = exitHit.point + flatDirection * PassThroughMargin;
                return true;
            }

            // 出口が求まらない場合は、厚さと入射角から通り抜け距離を見積もる
            float cos = Mathf.Max(Mathf.Abs(Vector3.Dot(flatDirection, hit.normal)), MinPassThroughCos);
            position = hit.point + flatDirection * (SlopeWallMeshBuilder.Thickness / cos + PassThroughMargin);
            return true;
        }

        public static SlopeWallView GetOrAdd(BuildingView wall)
        {
            if (wall == null) return null;
            if (!wall.TryGetComponent<SlopeWallView>(out var view))
            {
                view = wall.gameObject.AddComponent<SlopeWallView>();
            }
            return view;
        }

        /// <summary>
        /// 斜面の壁に切り替える。
        /// </summary>
        /// <param name="direction">斜面の向き</param>
        /// <param name="isPreview">true のときは当たり判定を無効にして半透明で表示する（向き選択中のプレビュー）</param>
        public void ApplySlope(EnergyWallDirection direction, bool isPreview)
        {
            EnsureSlopeObject();
            CacheOriginals();

            SetOriginalsEnabled(false);

            var mesh = SlopeWallMeshBuilder.Get(direction);
            _slopeMeshFilter.sharedMesh = mesh;

            // MeshCollider はメッシュを差し替えたら再設定しないと形状が更新されない
            _slopeCollider.sharedMesh = null;
            _slopeCollider.sharedMesh = mesh;
            _slopeCollider.enabled = !isPreview;

            ApplyPreviewColor(isPreview);

            _slopeObject.layer = gameObject.layer;

            // 攻撃ガイドラインは "Wall" タグの当たり判定だけを壁として扱うため、元の当たり判定のタグを引き継ぐ
            _slopeObject.tag = _originalColliders != null && _originalColliders.Length > 0 && _originalColliders[0] != null
                ? _originalColliders[0].gameObject.tag
                : gameObject.tag;
            _slopeObject.SetActive(true);

            Direction = direction;
            IsSlope = true;
        }

        /// <summary>
        /// 元の壁（キューブ）の見た目と当たり判定に戻す。
        /// </summary>
        public void Restore()
        {
            if (_slopeObject != null)
            {
                _slopeObject.SetActive(false);
            }

            if (_originalRenderers != null || _originalColliders != null)
            {
                SetOriginalsEnabled(true);
            }

            IsSlope = false;
        }

        private void EnsureSlopeObject()
        {
            if (_slopeObject != null) return;

            _slopeObject = new GameObject(SlopeObjectName);
            _slopeObject.transform.SetParent(transform, false);
            _slopeObject.transform.localPosition = Vector3.zero;
            _slopeObject.transform.localRotation = Quaternion.identity;
            _slopeObject.transform.localScale = Vector3.one;

            _slopeMeshFilter = _slopeObject.AddComponent<MeshFilter>();
            _slopeRenderer = _slopeObject.AddComponent<MeshRenderer>();
            _slopeCollider = _slopeObject.AddComponent<MeshCollider>();

            // 元の壁と同じマテリアルで描画する
            var originalRenderer = GetComponent<Renderer>();
            if (originalRenderer != null)
            {
                _slopeRenderer.sharedMaterials = originalRenderer.sharedMaterials;
                _slopeRenderer.shadowCastingMode = originalRenderer.shadowCastingMode;
                _slopeRenderer.receiveShadows = originalRenderer.receiveShadows;
            }

            _slopeObject.SetActive(false);
        }

        private void CacheOriginals()
        {
            if (_originalRenderers != null) return;

            // 斜面オブジェクト自身は除外する
            _originalRenderers = System.Array.FindAll(
                GetComponentsInChildren<Renderer>(true),
                r => _slopeObject == null || !r.transform.IsChildOf(_slopeObject.transform));
            _originalColliders = System.Array.FindAll(
                GetComponentsInChildren<Collider>(true),
                c => _slopeObject == null || !c.transform.IsChildOf(_slopeObject.transform));
        }

        private void SetOriginalsEnabled(bool isEnabled)
        {
            if (_originalRenderers != null)
            {
                foreach (var r in _originalRenderers)
                {
                    if (r != null) r.enabled = isEnabled;
                }
            }

            if (_originalColliders != null)
            {
                foreach (var c in _originalColliders)
                {
                    if (c != null) c.enabled = isEnabled;
                }
            }
        }

        private void ApplyPreviewColor(bool isPreview)
        {
            _propertyBlock ??= new MaterialPropertyBlock();
            _slopeRenderer.GetPropertyBlock(_propertyBlock);

            var material = _slopeRenderer.sharedMaterial;
            if (isPreview && material != null && material.HasProperty("_BaseColor"))
            {
                var color = material.GetColor("_BaseColor");
                color.a *= PreviewAlpha;
                _propertyBlock.SetColor("_BaseColor", color);
                _slopeRenderer.SetPropertyBlock(_propertyBlock);
            }
            else
            {
                _slopeRenderer.SetPropertyBlock(null);
            }
        }
    }
}
