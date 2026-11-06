using System.Collections.Generic;
using UnityEngine;

using EchoEdge.App.Battle;

namespace EchoEdge.Presenter.Battle
{
    /// <summary>
    /// マグネティックコアの使用マスを選んでいる間、効果範囲のマスを床に色付きのタイルで表示する。
    /// MainGame シーンに1つ置いて見た目を調整する（置かなければ初回表示時に既定の見た目で自動生成する）。
    /// タイルの見た目は <see cref="_tilePrefab"/> で差し替えられる。未設定なら白い四角のスプライトを自動で作って使う。
    /// </summary>
    public class MagneticCoreRangeView : MonoBehaviour
    {
        /// <summary>
        /// タイルの種類
        /// </summary>
        public enum TileKind
        {
            /// <summary>効果範囲のマス</summary>
            Range,
            /// <summary>引き寄せられる敵がいるマス</summary>
            Target,
            /// <summary>使用するマス（中心）</summary>
            Center,
        }

        public static MagneticCoreRangeView Instance { get; private set; }

        /// <summary>
        /// シーンに置いたものを返す。置いていなければ既定の見た目で作る
        /// </summary>
        public static MagneticCoreRangeView GetOrCreate()
        {
            if (Instance != null) return Instance;

            Instance = FindFirstObjectByType<MagneticCoreRangeView>();
            if (Instance != null) return Instance;

            return new GameObject(nameof(MagneticCoreRangeView)).AddComponent<MagneticCoreRangeView>();
        }

        [Header("見た目")]
        [Tooltip("1マス分のタイル（任意）。SpriteRenderer があれば color、無ければ Renderer のマテリアルの _BaseColor / _Color に下の色を入れる。" +
                 "未設定なら白い四角のスプライトを自動で作る。タイルは床に寝かせた向き（X 90°）で置くので、プレハブは XY 平面で作る")]
        [SerializeField]
        private GameObject _tilePrefab;

        [Tooltip("効果範囲のマスの色")]
        [SerializeField]
        private Color _rangeColor = new Color(0.243f, 0.467f, 0.867f, 0.35f);

        [Tooltip("引き寄せられる敵がいるマスの色")]
        [SerializeField]
        private Color _targetColor = new Color(1f, 0.55f, 0.15f, 0.6f);

        [Tooltip("使用するマス（中心）の色")]
        [SerializeField]
        private Color _centerColor = new Color(0.45f, 0.75f, 1f, 0.75f);

        [Tooltip("床からの高さ。床と重なってチラつく場合は少し上げる")]
        [SerializeField]
        private float _heightOffset = 0.02f;

        [Tooltip("タイルの大きさ（1 で1マスぴったり）。少し小さくするとマスの境目が見える")]
        [SerializeField, Range(0.1f, 1f)]
        private float _tileScale = 0.92f;

        [Tooltip("SpriteRenderer の Sorting Order。敵やエナジーウォール（-3）より奥に描きたい場合は小さくする")]
        [SerializeField]
        private int _sortingOrder = -4;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private readonly List<GameObject> _tiles = new();
        private int _activeTileCount;
        private MaterialPropertyBlock _propertyBlock;
        private Sprite _fallbackSprite;
        private Texture2D _fallbackTexture;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            if (_fallbackSprite != null) Destroy(_fallbackSprite);
            if (_fallbackTexture != null) Destroy(_fallbackTexture);
        }

        /// <summary>
        /// 範囲を表示する。前回の表示は消してから出し直す。
        /// 同じマスが複数の種類に入っている場合は 中心 ＞ 敵 ＞ 範囲 の順に優先する
        /// </summary>
        /// <param name="center">使用するマス（x = w, y = h）</param>
        /// <param name="rangeCells">効果範囲のマス（x = w, y = h）</param>
        /// <param name="targetCells">引き寄せられる敵がいるマス（x = w, y = h）</param>
        public void Show(Vector2Int center, IEnumerable<Vector2Int> rangeCells, IEnumerable<Vector2Int> targetCells)
        {
            Hide();
            if (MapManager.Instance == null) return;

            Dictionary<Vector2Int, TileKind> cells = new();
            foreach (var cell in rangeCells)
            {
                cells[cell] = TileKind.Range;
            }
            foreach (var cell in targetCells)
            {
                cells[cell] = TileKind.Target;
            }
            cells[center] = TileKind.Center;

            Vector3 basePos = MapManager.Instance.GetBasePos();
            foreach (var pair in cells)
            {
                if (!MapManager.Instance.IsInBounds(pair.Key.y, pair.Key.x)) continue;

                var tile = GetTile();
                tile.transform.SetPositionAndRotation(
                    basePos + new Vector3(pair.Key.x, _heightOffset, pair.Key.y),
                    Quaternion.Euler(90f, 0f, 0f));
                tile.transform.localScale = Vector3.one * _tileScale;
                ApplyColor(tile, GetColor(pair.Value));
                tile.SetActive(true);
            }
        }

        /// <summary>
        /// 範囲の表示を消す
        /// </summary>
        public void Hide()
        {
            for (int i = 0; i < _activeTileCount; i++)
            {
                if (_tiles[i] != null) _tiles[i].SetActive(false);
            }
            _activeTileCount = 0;
        }

        private Color GetColor(TileKind kind)
        {
            return kind switch
            {
                TileKind.Center => _centerColor,
                TileKind.Target => _targetColor,
                _ => _rangeColor,
            };
        }

        private GameObject GetTile()
        {
            if (_activeTileCount < _tiles.Count && _tiles[_activeTileCount] != null)
            {
                return _tiles[_activeTileCount++];
            }

            var tile = CreateTile();
            if (_activeTileCount < _tiles.Count)
            {
                _tiles[_activeTileCount] = tile;
            }
            else
            {
                _tiles.Add(tile);
            }
            _activeTileCount++;
            return tile;
        }

        private GameObject CreateTile()
        {
            GameObject tile;
            if (_tilePrefab != null)
            {
                tile = Instantiate(_tilePrefab, transform);
            }
            else
            {
                tile = new GameObject("MagneticCoreRangeTile");
                tile.transform.SetParent(transform, false);
                var spriteRenderer = tile.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = GetFallbackSprite();
            }

            foreach (var spriteRenderer in tile.GetComponentsInChildren<SpriteRenderer>(true))
            {
                spriteRenderer.sortingOrder = _sortingOrder;
            }

            tile.SetActive(false);
            return tile;
        }

        private void ApplyColor(GameObject tile, Color color)
        {
            var spriteRenderers = tile.GetComponentsInChildren<SpriteRenderer>(true);
            if (spriteRenderers.Length > 0)
            {
                foreach (var spriteRenderer in spriteRenderers)
                {
                    spriteRenderer.color = color;
                }
                return;
            }

            _propertyBlock ??= new MaterialPropertyBlock();
            foreach (var tileRenderer in tile.GetComponentsInChildren<Renderer>(true))
            {
                tileRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(BaseColorId, color);
                _propertyBlock.SetColor(ColorId, color);
                tileRenderer.SetPropertyBlock(_propertyBlock);
            }
        }

        /// <summary>
        /// 1マス（1ユニット）ぴったりの白い四角のスプライト
        /// </summary>
        private Sprite GetFallbackSprite()
        {
            if (_fallbackSprite != null) return _fallbackSprite;

            const int size = 4;
            _fallbackTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(255, 255, 255, 255);
            }
            _fallbackTexture.SetPixels32(pixels);
            _fallbackTexture.Apply();

            _fallbackSprite = Sprite.Create(_fallbackTexture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return _fallbackSprite;
        }
    }
}
