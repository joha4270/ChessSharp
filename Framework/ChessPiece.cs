using System;

namespace Framework
{
    [Flags]
    public enum ChessPiece : byte
    {
        Empty = 0,
        Pawn = 1,
        Knight = 2,
        Bishop = 3,
        Rook = 4,
        Queen= 5,
        King = 6,
        NotUsedPiece = 7,
        Moved = 8,
        Black = 128,

        Invalid = 0xFF
    }
}