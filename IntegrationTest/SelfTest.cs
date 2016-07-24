using System.Collections.Generic;
using Framework;

namespace IntegrationTest
{
    class SelfTest
    {
        public static Dictionary<string, long> Test(string fen, int depth)
        {
            Dictionary<string, long> result = new Dictionary<string, long>();

            ChessBoard board = ChessBoard.ParseFen(fen);

            foreach (ChessMove move in board.ValidMoves)
            {
                if (depth == 1)
                {
                    result.Add(move.AlgebraicFrom + move.AlgebraicTo, 1);
                }
                else
                {
                    ChessBoard after = board.ExecuteMove(move);
                    var res = PerformanceUtilities.Perft(depth - 1, after);
                    result.Add(move.AlgebraicFrom + move.AlgebraicTo, res.Item2);
                }
            }

            return result;
        }
    }
}