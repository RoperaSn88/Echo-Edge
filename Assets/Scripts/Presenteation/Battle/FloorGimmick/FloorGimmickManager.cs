using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

using EchoEdge.Domain.Map;

namespace EchoEdge.Presenter.Battle
{
    /// <summary>
    /// 床ギミック（ワープホールなど）をステージ CSV の配置どおりに置き、一閃の経路上にあるものを探す。
    /// MainGame シーンに1つ置いてプレハブを割り当てる（置かなければ初回のステージ構築時に自動生成し、仮の見た目で置く）。
    /// </summary>
    public class FloorGimmickManager : MonoBehaviour
    {
        public static FloorGimmickManager Instance { get; private set; }

        /// <summary>
        /// シーンに置いたものを返す。置いていなければ <paramref name="scene"/>（盤面のあるシーン）に作る。
        /// 盤面のシーンに作るのは、シーンを閉じたときに一緒に消えるようにするため。
        /// </summary>
        public static FloorGimmickManager GetOrCreate(Scene scene)
        {
            if (Instance != null) return Instance;

            Instance = FindFirstObjectByType<FloorGimmickManager>();
            if (Instance != null) return Instance;

            var go = new GameObject(nameof(FloorGimmickManager));
            if (scene.IsValid() && scene.isLoaded) SceneManager.MoveGameObjectToScene(go, scene);
            return go.AddComponent<FloorGimmickManager>();
        }

        [Tooltip("ワープホールのプレハブ（ルートに WarpHoleView を付ける）。ピボットをマスの中心・床の高さに合わせる。" +
                 "当たり判定のコライダーは子に付けてもよい（無ければ1マス分の箱を自動で付ける）。未設定なら仮の円盤を置く")]
        [SerializeField]
        private WarpHoleView _warpHolePrefab;

        [Tooltip("床からの高さ。床と重なってチラつく場合は少し上げる")]
        [SerializeField]
        private float _heightOffset = 0.01f;

        [Tooltip("プレハブ未設定のときの仮のワープホールの色")]
        [SerializeField]
        private Color _placeholderWarpHoleColor = new Color(0.6f, 0.3f, 1f, 1f);

        private readonly List<FloorGimmickView> _gimmicks = new();

        /// <summary>
        /// 置いてある床ギミック
        /// </summary>
        public IReadOnlyList<FloorGimmickView> Gimmicks => _gimmicks;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// 置いてある床ギミックを片付けてから、配置データどおりに置き直す（ウェーブの読み込みごとに呼ぶ）
        /// </summary>
        /// <param name="placements">床ギミックの配置</param>
        /// <param name="basePos">盤面の基準位置（<c>MapManager.GetBasePos</c>）。マス (h, w) の中心は basePos + (w, 0, h)</param>
        public void Build(IEnumerable<StagePlacementData> placements, Vector3 basePos)
        {
            Clear();

            var warpHoles = new List<WarpHoleView>();
            var pairIds = new Dictionary<WarpHoleView, int>();
            foreach (var placement in placements)
            {
                var center = basePos + new Vector3(placement.width, _heightOffset, placement.height);
                switch (placement.objectKind)
                {
                    case StageObjectKind.WarpHole:
                        var warpHole = CreateWarpHole();
                        warpHole.name = $"WarpHole_{placement.pairId}_({placement.height},{placement.width})";
                        warpHole.Setup(placement.height, placement.width, center);
                        warpHoles.Add(warpHole);
                        pairIds[warpHole] = placement.pairId;
                        _gimmicks.Add(warpHole);
                        break;
                }
            }

            // 同じペア番号のワープホールどうしを対にする。ちょうど2つでない番号は対にしない（入っても何も起きない）
            foreach (var group in warpHoles.GroupBy(w => pairIds[w]))
            {
                var members = group.ToList();
                if (members.Count != 2)
                {
                    Debug.LogWarning($"ワープホールのペア番号 {group.Key} が {members.Count} 個あります。同じ番号はちょうど2つにしてください（このペアは使えません）");
                    foreach (var member in members) member.SetPair(group.Key, null);
                    continue;
                }

                members[0].SetPair(group.Key, members[1]);
                members[1].SetPair(group.Key, members[0]);
            }
        }

        /// <summary>
        /// 置いてある床ギミックをすべて消す
        /// </summary>
        public void Clear()
        {
            foreach (var gimmick in _gimmicks)
            {
                if (gimmick != null) Destroy(gimmick.gameObject);
            }
            _gimmicks.Clear();
        }

        /// <summary>
        /// すべての床ギミックを未使用に戻す（プレイヤーフェーズの開始時に呼ぶ）
        /// </summary>
        public void ResetUsed()
        {
            foreach (var gimmick in _gimmicks)
            {
                if (gimmick != null) gimmick.SetUsed(false);
            }
        }

        /// <summary>
        /// シーンにマネージャーがあれば、すべての床ギミックを未使用に戻す
        /// </summary>
        public static void ResetUsedIfExists()
        {
            if (Instance != null) Instance.ResetUsed();
        }

        /// <summary>
        /// 一閃（origin から direction へ maxDistance まで）の経路上で最初に入る、使える床ギミックを探す。
        /// 使用済み・<paramref name="ignore"/> に含まれるもの・対の無いワープホールは飛ばす。
        /// </summary>
        /// <param name="origin">一閃の始点</param>
        /// <param name="direction">一閃の向き（正規化済み）</param>
        /// <param name="maxDistance">調べる距離（ふつうは次の壁までの距離）</param>
        /// <param name="ignore">使用済みとして扱うもの（予測線で、なぞった経路で使ったもの）。null 可</param>
        /// <param name="gimmick">見つかった床ギミック</param>
        /// <param name="distance">始点から床ギミックに入るまでの距離</param>
        public static bool TryFindNext(Vector3 origin, Vector3 direction, float maxDistance, ICollection<FloorGimmickView> ignore,
            out FloorGimmickView gimmick, out float distance)
        {
            gimmick = null;
            distance = float.MaxValue;
            if (Instance == null) return false;

            foreach (var candidate in Instance._gimmicks)
            {
                if (candidate == null || candidate.IsUsed) continue;
                if (ignore != null && ignore.Contains(candidate)) continue;
                if (candidate is WarpHoleView { Partner: null }) continue;

                if (candidate.TryRaycast(origin, direction, maxDistance, out var d) && d < distance)
                {
                    gimmick = candidate;
                    distance = d;
                }
            }
            return gimmick != null;
        }

        private WarpHoleView CreateWarpHole()
        {
            if (_warpHolePrefab != null)
            {
                return Instantiate(_warpHolePrefab, transform);
            }

            // プレハブ未設定の間の仮の見た目（平たい円盤）
            var root = new GameObject("WarpHole");
            root.transform.SetParent(transform, false);
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "Placeholder";
            DestroyImmediate(disc.GetComponent<Collider>());
            disc.transform.SetParent(root.transform, false);
            disc.transform.localScale = new Vector3(0.8f, 0.01f, 0.8f);
            var renderer = disc.GetComponent<Renderer>();
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", _placeholderWarpHoleColor);
            block.SetColor("_Color", _placeholderWarpHoleColor);
            renderer.SetPropertyBlock(block);
            return root.AddComponent<WarpHoleView>();
        }
    }
}
