namespace Framework
{
    internal static class ChessPieceExtensions
    {
        public static bool SameColor(this ChessPiece piece, ChessPiece other)
        {
            if (piece == ChessPiece.Invalid || other == ChessPiece.Invalid || piece == ChessPiece.Empty ||
                other == ChessPiece.Empty) return false;

            return (piece & ChessPiece.Black) == (other & ChessPiece.Black);
        }

        public static bool DifferentColor(this ChessPiece piece, ChessPiece other)
        {
            if (piece == ChessPiece.Invalid || other == ChessPiece.Invalid || piece == ChessPiece.Empty ||
                other == ChessPiece.Empty) return false;

            return (piece & ChessPiece.Black) != (other & ChessPiece.Black);
        }

        public static ChessPiece AsColor(this ChessPiece piece, ChessColor newColor)
        {
            piece = piece & ~ChessPiece.Black;

            return piece | (ChessPiece) newColor;
        }

        public static ChessColor GetColor(this ChessPiece piece)
        {
            return (ChessColor) (piece & ChessPiece.Black);
        }

        public static ChessPiece AsWhite(this ChessPiece piece)
        {
            if(piece == ChessPiece.Invalid) return ChessPiece.Invalid;

            return piece & ChessPiece.NotUsedPiece; 
        }

        public static int Type(this ChessPiece piece)
        {
            return ((int)piece & 0x7) - 1;
        }

        internal static int ZobristKey(this ChessPiece piece)
        {
            int i = piece.Type();
            if ((piece & ChessPiece.Black) != 0)
            {
                i += 6;
            }

            return i;
        }

        public static ChessColor OtherColor(this ChessColor color)
        {
            return color == ChessColor.White ? ChessColor.Black : ChessColor.White;
        }

        public static int PawnDirection(this ChessColor color)
        {
            return color == ChessColor.White ? 10 : -10;
        }

        public static int OfficerLine(this ChessColor color)
        {
            return color == ChessColor.White ? 20 : 90;
        }
    }
}