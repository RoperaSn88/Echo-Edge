using UnityEngine;
using UnityEngine.UI;

namespace EchoEdge.Presenter.UI
{
    /// <summary>
    /// Graphic を円形に [Min, Max] の区間だけ表示する
    /// マテリアルに EchoEdge/UI/RadialRangeFill シェーダーを使うこと
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public class RadialFillRange : BaseMeshEffect
    {
        [SerializeField, Range(0f, 1f), Tooltip("表示を開始する位置 (1 = 360度)")]
        private float _min = 0f;

        [SerializeField, Range(0f, 1f), Tooltip("表示を終了する位置 (1 = 360度)")]
        private float _max = 1f;

        [SerializeField, Range(0f, 360f), Tooltip("0 の位置。真上から時計回りの角度")]
        private float _originAngle = 0f;

        [SerializeField, Tooltip("時計回りに進むか")]
        private bool _clockwise = true;

        [SerializeField, Tooltip("回転の中心。矩形内の正規化座標")]
        private Vector2 _center = new Vector2(0.5f, 0.5f);

        public float Min { get => _min; set => SetRange(value, _max); }
        public float Max { get => _max; set => SetRange(_min, value); }

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
        /// 表示区間を設定する
        /// </summary>
        public void SetRange(float min, float max)
        {
            _min = Mathf.Clamp01(min);
            _max = Mathf.Clamp01(max);
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
            var size = new Vector2(Mathf.Max(rect.width, 1e-5f), Mathf.Max(rect.height, 1e-5f));
            var center = rect.min + Vector2.Scale(rect.size, _center);

            // シェーダー側は「真上から時計回り」で判定するので、開始位置と長さに変換して渡す
            var lo = Mathf.Min(_min, _max);
            var hi = Mathf.Max(_min, _max);
            var origin = _originAngle / 360f;
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
