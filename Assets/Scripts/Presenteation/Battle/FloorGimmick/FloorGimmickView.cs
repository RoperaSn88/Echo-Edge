using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace EchoEdge.Presenter.Battle
{
    /// <summary>
    /// 床ギミック（ワープホールなど）1マス分の見た目と当たり判定。
    /// 一閃の経路上にあるかどうかは、子も含めたコライダーへのレイキャスト（<see cref="TryRaycast"/>）で判定する。
    /// 使用済みになると半透明にし、次のプレイヤーフェーズの開始で元に戻す（<see cref="FloorGimmickManager.ResetUsed"/>）。
    /// </summary>
    public abstract class FloorGimmickView : MonoBehaviour
    {
        /// <summary>
        /// 壁判定・ポインター判定のレイキャストに当たらないようにするレイヤー（Ignore Raycast）
        /// </summary>
        private const int IgnoreRaycastLayer = 2;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        
        private const float UsedAlpha = 0.3f;

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

        private Collider[] _colliders;
        private Renderer[] _renderers;
        private readonly Dictionary<SpriteRenderer, Color> _spriteColors = new();
        private MaterialPropertyBlock _propertyBlock;

        /// <summary>
        /// 指定マスに置く。コライダーはトリガーにし、壁判定のレイキャストに当たらないよう Ignore Raycast レイヤーにする。
        /// コライダーが1つも無ければ、1マス分の箱のコライダーを付ける。
        /// </summary>
        public void Setup(int h, int w, Vector3 cellCenter)
        {
            Height = h;
            Width = w;
            CellCenter = cellCenter;
            transform.position = cellCenter;

            _colliders = GetComponentsInChildren<Collider>(true);
            if (_colliders.Length == 0)
            {
                var box = gameObject.AddComponent<BoxCollider>();
                box.size = Vector3.one;
                box.center = new Vector3(0f, 0.5f, 0f);
                _colliders = new Collider[] { box };
            }

            foreach (var col in _colliders)
            {
                col.isTrigger = true;
                col.gameObject.layer = IgnoreRaycastLayer;
            }

            _renderers = GetComponentsInChildren<Renderer>(true);
            _spriteColors.Clear();
            foreach (var renderer in _renderers)
            {
                if (renderer is SpriteRenderer spriteRenderer) _spriteColors[spriteRenderer] = spriteRenderer.color;
            }
            CacheRendererColors();

            SetUsed(false);
        }

        /// <summary>
        /// 使用済みにする（半透明にする）／元に戻す
        /// </summary>
        public void SetUsed(bool used)
        {
            IsUsed = used;
            ApplyAlpha(used ? UsedAlpha : 1f);
        }

        /// <summary>
        /// 一閃（origin から direction へ maxDistance まで）がこのマスのコライダーに入るかを調べ、入るまでの距離を返す。
        /// 高さはコライダーごとの中心の高さで調べるので、コライダーの高さとプレイヤーの高さがずれていてもよい。
        /// </summary>
        public bool TryRaycast(Vector3 origin, Vector3 direction, float maxDistance, out float distance)
        {
            distance = float.MaxValue;
            if (_colliders == null) return false;

            var found = false;
            foreach (var col in _colliders)
            {
                if (col == null || !col.enabled || !col.gameObject.activeInHierarchy) continue;

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

        /// <summary>
        /// Renderer ごとの元の色（未使用のときの色）。プロパティブロックで色を上書きしていればその色を使う
        /// </summary>
        private readonly Dictionary<Renderer, (bool hasBase, Color baseColor, bool hasColor, Color color)> _rendererColors = new();

        private void CacheRendererColors()
        {
            _rendererColors.Clear();
            _propertyBlock ??= new MaterialPropertyBlock();
            foreach (var renderer in _renderers)
            {
                if (renderer == null || renderer is SpriteRenderer) continue;
                var material = renderer.sharedMaterial;
                if (material == null) continue;

                renderer.GetPropertyBlock(_propertyBlock);
                var hasBase = material.HasProperty(BaseColorId);
                var hasColor = material.HasProperty(ColorId);
                var baseColor = hasBase ? GetColor(material, BaseColorId) : default;
                var color = hasColor ? GetColor(material, ColorId) : default;
                _rendererColors[renderer] = (hasBase, baseColor, hasColor, color);
            }

            Color GetColor(Material material, int id)
            {
                var blockColor = _propertyBlock.GetColor(id);
                return _propertyBlock.HasColor(id) ? blockColor : material.GetColor(id);
            }
        }

        private void ApplyAlpha(float alpha)
        {
            if (_renderers != null)
            {
                _propertyBlock ??= new MaterialPropertyBlock();
                foreach (var renderer in _renderers)
                {
                    if (renderer == null) continue;

                    if (renderer is SpriteRenderer spriteRenderer)
                    {
                        if (_spriteColors.TryGetValue(spriteRenderer, out var spriteColor))
                        {
                            spriteColor.a *= alpha;
                            spriteRenderer.color = spriteColor;
                        }
                        continue;
                    }

                    if (!_rendererColors.TryGetValue(renderer, out var original)) continue;

                    renderer.GetPropertyBlock(_propertyBlock);
                    if (original.hasBase)
                    {
                        var baseColor = original.baseColor;
                        baseColor.a *= alpha;
                        _propertyBlock.SetColor(BaseColorId, baseColor);
                    }
                    if (original.hasColor)
                    {
                        var color = original.color;
                        color.a *= alpha;
                        _propertyBlock.SetColor(ColorId, color);
                    }
                    renderer.SetPropertyBlock(_propertyBlock);
                }
            }
        }
    }
}
