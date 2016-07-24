using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting;
using System.Text;
using static Framework.ChessUtil;

namespace Framework
{
    public class ChessBoard : IChessBoard
    {
        //Actual game state
        private readonly ChessPiece[] _boardState;
        private ChessColor _nextMove;
        private readonly int _epSquare = 0;
        private readonly int _halfMoveActionCounter;
        private readonly int _moveCounter = 1;
        private readonly bool bKingSide = true;
        private readonly bool wKingSide = true;
        private readonly bool bQueenSide = true;
        private readonly bool wQueenSide = true;

        //cached stuff
        private IReadOnlyList<ChessMove> _validMoves;
        private readonly int _whiteKing;
        private readonly int _blackKing;
        private long _zobristHash = 0;

        public static IChessHashTable HashTableImplentation { get; set; } = new DefaultHashTable();

        public IReadOnlyList<ChessMove> ValidMoves
            => _validMoves ?? (_validMoves = Array.AsReadOnly(GenerateValidMoves()));

        private static readonly long[] ZobristTable;
        private const int ZobristBlackIndex = 12*64;
        private const int ZobristCastleIndex = ZobristBlackIndex + 1;
        private const int ZobristEpIndex = ZobristCastleIndex + 4;
        static ChessBoard()
        {
            Random r = new Random(302499); //Seed needs to be constant. Otherwise hash changes from run to run. Makes saving just hash impossible. Not sure if number is good
            ZobristTable = new long[12 * 64 + 1 +  4 + 8]; //12 pieces on 64 squares, 1 side to move, 4 castling rights 8 en passant
            for (int i = 0; i < ZobristTable.Length; i++)
            {
                ZobristTable[i] = r.NextLong();
            }
        }

        public ChessBoard()
        {
            _nextMove = ChessColor.White;
            _boardState = InitialBoardState();

            for (int i = 0; i < 8; i++)
            {
                _boardState[31 + i] = ChessPiece.Pawn;
                _boardState[81 + i] = ChessPiece.Pawn;
            }

            int wking = 25;
            int bking = 95;

            SetPoints(_boardState, ChessPiece.Rook, 21, 28, 91, 98);
            SetPoints(_boardState, ChessPiece.Bishop, 23, 26, 93, 96);
            SetPoints(_boardState, ChessPiece.Knight, 22, 27, 92, 97);
            SetPoints(_boardState, ChessPiece.Queen, 24, 94);
            SetPoints(_boardState, ChessPiece.King, wking, bking);
            _whiteKing = wking;
            _blackKing = bking;

            for (int i = 0; i < 8; i++)
            {
                _boardState[81 + i] |= ChessPiece.Black;
                _boardState[91 + i] |= ChessPiece.Black;
            }
        }

        private ChessBoard(ChessBoard previousTurn, ChessMove move)
        {
            _boardState = new ChessPiece[120];
            Array.Copy(previousTurn._boardState, _boardState, 120);

            _nextMove = previousTurn._nextMove == ChessColor.White ? ChessColor.Black : ChessColor.White;

            _moveCounter = previousTurn._moveCounter + (_nextMove == ChessColor.White ? 1 : 0);

            _whiteKing = previousTurn._whiteKing == move.InnerMove.PieceCordinate
                ? move.InnerMove.HitCordinate
                : previousTurn._whiteKing;

            _blackKing = previousTurn._blackKing == move.InnerMove.PieceCordinate
                ? move.InnerMove.HitCordinate
                : previousTurn._blackKing;

            if (move.Capture != ChessPiece.Empty || move.Moved.AsWhite() == ChessPiece.Pawn)
            {
                _halfMoveActionCounter = 0;
            }
            else
            {
                _halfMoveActionCounter = previousTurn._halfMoveActionCounter + 1;
            }

            if (move.EnPassant)
            {
                //Clear the point that was taken with enpassant. Involes witchcraft
                //as this is not actually stored anywhere, we have to derive it. We do this by taking where the peasent is moving
                //from to get the row and where it goes to get the column

                int a = (move.InnerMove.PieceCordinate/10)*10;
                a += move.InnerMove.HitCordinate%10;

                _boardState[a] = ChessPiece.Empty;

            }

            if (move.Moved.AsWhite() == ChessPiece.Pawn &&
                Math.Abs(move.InnerMove.PieceCordinate - move.InnerMove.HitCordinate) == 20)
            {
                //According to FEN notation, the EP point is the field the peasent skipped. 
                //This evaluates to the middle of destination and origin. Easier than calculating it with multi direction logic
                _epSquare = (move.InnerMove.PieceCordinate + move.InnerMove.HitCordinate)/2;
            }

            bKingSide = previousTurn.bKingSide;
            wKingSide = previousTurn.wKingSide;
            bQueenSide = previousTurn.bQueenSide;
            wQueenSide = previousTurn.wQueenSide;

            if (previousTurn._whiteKing == 25 && move.InnerMove.PieceCordinate == previousTurn._whiteKing)
            {
                wKingSide = false;
                wQueenSide = false;
            }

            if (previousTurn._blackKing == 95 && move.InnerMove.PieceCordinate == previousTurn._blackKing)
            {
                bKingSide = false;
                bQueenSide = false;
            }

            if (move.Castle)
            {
                bool kingside = move.InnerMove.HitCordinate > move.InnerMove.PieceCordinate;
                int rookDest = move.InnerMove.HitCordinate + (kingside ? -1 : 1);
                int rookFrom = 0;
                for (int i = 1 + move.Moved.GetColor().OfficerLine(); i < 9 + move.Moved.GetColor().OfficerLine(); i++)
                {
                    if (_boardState[i].AsWhite() != ChessPiece.Rook) continue; 
                    rookFrom = i; //when rook found save it
                    if (!kingside)  //if we are queenside (not kingside) stop on the first we find, otherwise keep searching for the (maybe) second.
                        break;
                }
                _boardState[rookDest] = _boardState[rookFrom] |= ChessPiece.Moved;
                _boardState[rookFrom] = ChessPiece.Empty;
            }

            if (move.Moved.AsWhite() == ChessPiece.Rook && (_boardState[move.InnerMove.PieceCordinate] & ChessPiece.Moved) == 0 &&  (move.InnerMove.PieceCordinate / 10) == (move.Moved.GetColor().OfficerLine() / 10))
            {

                if (previousTurn._nextMove == ChessColor.Black)
                {
                    bool kingSide = _blackKing < move.InnerMove.PieceCordinate;
                    if (kingSide)
                    {
                        bKingSide = false;
                    }
                    else
                    {
                        bQueenSide = false;
                    }
                }
                else
                {
                    bool kingside = _whiteKing < move.InnerMove.PieceCordinate;
                    if (kingside)
                    {
                        wKingSide = false;
                    }
                    else
                    {
                        wQueenSide = false;
                    }
                }
            }

            

            _boardState[move.InnerMove.HitCordinate] = _boardState[move.InnerMove.PieceCordinate] |= ChessPiece.Moved;
            _boardState[move.InnerMove.PieceCordinate] = ChessPiece.Empty;

        }

        private ChessBoard(ChessPiece[] board, ChessColor color, int ep, int halfmove, int fullmove, bool wKingSide, bool wQueenSide, bool bKingSide, bool bQueenSide)
        {
            _boardState = board;
            _nextMove = color;
            _epSquare = ep;
            _halfMoveActionCounter = halfmove;
            _moveCounter = fullmove;
            this.bKingSide = bKingSide;
            this.bQueenSide = bQueenSide;
            this.wKingSide = wKingSide;
            this.wQueenSide = wQueenSide;

            ChessPiece kingMask = ChessPiece.NotUsedPiece | ChessPiece.Black;
            ChessPiece blackKing = ChessPiece.King | ChessPiece.Black;
            ChessPiece whiteKing = ChessPiece.King;

            for (int i = 0; i < 120; i++)
            {
                if ((_boardState[i] & kingMask) == whiteKing)
                {
                    if (_whiteKing == 0)
                    {
                        _whiteKing = i;
                    }
                    else
                    {
                        throw new ArgumentException("Multiple Kings are not allowed");
                    }
                }

                if ((_boardState[i] & kingMask) == blackKing)
                {
                    if (_blackKing == 0)
                    {
                        _blackKing = i;
                    }
                    else
                    {
                        throw new ArgumentException("Multiple Kings are not allowed");
                    }
                }
            }
        }

        private void SetPoints(ChessPiece[] board, ChessPiece value, params int[] points)
        {
            foreach (int t in points)
            {
                board[t] = value;
            }
        }

        private static ChessPiece[] InitialBoardState()
        {
            ChessPiece[] state = new ChessPiece[120];

            for (int i = 0; i < 20; i++)
            {
                state[i] = ChessPiece.Invalid;
                state[119 - i] = ChessPiece.Invalid;
            }

            for (int i = 0; i < 8; i++)
            {
                state[20 + (i*10)] = ChessPiece.Invalid;
                state[29 + (i*10)] = ChessPiece.Invalid;
            }

            return state;
        }

        public ChessPiece this[int row, char column]
        {
            get
            {
                int start = row*10 + 21;
                start += 'a' - column;

                return _boardState[start];
            }
        }

        //TODO: new ChessBoard(move) ignores castle state

        private IEnumerable<int> AllPieces(ChessColor color)
        {
            for (int i = 20; i < 100; i += 10)
            {
                for (int j = 1; j < 9; j++)
                {
                    int k = i + j;
                    if (_boardState[k] == ChessPiece.Empty) continue;
                    if ((_boardState[k] & ChessPiece.Black) == (ChessPiece) color)
                    {
                        yield return k;
                    }
                }
            }
        }
                                                           //♟,  ♜,    ♝,   ♜,   ♛,   ♚
        private static readonly bool[] LongMoves = {false, false, true, true, true, false};
        private static readonly int[] Diections =  {   0,     8,    4,    4,    8,    8 };

        private static readonly int[,] MoveMask = {
            {0,  0,  0,   0,   0,   0,   0,   0}, //Pawn not used
            {19, 21, 12,  8,  -8, -12, -21, -19}, //Knight move mask
            {11, 9, -11, -9,   0,   0,   0,   0}, //bishop moove mask
            {10, 1, -1,  -10,  0,   0,   0,   0}, //rook 
            {10, 1, -1,  -10, 11,   9, -11,  -9}, //Queen
            {10, 1, -1,  -10, 11,   9, -11,  -9}, //King

        };

        private ChessMove[] GenerateValidMoves()
        {
            ChessMove[] ret = HashTableImplentation?.GetHashedMoves(Zobrist64Hash());

            if (ret == null)
            {
                ret = GenerateValidMovesInner().ToArray();
                HashTableImplentation?.SetMoveHash(Zobrist64Hash(), ret);
            }

            return ret;
        }

        private IEnumerable<ChessMove> GenerateValidMovesInner()
        {
            Stopwatch sw = Stopwatch.StartNew();
            var v = AllPseudoLegalMoves().ToList();
            TimeSpan ps = sw.Elapsed;
            foreach (InnerChessMove move in v)
            {
                bool check, mate;
                if (IsLegal(move, out check, out mate))
                {
                    yield return new ChessMove(_boardState, move, check, mate);
                }
            }

            sw.Stop();
            TimeSpan vf = sw.Elapsed;
        }

        public static int counter = 0;
        
        private bool IsLegal(InnerChessMove move, out bool check, out bool mate)
        {
            mate = false;

            //Check if move removes cover from king
            int selfKing = _nextMove == ChessColor.White ? _whiteKing : _blackKing;
            int otherKing = _nextMove != ChessColor.White ? _whiteKing : _blackKing;

            int kingNewPos = selfKing == move.PieceCordinate ? move.HitCordinate : selfKing;

            bool peasentThreat = IsPeasentThreat(kingNewPos, _nextMove,
                _boardState[move.PieceCordinate], move.PieceCordinate, move.HitCordinate);
            bool knightThreat = IsKnightThreat(_boardState, kingNewPos, _nextMove,
                _boardState[move.PieceCordinate], move.PieceCordinate, move.HitCordinate);
            bool slideThreat = IsSlideThreat(_boardState, _nextMove, kingNewPos,
                _boardState[move.PieceCordinate], move.PieceCordinate, move.HitCordinate);

            var legal = !(slideThreat ||
                          knightThreat ||
                           peasentThreat);

            if (!legal)
            {
                check = false;
                mate = false;
                return false;
            }

            //if (IsSlideThreat(_boardState, _boardState[move.PieceCordinate].GetColor(),
            //    selfKing == move.PieceCordinate ? move.HitCordinate : selfKing, _boardState[move.PieceCordinate],
            //    move.PieceCordinate, move.HitCordinate))
            //{
            //    counter++;
            //    legal = false;
            //}
            //else
            //{
            //    if (move.PieceCordinate == selfKing)
            //    {
            //        legal = !IsKnightThreat(_boardState, move.HitCordinate, _boardState[selfKing].GetColor());
            //    }
            //    else
            //    {
            //        legal = true;
            //    }

            //}



            check =
                IsSlideThreat(_boardState, _nextMove.OtherColor(), otherKing,
                    _boardState[move.PieceCordinate], move.PieceCordinate, move.HitCordinate) ||
                IsKnightThreat(_boardState, otherKing, _nextMove.OtherColor(),
                    _boardState[move.PieceCordinate], move.PieceCordinate, move.HitCordinate) ||
                IsPeasentThreat(otherKing, _nextMove.OtherColor(), _boardState[move.PieceCordinate], move.PieceCordinate, move.HitCordinate);

            if (check)
            {
                mate = IsMate(move, otherKing);
            }

            return true;
        }

        private bool IsMate(InnerChessMove move, int kingPos)
        {
            ChessBoard board = new ChessBoard(this, new ChessMove(_boardState, move, true, false));

            var defenderMoves = board.AllPseudoLegalMoves().ToList();

            board._nextMove = board._nextMove.OtherColor();
            var attackerMoves = board.AllPseudoLegalMoves();
            //Check if king can move
            var attackingKingMoves = attackerMoves.Where(x => x.HitCordinate == OtherKing).ToList();
            
            if (attackingKingMoves.Count == 0)
            { throw new ImpossibleException(); }
            else if (attackingKingMoves.Count == 1)
            {
                int pieceCord = attackingKingMoves[0].PieceCordinate;
                ChessPiece piece = board._boardState[pieceCord];
                bool slide = LongMoves[piece.Type()];
                if (slide)
                {
                    bool block = false;
                    foreach (InnerChessMove x in defenderMoves)
                    {
                        if(x.PieceCordinate == kingPos) continue;

                        if (IsBeteen(pieceCord, kingPos, x.HitCordinate))
                        {
                            block = true;
                            break;
                        }
                    }
                    if (block)
                        return false;
                }

                bool take = defenderMoves.Any(x => x.HitCordinate == pieceCord);

                if(take)
                    return false;

            }

            int[] directions = { 1, 10, 9, 11, -1, -10, -9, -11 };
            foreach (int vector in directions)
            {
                int square = vector + kingPos;
                bool sameColor = _boardState[square].SameColor(_boardState[kingPos]);
                if (_boardState[square] == ChessPiece.Invalid || sameColor || board.AnyThreat(square, _nextMove.OtherColor(), kingPos))  //bug? board._nextMove == _nextMove wtf?
                    continue;

                return false;
            }
            
            return true;

        }

        private bool AnyThreat(int square, ChessColor defender, int notBlockinganymore = 0)
        {
            bool knightThreat = IsKnightThreat(_boardState, square, defender);

            if (knightThreat)
                return true;

            bool slideThreat = IsSlideThreat(_boardState, defender, square, ChessPiece.King | (ChessPiece) defender, notBlockinganymore);

            if (slideThreat)
                return true;

            bool peasentThreat = IsPeasentThreat(square, defender);
            //TODO: pin detection
            if (peasentThreat)
                return true;

            return false;
        }

        private static bool IsSlideThreat(ChessPiece[] board, ChessColor defender, int point, ChessPiece movedPiece,
            int nowEmpty = 0, int nowChanged = 0)
        {
            for (int i = 0; i < 8; i++)
            {
                ChessPiece hit;
                int hitpoint;
                int checkdir = MoveMask[4, i];
                for (int j = 1; true; j++)
                {
                    hitpoint = point + checkdir*j;

                    hit = ChangedBoard(board, movedPiece, nowEmpty, nowChanged, hitpoint);

                    if (hit != ChessPiece.Empty)
                    {
                        break;
                    }
                }

                if (hit.GetColor() == defender || hit == ChessPiece.Invalid)
                    continue;

                if (hit.AsWhite() == ChessPiece.Pawn) continue;
                
                if (hit.AsWhite() == ChessPiece.Queen)
                {
                    return true;
                }
                else if (hit.AsWhite() == ChessPiece.Rook && i < 4)
                {
                    return true;
                }
                else if (hit.AsWhite() == ChessPiece.Bishop && i >= 4)
                {
                    return true;
                }
            }

            return false;
        }

        private static ChessPiece ChangedBoard(ChessPiece[] board, ChessPiece movedPiece, int @from, int to,
            int hitpoint)
        {
            ChessPiece hit;
            if (hitpoint == @from)
            {
                hit = ChessPiece.Empty;
            }
            else if (hitpoint == to)
            {
                hit = movedPiece;
            }
            else
            {
                hit = board[hitpoint];
            }
            return hit;
        }

        private bool IsKnightThreat(ChessPiece[] board, int point, ChessColor defender, ChessPiece moved = ChessPiece.Empty, int from = 0, int to = 0)
        {

            for (int i = 0; i < 8; i++)
            {
                ChessPiece atPoint = ChangedBoard(board, moved, from, to, point + MoveMask[1, i]);
                if (atPoint != ChessPiece.Invalid && atPoint.AsWhite() == ChessPiece.Knight && atPoint.GetColor() != defender)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsPeasentThreat(int point, ChessColor forWho, ChessPiece moved = ChessPiece.Empty, int from = 0, int to = 0)
        {
            ChessPiece falsePiece = ChessPiece.King | (ChessPiece) forWho ;

            int pawnMove = forWho.OtherColor().PawnDirection();

            for (int i = -1; i <= 1; i+=2)
            {
                ChessPiece maybePawn = ChangedBoard(_boardState, moved, from, to, point + i - pawnMove);
                if (maybePawn != ChessPiece.Empty && 
                    maybePawn != ChessPiece.Invalid &&
                    maybePawn.AsWhite() == ChessPiece.Pawn && 
                    maybePawn.DifferentColor(falsePiece))
                    return true;
            }

            return false;
        }

        private IEnumerable<InnerChessMove> AllPseudoLegalMoves()
        {
            foreach (int pieceCordinate in AllPieces(_nextMove))
            {
                foreach (InnerChessMove innerChessMove in PieceMovements(pieceCordinate))
                {
                    yield return innerChessMove;
                }
            }
        }

        private IEnumerable<InnerChessMove> PieceMovements(int pieceCordinate)
        {
            ChessPiece workingPiece = _boardState[pieceCordinate];
            int pieceType = workingPiece.Type();

            if (pieceType == 0) //Pawn
            {
                int pawnLine = _nextMove == ChessColor.White ? 3 : 8;
                int pawnDirection = _nextMove.PawnDirection();

                if (_boardState[pieceCordinate + pawnDirection] == ChessPiece.Empty) //Normal pawn one field forward
                {
                    yield return new InnerChessMove(pieceCordinate, pieceCordinate + pawnDirection);

                    //Double move at start?
                    if (pieceCordinate/10 == pawnLine && _boardState[pieceCordinate + pawnDirection*2] == ChessPiece.Empty)
                        yield return new InnerChessMove(pieceCordinate, pieceCordinate + pawnDirection*2);
                }

                //Pawn captures (Normal)
                for (int i = -1; i <= 1; i += 2)
                {
                    int hitpoint = pieceCordinate + pawnDirection + i;
                    if (_boardState[hitpoint].DifferentColor(workingPiece) || hitpoint == _epSquare)
                        yield return new InnerChessMove(pieceCordinate, hitpoint);
                }
            }
            else
            {
                int directions = Diections[pieceType];
                for (int i = 0; i < directions; i++) //For all directions the piece can move in
                {
                    for (int j = 1;; j++) //As long as possible
                    {
                        int hitCordinate = pieceCordinate + MoveMask[pieceType, i]*j;
                        ChessPiece hit = _boardState[hitCordinate]; //Find what we would hit

                        if (workingPiece.SameColor(hit) || hit == ChessPiece.Invalid)
                            break; //if it is the same color, or outside board

                        if (hit == ChessPiece.Empty)
                            yield return new InnerChessMove(pieceCordinate, hitCordinate); //Is it empty or enemy it is valid
                        else if (hit.DifferentColor(workingPiece))
                        {
                            yield return new InnerChessMove(pieceCordinate, hitCordinate);
                            break;
                        }

                        if (!LongMoves[pieceType]) break;
                    }
                }
                if (pieceType == 5) //king?
                {
                    bool kingSide = _nextMove == ChessColor.White ? wKingSide : bKingSide;
                    bool queenSide = _nextMove == ChessColor.White ? wQueenSide : bQueenSide;

                    if (kingSide && CastleAvailable(true, _boardState, _nextMove))
                        yield return CastleMove(true,  ActiveKing, _nextMove);

                    if (queenSide && CastleAvailable(false, _boardState, _nextMove))
                        yield return CastleMove(false,  ActiveKing, _nextMove);
                }
            }
        }

        private InnerChessMove CastleMove(bool kingside, int kingpos, ChessColor player)
        {
            int column = kingside ? 7 : 3;
            return new InnerChessMove(kingpos, player.OfficerLine() + column);
        }

        private bool CastleAvailable(bool kingside, ChessPiece[] boardState, ChessColor player)
        {
            //BUG Castle to queenside can be blocked by threatning b. This should not be possible
            int start = !kingside ? 1 : 9;
            int mul = !kingside ? 1 : -1;
            int kingPos = 0;
            int rookPos = 0;
            int officerLine = player.OfficerLine();

            for (int i = 0; i < 8; i++)
            {
                int square = officerLine + start + (mul*i);
                ChessPiece piece = boardState[square].AsWhite();
                if (piece == ChessPiece.Rook)
                {
                    rookPos = square;
                }

                if (piece == ChessPiece.King)
                {
                    kingPos = square;
                }

                if (kingPos != 0 && rookPos != 0) break;
            }

            if (rookPos == 0 || kingPos == 0)
                return false; //Sanity! Should never happen, but does (don't quite handle castle yet) 

            //Rook don't care about threats, so move rook pos 1 toward king
            //stuff
            int kingTo;
            if (rookPos > kingPos)
            {
                kingTo = 7 + officerLine;
            }
            else
            {
                kingTo = 3 + officerLine;
            }

            int min = Math.Min(rookPos, kingPos);
            int max = Math.Max(rookPos, kingPos);

            for (int i = min + 1; i < max; i++)
            {
                if (boardState[i] != ChessPiece.Empty)
                    return false;
            }

            min = Math.Min(kingTo, kingPos);
            max = Math.Max(kingTo, kingPos);

            for (int i = min; i <= max; i++)
            {
                if (AnyThreat(i, player))
                    return false;
            }

            

            return true;
        }

        private int ActiveKing => KingOfColor(_nextMove);

        private int KingOfColor(ChessColor color)
        {
            return color == ChessColor.White ? _whiteKing : _blackKing;
        }

        private int OtherKing => KingOfColor(_nextMove.OtherColor());

        public ChessBoard ExecuteMove(ChessMove move)
        {
            bool check, mate;
            if (IsLegal(move.InnerMove, out check, out mate))
            {
                return new ChessBoard(this, move);
            }
            

            //if (ValidMoves.Contains(move))
            //{
            //    return new ChessBoard(this, move);
            //}
            else
            {
                throw new InvalidOperationException("Attempted ChessMove is not valid on this ChessBoard");
            }
        }

        public string ToFen()
        {
            const string letters = "pnbrqk";
            StringBuilder sb = new StringBuilder();
            int emptyCounter = 0;

            for (int i = 8 - 1; i >= 0; i--)
            {
                for (int j = 0; j < 8; j++)
                {
                    ChessPiece piece = _boardState[21 + j + i*10];
                    if (piece == ChessPiece.Empty)
                    {
                        emptyCounter++;
                    }
                    else
                    {
                        if(emptyCounter != 0)
                            sb.Append(emptyCounter);
                        emptyCounter = 0;

                        char let = letters[piece.Type()];
                        if ((piece & ChessPiece.Black) == ChessPiece.Empty)
                            let = char.ToUpper(let);
                        sb.Append(let);
                    }
                }
                if (emptyCounter != 0)
                    sb.Append(emptyCounter);
                emptyCounter = 0;
                sb.Append("/");
            }

            sb.Length--;
            sb.Append(' ');


            sb.Append(_nextMove == ChessColor.Black ? 'b' : 'w');
            
            sb.Append(' ');

            if (wKingSide | bKingSide | wQueenSide | bQueenSide)
            {
                if (wKingSide) sb.Append('K');
                if (wQueenSide) sb.Append('Q');
                if (bKingSide) sb.Append('k');
                if (bQueenSide) sb.Append('q');
            }
            else
            {
                sb.Append('-');
            }

            sb.Append(' ');
            sb.Append(_epSquare != 0 ? AlgebraicNotation[_epSquare] : "-");
            sb.Append(' ');
            sb.Append(_halfMoveActionCounter);
            sb.Append(' ');
            sb.Append(_moveCounter);
            
            return sb.ToString();
        }

        public long Zobrist64Hash()
        {
            if (_zobristHash != 0) return _zobristHash;

            for (int i = 0; i < 64; i++)
            {
                int index = Convert64IndexToMailBox(i);
                ChessPiece p = _boardState[index];
                if (p != ChessPiece.Empty)
                {
                    _zobristHash ^= ZobristTable[i*12 + p.ZobristKey()];
                }
            }

            if (_nextMove == ChessColor.Black)
                _zobristHash ^= ZobristTable[ZobristBlackIndex];

            if (wQueenSide)
                _zobristHash ^= ZobristTable[ZobristCastleIndex];
            if (bQueenSide)
                _zobristHash ^= ZobristTable[ZobristCastleIndex + 1];
            if (wKingSide)
                _zobristHash ^= ZobristTable[ZobristCastleIndex + 2];
            if (bKingSide)
                _zobristHash ^= ZobristTable[ZobristCastleIndex + 3];

            if (_epSquare != 0)
            {
                int epFile = (_epSquare%10) - 1;
                _zobristHash ^= ZobristTable[ZobristEpIndex + epFile];
            }

            return _zobristHash;
        }

        public override int GetHashCode()
        {
            return Zobrist64Hash().GetHashCode();
        }

        internal static ChessPiece[] GetInternalState(ChessBoard board)
        {
            ChessPiece[] copystate = new ChessPiece[120];
            Array.Copy(board._boardState, copystate, 120);
            return copystate;
        }

        public static ChessBoard ParseFen(string fen)
        {
            ChessPiece[] board = InitialBoardState();
            ChessColor color;
            string[] fields = fen.Split(' ');
            if(fields.Length != 6) throw new ArgumentException("Invalid amount of fields in FEN record");

            //field 1 (game board)
            string[] rows = fields[0].Split('/');
            if(rows.Length != 8) throw new ArgumentException("Invalid row count in FEN record");

            Dictionary<char, ChessPiece> keyPiecesMap = new Dictionary<char, ChessPiece>
            {
                {'R', ChessPiece.Rook},
                {'N', ChessPiece.Knight},
                {'B', ChessPiece.Bishop},
                {'Q', ChessPiece.Queen},
                {'K', ChessPiece.King},
                {'P', ChessPiece.Pawn}
            };

            var temp =
                keyPiecesMap.Select(
                    x => new KeyValuePair<char, ChessPiece>(char.ToLower(x.Key), x.Value | ChessPiece.Black)).ToList();

            foreach (KeyValuePair<char, ChessPiece> keyValuePair in temp)
            {
                keyPiecesMap.Add(keyValuePair.Key, keyValuePair.Value);
            }

            int rowNumber = 7;
            foreach (string row in rows)
            {
                int column = 1;
                foreach (char piece in row)
                {
                    ChessPiece o;
                    if ('0' <= piece && piece <= '8')
                    {
                        column += piece - '0';
                    }
                    else if (keyPiecesMap.TryGetValue(piece, out o))
                    {
                        board[20 + rowNumber*10 + column] = o;
                        column++;
                    }
                    else
                    {
                        throw new ArgumentException($"Invalid character ({piece}) in FEN board");
                    }
                }

                rowNumber--;
            }

            //field 2 (turn)
            if("wb".IndexOf(fields[1], StringComparison.Ordinal) == -1)
                throw new ArgumentException("Turn field has invalid value");

            color = fields[1][0] == 'w' ? ChessColor.White : ChessColor.Black;

            bool wKingSide, wQueenSide, bKingSide, bQueenSide;
            //field 3 (castling)
            if (fields[2] == "-")
            {
                //no castle
                wKingSide = wQueenSide = bKingSide = bQueenSide = false;
            }
            else
            {
                wKingSide = fields[2].Contains("K");
                wQueenSide = fields[2].Contains("Q");
                bKingSide = fields[2].Contains("k");
                bQueenSide = fields[2].Contains("q");
            }


            int ep;
            //field 4 (en pessant)
            if (fields[3] == "-")
            {
                ep = 0;
            }
            else
            {
                ep = AlgebaricToInternal(fields[3]);
                if(ep == -1) throw new ArgumentException("En passant field has invalid value");
            }

            //field 5 (halfmove since capture/pawn)
            int halfmove, fullmove;
            if(!int.TryParse(fields[4], out halfmove)) throw new ArgumentException("Halfmove field is not a number");

            //field 6 (move clock)
            if(!int.TryParse(fields[5], out fullmove)) throw new ArgumentException("Fullmove field s not a number");

            return new ChessBoard(board, color, ep, halfmove, fullmove, wKingSide, wQueenSide,bKingSide, bQueenSide);
        }

        private static int Convert64IndexToMailBox(int index)
        {
            int row = index/8;
            int column = index%8;
            return 21 + row*10 + column;
        }
    }

    internal class ImpossibleException : Exception
    {
        public ImpossibleException() : base("This should literally be impossible, something went wrong") { }
    }

    [DebuggerDisplay("{ChessUtil.AlgebraicNotation[PieceCordinate]} -> {ChessUtil.AlgebraicNotation[HitCordinate]}")]
    internal struct InnerChessMove
    {
        public int PieceCordinate { get; }
        public int HitCordinate { get; }

        public InnerChessMove(int pieceCordinate, int hitCordinate)
        {
            PieceCordinate = pieceCordinate;
            HitCordinate = hitCordinate;
        }
    }
}
