using System;
using System.Collections.Generic;

namespace EchoEdge.Domain.Battle
{
    /// <summary>
    /// エネミーの移動先を決めるために、盤面から読み取る情報。
    /// 実際のゲーム（MapManager）とステージエディタのシミュレーターの両方から同じアルゴリズムを使うための窓口。
    /// </summary>
    public interface IEnemyMoveBoard
    {
        /// <summary>盤面の縦マス数</summary>
        int Height { get; }

        /// <summary>盤面の横マス数</summary>
        int Width { get; }

        /// <summary>指定マスに壁（building）があるか。盤面外は呼ばれない。</summary>
        bool IsWall(int h, int w);

        /// <summary>
        /// anchorH/anchorW を基準マスとする span マス四方が、すべて盤面内かつ空いているか。
        /// 移動するユニット自身が占有しているマスは空きとして扱うこと。
        /// </summary>
        bool IsFootprintFree(int anchorH, int anchorW, int span);
    }

    /// <summary>
    /// エネミーの1フェーズ分の移動経路を決めるアルゴリズム。
    /// 目的地（左端）までの距離を測り、Move 手以内で「今より近づけるマス」へ移動する。近づけないならその場に留まる。
    /// 距離は次の順に測り、最初に近づけるものを使う。
    /// <list type="number">
    /// <item>今の盤面（壁と他のユニット）を障害物にした距離。前のユニットを迂回すれば進めるなら、迂回路を進む。</item>
    /// <item>壁だけを障害物にした距離。他のユニットで道が完全に塞がれていても、塞がれている手前までは進む。</item>
    /// <item>横方向の位置（壁に囲まれて左端へ行けない場合）。</item>
    /// </list>
    /// 「左に1マス進むと+1」の点数方式だと、前を塞がれたときに点数0の上下移動を繰り返して同じ場所に留まっていた（特に2x2の『でかエナー』）。
    /// 毎回距離が必ず縮むマスにだけ動くので、上下にうろうろして元の場所に戻ることはない。
    /// </summary>
    public static class EnemyMovePlanner
    {
        private static readonly int[] DirH = { 0, -1, 1, 0 };
        private static readonly int[] DirW = { -1, 0, 0, 1 };

        private const int Unreachable = int.MaxValue;

        /// <summary>
        /// 1フェーズ分の移動経路を求める。
        /// </summary>
        /// <param name="board">盤面</param>
        /// <param name="span">ユニットが占有する1辺のマス数（1x1 なら 1、2x2 なら 2）</param>
        /// <param name="srcH">現在の基準マス（縦）</param>
        /// <param name="srcW">現在の基準マス（横）</param>
        /// <param name="move">このフェーズで移動できる手数</param>
        /// <param name="randomRange">
        /// 同点の候補から選ぶための乱数。0 以上 maxExclusive 未満を返す。
        /// null のときは最初に見つかった候補を選ぶ（ステージエディタのプレビュー用）。
        /// </param>
        /// <returns>移動するマスを順に並べた経路。移動しないときは空。</returns>
        public static List<(int h, int w)> PlanPath(IEnemyMoveBoard board, int span, int srcH, int srcW, int move,
            Func<int, int> randomRange = null)
        {
            var path = new List<(int h, int w)>();
            if (move <= 0 || srcW <= 0) return path;

            var mapH = board.Height;
            var mapW = board.Width;
            if (srcH < 0 || srcH >= mapH || srcW >= mapW) return path;

            // 今の盤面で move 手以内に行けるマスと、そこまでの経路
            var stepTo = ComputeReachable(board, span, srcH, srcW, move, out var prevH, out var prevW);

            // 距離の測り方を順に試し、近づけるマスが見つかったものを使う
            var metrics = new Func<int, int, int>[]
            {
                CreateDistanceMetric(ComputeGoalDistance(board, span, (h, w) => board.IsFootprintFree(h, w, span))),
                CreateDistanceMetric(ComputeGoalDistance(board, span, (h, w) => IsFootprintWithoutWall(board, h, w, span))),
                (_, w) => w,
            };

            foreach (var metric in metrics)
            {
                if (!TryFindDestination(stepTo, metric, srcH, srcW, randomRange, out var dstH, out var dstW)) continue;

                // 目的地から prev を逆にたどり、実際の移動経路を復元する
                var currentH = dstH;
                var currentW = dstW;
                while (currentH != srcH || currentW != srcW)
                {
                    path.Add((currentH, currentW));
                    var fromH = prevH[currentH, currentW];
                    var fromW = prevW[currentH, currentW];
                    if (fromH < 0 || fromW < 0) break;
                    currentH = fromH;
                    currentW = fromW;
                }

                path.Reverse();
                return path;
            }

            return path;
        }

        private static Func<int, int, int> CreateDistanceMetric(int[,] distance)
        {
            return (h, w) => distance[h, w];
        }

        /// <summary>
        /// 到達できるマスのうち、metric が今いる場所より小さい（＝目的地に近い）マスを1つ選ぶ。
        /// 同じ近さなら、手数が少ない → 縦のズレが小さい順に優先し、それでも並べばランダムに選ぶ。
        /// </summary>
        private static bool TryFindDestination(int[,] stepTo, Func<int, int, int> metric, int srcH, int srcW,
            Func<int, int> randomRange, out int dstH, out int dstW)
        {
            dstH = -1;
            dstW = -1;

            var srcMetric = metric(srcH, srcW);
            if (srcMetric == Unreachable) return false;

            var bestMetric = srcMetric;
            var tieCount = 0;
            var mapH = stepTo.GetLength(0);
            var mapW = stepTo.GetLength(1);
            for (var h = 0; h < mapH; h++)
            {
                for (var w = 0; w < mapW; w++)
                {
                    if (stepTo[h, w] <= 0) continue;

                    var value = metric(h, w);
                    if (value == Unreachable || value > bestMetric) continue;
                    if (dstH < 0 && value == srcMetric) continue;

                    int compare;
                    if (dstH < 0 || value < bestMetric)
                    {
                        compare = -1;
                    }
                    else
                    {
                        compare = stepTo[h, w].CompareTo(stepTo[dstH, dstW]);
                        if (compare == 0) compare = Math.Abs(h - srcH).CompareTo(Math.Abs(dstH - srcH));
                    }

                    if (compare < 0)
                    {
                        bestMetric = value;
                        dstH = h;
                        dstW = w;
                        tieCount = 1;
                    }
                    else if (compare == 0)
                    {
                        // 同点候補はリザーバーサンプリングで等確率に選ぶ
                        tieCount++;
                        if (randomRange != null && randomRange(tieCount) == 0)
                        {
                            dstH = h;
                            dstW = w;
                        }
                    }
                }
            }

            return dstH >= 0;
        }

        /// <summary>
        /// 今の盤面で、move 手以内に到達できるマスを幅優先探索で求める。到達できないマスは -1。
        /// </summary>
        private static int[,] ComputeReachable(IEnemyMoveBoard board, int span, int srcH, int srcW, int move,
            out int[,] prevH, out int[,] prevW)
        {
            var mapH = board.Height;
            var mapW = board.Width;
            var stepTo = new int[mapH, mapW];
            prevH = new int[mapH, mapW];
            prevW = new int[mapH, mapW];
            for (var h = 0; h < mapH; h++)
            {
                for (var w = 0; w < mapW; w++)
                {
                    stepTo[h, w] = -1;
                    prevH[h, w] = -1;
                    prevW[h, w] = -1;
                }
            }

            var queue = new Queue<(int h, int w)>();
            stepTo[srcH, srcW] = 0;
            queue.Enqueue((srcH, srcW));
            while (queue.Count > 0)
            {
                var (h, w) = queue.Dequeue();
                if (stepTo[h, w] >= move) continue;

                for (var dir = 0; dir < DirH.Length; dir++)
                {
                    var nextH = h + DirH[dir];
                    var nextW = w + DirW[dir];
                    if (!IsInBounds(nextH, nextW, mapH, mapW) || stepTo[nextH, nextW] >= 0) continue;
                    if (!board.IsFootprintFree(nextH, nextW, span)) continue;

                    stepTo[nextH, nextW] = stepTo[h, w] + 1;
                    prevH[nextH, nextW] = h;
                    prevW[nextH, nextW] = w;
                    queue.Enqueue((nextH, nextW));
                }
            }

            return stepTo;
        }

        /// <summary>
        /// 各マスを基準マスにしたとき、左端（width=0）まで何手で行けるかを求める。
        /// isPassable が false のマスは通れない。到達できないマスは int.MaxValue。
        /// </summary>
        private static int[,] ComputeGoalDistance(IEnemyMoveBoard board, int span, Func<int, int, bool> isPassable)
        {
            var mapH = board.Height;
            var mapW = board.Width;
            var dist = new int[mapH, mapW];
            var queue = new Queue<(int h, int w)>();
            for (var h = 0; h < mapH; h++)
            {
                for (var w = 0; w < mapW; w++)
                {
                    dist[h, w] = Unreachable;
                }
            }

            for (var h = 0; h < mapH; h++)
            {
                if (!isPassable(h, 0)) continue;
                dist[h, 0] = 0;
                queue.Enqueue((h, 0));
            }

            while (queue.Count > 0)
            {
                var (h, w) = queue.Dequeue();
                for (var dir = 0; dir < DirH.Length; dir++)
                {
                    var nextH = h + DirH[dir];
                    var nextW = w + DirW[dir];
                    if (!IsInBounds(nextH, nextW, mapH, mapW) || dist[nextH, nextW] != Unreachable) continue;
                    if (!isPassable(nextH, nextW)) continue;

                    dist[nextH, nextW] = dist[h, w] + 1;
                    queue.Enqueue((nextH, nextW));
                }
            }

            return dist;
        }

        /// <summary>
        /// anchorH/anchorW を基準マスとする span マス四方が、すべて盤面内かつ壁が無いかを返す
        /// </summary>
        private static bool IsFootprintWithoutWall(IEnemyMoveBoard board, int anchorH, int anchorW, int span)
        {
            for (var dh = 0; dh < span; dh++)
            {
                for (var dw = 0; dw < span; dw++)
                {
                    var h = anchorH + dh;
                    var w = anchorW + dw;
                    if (!IsInBounds(h, w, board.Height, board.Width)) return false;
                    if (board.IsWall(h, w)) return false;
                }
            }
            return true;
        }

        private static bool IsInBounds(int h, int w, int mapH, int mapW)
        {
            return h >= 0 && h < mapH && w >= 0 && w < mapW;
        }
    }
}
