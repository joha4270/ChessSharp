using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework
{
    public static class PerformanceUtilities
    {
        public static Tuple<TimeSpan, int> Perft(int depth, ChessBoard board = default(ChessBoard))
        {
            return Perft(depth, x => 1, board);
        }

        public static Tuple<TimeSpan, int> Perft(int depth, Func<ChessMove, int> count,
            ChessBoard board = default(ChessBoard))
        {
            if(board == null)
                board = new ChessBoard();

            Stopwatch sw = Stopwatch.StartNew();

            int counted = PerftInner(depth, board, count);

            sw.Stop();

            return new Tuple<TimeSpan, int>(sw.Elapsed, counted);
        }

        private static int PerftInner(int depth, ChessBoard board, Func<ChessMove, int> count)
        {
            if (depth < 1) return 1;
            else if (depth == 1)
            {
                return board.ValidMoves.Sum(count);
            }
            else
            {
                int total = 0;
                foreach (ChessMove move in board.ValidMoves)
                {
                    ChessBoard child = board.ExecuteMove(move);
                    total += PerftInner(depth - 1, child, count);
                }

                return total;
            }
        }
    }
}
