#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using EchoEdge.Domain.Battle;

namespace EchoEdge.Infra.Map
{
    /// <summary>
    /// ステージエディタ用に、エネミーフェーズの移動だけを盤面データ上で再現するシミュレーター。
    /// 移動先の決め方は EnemyMovePlanner（BaseUnit.TryMoveByScoreMap と共通）、行動順は MapManager.MoveUnit と同じ。
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
        /// 実際のゲーム（BaseUnit.TryMoveByScoreMap）と同じ EnemyMovePlanner で、そのフェーズの移動先を求める。
        /// 同じ近さの候補が複数あるときは、ゲームではランダムに選ぶが、プレビューでは最初に見つかった候補にする。
        /// </summary>
        private static (int h, int w) FindDestination(int[,] grid, int self, Unit unit, int srcH, int srcW)
        {
            var path = EnemyMovePlanner.PlanPath(new GridMoveBoard(grid, self), unit.Size, srcH, srcW, unit.Move);
            return path.Count > 0 ? path[path.Count - 1] : (srcH, srcW);
        }

        /// <summary>
        /// シミュレーター用の盤面を EnemyMovePlanner に渡すためのアダプター
        /// </summary>
        private sealed class GridMoveBoard : IEnemyMoveBoard
        {
            private readonly int[,] _grid;
            private readonly int _self;

            public GridMoveBoard(int[,] grid, int self)
            {
                _grid = grid;
                _self = self;
            }

            public int Height => _grid.GetLength(0);
            public int Width => _grid.GetLength(1);

            public bool IsWall(int h, int w) => _grid[h, w] == Wall;

            public bool IsFootprintFree(int anchorH, int anchorW, int span) =>
                StageMoveSimulator.IsFootprintFree(_grid, anchorH, anchorW, span, _self);
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
