using UnityEngine;

namespace EchoEdge.Presenter.Battle
{
    /// <summary>
    /// 床ギミック（ワープホールなど）1マス分の見た目と当たり判定。
    /// 一閃の経路上にあるかどうかは、<see cref="_colliders"/> へのレイキャスト（<see cref="TryRaycast"/>）で判定する。
    /// 使用済みになると半透明にし、次のプレイヤーフェーズの開始で元に戻す（<see cref="FloorGimmickManager.ResetUsed"/>）。
    /// 半透明は、<see cref="_material"/> をこのギミック専用に複製して <see cref="_renderers"/> に割り当て、その色の α を変えて行う。
    /// </summary>
    public abstract class FloorGimmickView : MonoBehaviour
    {
        /// <summary>
        /// 壁判定・ポインター判定のレイキャストに当たらないようにするレイヤー（Ignore Raycast）
        /// </summary>
        private const int IgnoreRaycastLayer = 2;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("一閃が入ったかを判定するコライダー。実行時に Is Trigger をオン・レイヤーを Ignore Raycast にする")]
        [SerializeField]
        private Collider[] _colliders;

        [Tooltip("見た目のマテリアル。ギミックごとに複製して下の Renderer に割り当て、使用済みのときにその _BaseColor の α を下げる。" +
                 "Surface Type を Transparent にしておく（Opaque だと透けない）")]
        [SerializeField]
        private Material _material;

        [Tooltip("複製したマテリアルを割り当てる Renderer（パーティクルなら ParticleSystemRenderer）")]
        [SerializeField]
        private Renderer[] _renderers;

        [Tooltip("使用済みのときの不透明度（0 で見えない、1 で変化なし）")]
        [SerializeField, Range(0f, 1f)]
        private float _usedAlpha = 0.3f;

        /// <summary>
        /// 配置したマス（縦）
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        /// 配置したマス（横）
        /// </summary>
        public int Width { get; private set; }

        /// <summary>
        /// マスの中心のワールド座標（y は床の高さ）
        /// </summary>
        public Vector3 CellCenter { get; private set; }

        /// <summary>
        /// このターンに使ったかどうか。使用済みの間は一閃が上を通っても何も起きない
        /// </summary>
        public bool IsUsed { get; private set; }

        /// <summary>
        /// このギミック専用に複製したマテリアル
        /// </summary>
        private Material _instanceMaterial;

        /// <summary>
        /// 複製したマテリアルの元の色（未使用のときの色）
        /// </summary>
        private Color _baseColor;

        /// <summary>
        /// 指定マスに置く。マテリアルを複製して Renderer に割り当て、コライダーをトリガー・Ignore Raycast レイヤーにする。
        /// </summary>
        public void Setup(int h, int w, Vector3 cellCenter)
        {
            Height = h;
            Width = w;
            CellCenter = cellCenter;
            transform.position = cellCenter;

            foreach (var col in _colliders)
            {
                col.isTrigger = true;
                col.gameObject.layer = IgnoreRaycastLayer;
            }

            if (_material != null)
            {
                _instanceMaterial = new Material(_material);
                _baseColor = _instanceMaterial.GetColor(BaseColorId);
                foreach (var target in _renderers)
                {
                    target.sharedMaterial = _instanceMaterial;
                }
            }

            SetUsed(false);
        }

        private void OnDestroy()
        {
            if (_instanceMaterial != null) Destroy(_instanceMaterial);
        }

        /// <summary>
        /// 使用済みにする（半透明にする）／元に戻す
        /// </summary>
        public void SetUsed(bool used)
        {
            IsUsed = used;
            if (_instanceMaterial == null) return;

            var color = _baseColor;
            color.a *= used ? _usedAlpha : 1f;
            _instanceMaterial.SetColor(BaseColorId, color);
        }

        /// <summary>
        /// 一閃（origin から direction へ maxDistance まで）がこのマスのコライダーに入るかを調べ、入るまでの距離を返す。
        /// 高さはコライダーごとの中心の高さで調べるので、コライダーの高さとプレイヤーの高さがずれていてもよい。
        /// </summary>
        public bool TryRaycast(Vector3 origin, Vector3 direction, float maxDistance, out float distance)
        {
            distance = float.MaxValue;

            var found = false;
            foreach (var col in _colliders)
            {
                if (!col.enabled || !col.gameObject.activeInHierarchy) continue;

                var rayOrigin = origin;
                rayOrigin.y = col.bounds.center.y;
                if (col.Raycast(new Ray(rayOrigin, direction), out var hit, maxDistance) && hit.distance < distance)
                {
                    distance = hit.distance;
                    found = true;
                }
            }
            return found;
        }
    }
}
