using UnityEngine;
using UnityEngine.UI;

namespace EchoEdge.Presenter.UI
{
    /// <summary>
    /// Graphic を円形に [MinAngle, MaxAngle] の区間だけ表示する
    /// 角度はすべて度数法。OriginAngle を 0 度として、Clockwise の向きに進む
    /// マテリアルに EchoEdge/UI/RadialRangeFill シェーダーを使うこと
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public class RadialFillRange : BaseMeshEffect
    {
        private const float FullAngle = 360f;

        [SerializeField, Range(0f, FullAngle), Tooltip("表示を開始する角度 (度)")]
        private float _minAngle = 0f;

        [SerializeField, Range(0f, FullAngle), Tooltip("表示を終了する角度 (度)")]
        private float _maxAngle = FullAngle;

        [SerializeField, Range(0f, FullAngle), Tooltip("0 度の位置。真上から時計回りの角度")]
        private float _originAngle = 0f;

        [SerializeField, Tooltip("時計回りに進むか")]
        private bool _clockwise = true;

        [SerializeField, Tooltip("回転の中心。矩形内の正規化座標")]
        private Vector2 _center = new Vector2(0.5f, 0.5f);

        /// <summary>表示を開始する角度 (度)</summary>
        public float MinAngle { get => _minAngle; set => SetRange(value, _maxAngle); }

        /// <summary>表示を終了する角度 (度)</summary>
        public float MaxAngle { get => _maxAngle; set => SetRange(_minAngle, value); }

        public float OriginAngle
        {
            get => _originAngle;
            set { _originAngle = value; SetDirty(); }
        }

        public bool Clockwise
        {
            get => _clockwise;
            set { _clockwise = value; SetDirty(); }
        }

        /// <summary>
        /// 表示区間を角度 (度) で設定する
        /// </summary>
        public void SetRange(float minAngle, float maxAngle)
        {
            _minAngle = Mathf.Clamp(minAngle, 0f, FullAngle);
            _maxAngle = Mathf.Clamp(maxAngle, 0f, FullAngle);
            SetDirty();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EnableTexCoord1();
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive()) return;

            EnableTexCoord1();

            var rect = graphic.rectTransform.rect;
            // 縦横を同じ値で正規化する。縦横別々に割ると、横長の矩形 (半円の分度器など) で角度が歪む
            var size = Mathf.Max(rect.width, rect.height, 1e-5f);
            var center = rect.min + Vector2.Scale(rect.size, _center);

            // シェーダー側は「真上から時計回り」で判定するので、開始位置と長さに変換して渡す
            // シェーダーは 1 = 360 度の割合で受け取る
            var lo = Mathf.Min(_minAngle, _maxAngle) / FullAngle;
            var hi = Mathf.Max(_minAngle, _maxAngle) / FullAngle;
            var origin = _originAngle / FullAngle;
            var start = _clockwise ? origin + lo : origin - hi;
            var length = hi - lo;

            var v = new UIVertex();
            for (var i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref v, i);
                var local = ((Vector2)v.position - center) / size;
                v.uv1 = new Vector4(local.x, local.y, start - Mathf.Floor(start), length);
                vh.SetUIVertex(v, i);
            }
        }

        private void SetDirty()
        {
            if (graphic != null) graphic.SetVerticesDirty();
        }

        // 区間情報を TEXCOORD1 で渡すため、Canvas のシェーダーチャンネルを有効にする
        private void EnableTexCoord1()
        {
            var canvas = graphic != null ? graphic.canvas : null;
            if (canvas == null) return;
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
        }
    }
}
