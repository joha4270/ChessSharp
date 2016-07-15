using System.Collections.Generic;

namespace Framework
{
    public interface IChessBoard
    {
        IReadOnlyList<ChessMove> ValidMoves { get; }

        ChessBoard ExecuteMove(ChessMove move);
    }
}