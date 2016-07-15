using System;
using System.Runtime.InteropServices;

namespace Framework
{
    public struct ChessMove
    {
        public override string ToString()
        {
            return (Moved.AsWhite() == ChessPiece.Pawn ? "" : " NBRQK"[Moved.Type()].ToString()) + AlgebraicTo;
        }

        internal InnerChessMove InnerMove { get; }

        internal ChessMove(ChessPiece[] boardState, InnerChessMove innerMove, bool check, bool mate)
        {
            InnerMove = innerMove;

            Capture = boardState[innerMove.HitCordinate];
            Moved = boardState[innerMove.PieceCordinate];

            

            Castle = (Moved.AsWhite() == ChessPiece.King && Math.Abs(InnerMove.PieceCordinate - InnerMove.HitCordinate) == 2);
            EnPassant = false;
            Check = check;
            CheckMate = mate;

            if (Moved.AsWhite() == ChessPiece.Pawn && Capture == ChessPiece.Empty &&
                (innerMove.PieceCordinate - innerMove.HitCordinate)%10 != 0)
            {
                EnPassant = true;
                Capture = ChessPiece.Pawn.AsColor(Capture.GetColor().OtherColor()) | ChessPiece.Moved;
            }

        }

        public string AlgebraicFrom => ChessUtil.AlgebraicNotation[InnerMove.PieceCordinate];
        public string AlgebraicTo => ChessUtil.AlgebraicNotation[InnerMove.HitCordinate];

        public ChessPiece Moved { get; }

        public ChessPiece Capture { get;  }

        public bool EnPassant { get;  }

        public bool Castle { get; }

        public bool Check { get; }

        public bool CheckMate { get; }
    }
}