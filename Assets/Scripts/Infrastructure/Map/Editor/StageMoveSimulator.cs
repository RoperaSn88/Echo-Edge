#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;

namespace EchoEdge.Infra.Map
{
    /// <summary>
    /// ステージエディタ用に、エネミーフェーズの移動だけを盤面データ上で再現するシミュレーター。
    /// 移動先の決め方は BaseUnit.TryMoveByScoreMap、行動順は MapManager.MoveUnit と同じ。
    /// バフ・ランダムな特殊行動（飛行、壁生成、召喚、吸収など）・一閃による撃破は考慮しない。
    /// </summary>
    public static class StageMoveSimulator
    {
        /// <summary>
        /// シミュレーション対象のユニット（盤面上に立つエネミー）
        /// </summary>
        public readonly struct Unit
        {
            /// <summary>占有する 1 辺あたりのマス数（1x1 なら 1、2x2 なら 2）</summary>
            public readonly int Size;

            /// <summary>1 フェーズの移動回数</summary>
            public readonly int Move;

            public Unit(int size, int move)
            {
                Size = Math.Max(1, size);
                Move = move;
            }
        }

        private const int Empty = -1;
        private const int Wall = -2;

        // 4 方向の移動定義（BaseUnit.TryMoveByScoreMap と同じ）。左移動のみ加点、右移動のみ減点する。
        private static readonly int[] DirH = { 0, -1, 1, 0 };
        private static readonly int[] DirW = { -1, 0, 0, 1 };
        private static readonly int[] DirScore = { 1, 0, 0, -1 };
        // 各方向の逆方向（Uターン判定用）。左⇔右(0⇔3)、上⇔下(1⇔2)。
        private static readonly int[] ReverseDir = { 3, 2, 1, 0 };

        /// <summary>
        /// 1 エネミーフェーズ分の移動を行い、移動後の各ユニットの位置（左下のマス）を返す。
        /// </summary>
        /// <param name="walls">壁（普通の壁・斜面の壁）があるマス</param>
        /// <param name="units">ユニット一覧</param>
        /// <param name="positions">フェーズ開始時の各ユニットの位置（units と同じ並び）</param>
        public static (int h, int w)[] NextPhase(bool[,] walls, IReadOnlyList<Unit> units, IReadOnlyList<(int h, int w)> positions)
        {
            var height = walls.GetLength(0);
            var width = walls.GetLength(1);
            var result = positions.ToArray();

            var grid = new int[height, width];
            for (var h = 0; h < height; h++)
            {
                for (var w = 0; w < width; w++)
                {
                    grid[h, w] = walls[h, w] ? Wall : Empty;
                }
            }
            for (var i = 0; i < units.Count; i++)
            {
                SetFootprint(grid, result[i].h, result[i].w, units[i].Size, i);
            }

            // 行動順は MapManager.GetUnitsInMapOrderSnapshot と同じく、フェーズ開始時の横→縦の昇順
            var order = Enumerable.Range(0, units.Count)
                .OrderBy(i => positions[i].w)
                .ThenBy(i => positions[i].h)
                .ToList();

            foreach (var i in order)
            {
                var src = result[i];
                var dst = FindDestination(grid, i, units[i], src.h, src.w);
                if (dst == src) continue;

                SetFootprint(grid, src.h, src.w, units[i].Size, Empty);
                SetFootprint(grid, dst.h, dst.w, units[i].Size, i);
                result[i] = dst;
            }

            return result;
        }

        /// <summary>
        /// BaseUnit.TryMoveByScoreMap と同じ DP で、そのフェーズの移動先を求める。
        /// </summary>
        private static (int h, int w) FindDestination(int[,] grid, int self, Unit unit, int srcH, int srcW)
        {
            var count = unit.Move;
            if (count <= 0) return (srcH, srcW);

            // 一番左ならなし
            if (srcW <= 0) return (srcH, srcW);

            var height = grid.GetLength(0);
            var width = grid.GetLength(1);
            if (srcH < 0 || srcH >= height || srcW >= width) return (srcH, srcW);

            const int minScore = int.MinValue / 4;
            var scoreByStep = new int[count + 1, height, width];
            var arrivalDir = new int[count + 1, height, width];

            for (var step = 0; step <= count; step++)
            {
                for (var h = 0; h < height; h++)
                {
                    for (var w = 0; w < width; w++)
                    {
                        scoreByStep[step, h, w] = minScore;
                        arrivalDir[step, h, w] = -1;
                    }
                }
            }

            scoreByStep[0, srcH, srcW] = 0;

            for (var step = 1; step <= count; step++)
            {
                for (var h = 0; h < height; h++)
                {
                    for (var w = 0; w < width; w++)
                    {
                        var baseScore = scoreByStep[step - 1, h, w];
                        if (baseScore == minScore) continue;

                        // 2手目以降は直前の移動を打ち消す方向（Uターン）を禁止する
                        var forbiddenDir = -1;
                        if (step >= 2)
                        {
                            var lastDir = arrivalDir[step - 1, h, w];
                            if (lastDir >= 0) forbiddenDir = ReverseDir[lastDir];
                        }

                        for (var dir = 0; dir < DirH.Length; dir++)
                        {
                            if (dir == forbiddenDir) continue;

                            var nextH = h + DirH[dir];
                            var nextW = w + DirW[dir];
                            if (!IsFootprintFree(grid, nextH, nextW, unit.Size, self)) continue;

                            var candidate = baseScore + DirScore[dir];
                            if (candidate <= scoreByStep[step, nextH, nextW]) continue;

                            scoreByStep[step, nextH, nextW] = candidate;
                            arrivalDir[step, nextH, nextW] = dir;
                        }
                    }
                }
            }

            // 全 step の候補から最終目的地を選ぶ。同スコア時は「より左」「縦距離が小さい」位置を優先する。
            var hasDestination = false;
            var dstH = srcH;
            var dstW = srcW;
            var bestScore = minScore;

            for (var step = 1; step <= count; step++)
            {
                for (var h = 0; h < height; h++)
                {
                    for (var w = 0; w < width; w++)
                    {
                        var score = scoreByStep[step, h, w];
                        if (score == minScore) continue;

                        if (!hasDestination || score > bestScore ||
                            (score == bestScore && (w < dstW || (w == dstW && Math.Abs(h - srcH) < Math.Abs(dstH - srcH)))))
                        {
                            hasDestination = true;
                            bestScore = score;
                            dstH = h;
                            dstW = w;
                        }
                    }
                }
            }

            return (dstH, dstW);
        }

        private static bool IsFootprintFree(int[,] grid, int anchorH, int anchorW, int size, int self)
        {
            for (var dh = 0; dh < size; dh++)
            {
                for (var dw = 0; dw < size; dw++)
                {
                    var h = anchorH + dh;
                    var w = anchorW + dw;
                    if (h < 0 || h >= grid.GetLength(0) || w < 0 || w >= grid.GetLength(1)) return false;

                    var occupant = grid[h, w];
                    if (occupant != Empty && occupant != self) return false;
                }
            }
            return true;
        }

        private static void SetFootprint(int[,] grid, int anchorH, int anchorW, int size, int value)
        {
            for (var dh = 0; dh < size; dh++)
            {
                for (var dw = 0; dw < size; dw++)
                {
                    var h = anchorH + dh;
                    var w = anchorW + dw;
                    if (h >= 0 && h < grid.GetLength(0) && w >= 0 && w < grid.GetLength(1))
                    {
                        grid[h, w] = value;
                    }
                }
            }
        }
    }
}
#endif
