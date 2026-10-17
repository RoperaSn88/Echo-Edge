using UnityEngine;

using EchoEdge.Domain.Battle;

namespace EchoEdge.Presenter.Battle
{
    /// <summary>
    /// 斜面のエナジーウォールのプレハブに付けるクラス。
    /// 子の板（Body）を Y 軸で ±45° 回して向き（／・＼）を表し、向き選択中のプレビュー表示も切り替える。
    /// </summary>
    [RequireComponent(typeof(BuildingView))]
    public class SlopeWallView : MonoBehaviour
    {
        /// <summary>
        /// ／ 向きのときの板の Y 軸回転。板の長辺（ローカル X 軸）がマスの左下から右上へ伸びる
        /// </summary>
        private const float SlashYaw = -45f;

        /// <summary>
        /// ＼ 向きのときの板の Y 軸回転
        /// </summary>
        private const float BackSlashYaw = 45f;

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

        /// <summary>
        /// 出口が求まらなかったときに使う、板の厚さの見積もり
        /// </summary>
        private const float FallbackThickness = 0.12f;

        /// <summary>
        /// 回転させる板（薄い Cube）。Renderer と Collider を持つ
        /// </summary>
        [SerializeField]
        private Transform _body;

        [SerializeField]
        private Renderer _bodyRenderer;

        [SerializeField]
        private Collider _bodyCollider;

        private BuildingView _buildingView;
        private MaterialPropertyBlock _propertyBlock;

        public BuildingView BuildingView => _buildingView ??= GetComponent<BuildingView>();

        public EnergyWallDirection Direction { get; private set; }

        /// <summary>
        /// 貫通攻撃で斜面の壁を通り抜けたあとの位置を求める。
        /// 斜面は法線が斜めなので、従来の「法線の逆方向へ1マス進む」処理ではなく、進行方向に沿って板の出口まで進める。
        /// </summary>
        /// <returns>当たった壁が斜面なら true</returns>
        public static bool TryGetPassThroughPosition(RaycastHit hit, Vector3 direction, out Vector3 position)
        {
            position = hit.point;
            if (hit.collider == null) return false;

            var slopeView = hit.collider.GetComponentInParent<SlopeWallView>();
            if (slopeView == null) return false;

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
            position = hit.point + flatDirection * (FallbackThickness / cos + PassThroughMargin);
            return true;
        }

        /// <summary>
        /// 斜面の向きと、プレビューかどうかを設定する。
        /// </summary>
        /// <param name="direction">斜面の向き</param>
        /// <param name="isPreview">true のときは当たり判定を無効にして半透明で表示する（向き選択中のプレビュー）</param>
        public void Apply(EnergyWallDirection direction, bool isPreview)
        {
            if (_body == null)
            {
                Debug.LogError($"{nameof(SlopeWallView)}: Body が未設定です。");
                return;
            }

            float yaw = direction == EnergyWallDirection.Slash ? SlashYaw : BackSlashYaw;
            _body.localRotation = Quaternion.Euler(0f, yaw, 0f);

            if (_bodyCollider != null)
            {
                _bodyCollider.enabled = !isPreview;
            }

            ApplyPreviewColor(isPreview);
            Direction = direction;
        }

        private void ApplyPreviewColor(bool isPreview)
        {
            if (_bodyRenderer == null) return;

            _propertyBlock ??= new MaterialPropertyBlock();

            var material = _bodyRenderer.sharedMaterial;
            if (isPreview && material != null && material.HasProperty("_BaseColor"))
            {
                _bodyRenderer.GetPropertyBlock(_propertyBlock);
                var color = material.GetColor("_BaseColor");
                color.a *= PreviewAlpha;
                _propertyBlock.SetColor("_BaseColor", color);
                _bodyRenderer.SetPropertyBlock(_propertyBlock);
            }
            else
            {
                _bodyRenderer.SetPropertyBlock(null);
            }
        }
    }
}
