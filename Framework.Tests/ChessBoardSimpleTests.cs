using System.Linq;
using NUnit.Framework;

namespace Framework.Tests
{
    [TestFixture]
    public class ChessBoardSimpleTests
    {
        [Test]
        public void FenStart()
        {
            ChessBoard fenParsed = ChessBoard.ParseFen("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
            ChessBoard normal = new ChessBoard();

            ChessPiece[] fenInner = ChessBoard.GetInternalState(fenParsed);
            ChessPiece[] normalInner = ChessBoard.GetInternalState(normal);

            CollectionAssert.AreEqual(normalInner, fenInner);
        }

        [Test]
        public void FoolsMateWorking()
        {
            Assert.That(ChessBoard.ParseFen("rnbqkbnr/pppp1ppp/8/4p3/6P1/5P2/PPPPP2P/RNBQKBNR b KQkq - 0 2").ValidMoves.Any(x => x.CheckMate));
        }

        [Test]
        public void DiagionalSlideCheck()
        {
            Assert.That(ChessBoard.ParseFen("rnbqkbnr/ppp1pppp/3p4/8/8/4P3/PPPP1PPP/RNBQKBNR w KQkq - 0 2").ValidMoves.Any(x => x.Check));
        }

        [Test]
        public void KnighCheck()
        {
            Assert.That(ChessBoard.ParseFen("rnbqkb1r/pppppppp/5n2/8/8/5P2/PPPPPKPP/RNBQ1BNR b kq - 2 2").ValidMoves.Any(x => x.Check));
        }

        [Test]
        public void SlideDiagionalPreventKingMove()
        {
            Assert.AreEqual(3, ChessBoard.ParseFen("6k1/8/7B/8/8/8/8/K7 b - - 2 2").ValidMoves.Count);
        }

        [Test]
        public void SlideStraightPreventKingMove()
        {
            Assert.AreEqual(1, ChessBoard.ParseFen("7k/8/8/8/8/8/8/K5R1 b - - 2 2").ValidMoves.Count);
        }

        [Test]
        public void KnightPreventKingMove()
        {
            Assert.AreEqual(1, ChessBoard.ParseFen("7k/8/5N2/8/8/8/8/K7 b - - 2 2").ValidMoves.Count);
        }

        [Test]
        public void PawnPreventsKingMove()
        {
            Assert.AreEqual(1, ChessBoard.ParseFen("7k/5P2/5P2/8/8/8/8/2K5 b - - 2 2").ValidMoves.Count);
        }
        
        [Test]
        public void PawnCanCheckBlackKing()
        {
            Assert.That(ChessBoard.ParseFen("6k1/8/5P2/8/8/8/8/2K5 w - - 2 2").ValidMoves.Any(x => x.Check));
        }

        [Test]
        public void PawnCanCheckWhiteKing()
        {
            Assert.That(ChessBoard.ParseFen("6k1/8/8/8/8/3p4/8/2K5 b - - 2 2").ValidMoves.Any(x => x.Check));
        }

        [Test]
        public void BlockedKingMateBlack()
        {
            Assert.That(ChessBoard.ParseFen("rnbq1bnr/ppppkppp/8/4p2Q/8/4P3/PPPP1PPP/RNB1KBNR w KQkq - 1 2").ValidMoves.Any(x => x.CheckMate));
        }

        [Test]
        public void CastleKingsideRookThreatKnight()
        {
            ChessBoard board = ChessBoard.ParseFen("r3k2r/pppppNpp/8/8/8/8/PPPPPnPP/R3K2R w KQkq - 2 2");
            Assert.AreEqual(1, board.ValidMoves.Count(x => x.Castle));
        }

        [Test]
        public void CastleQueenRookThreatKnight()
        {
            ChessBoard board = ChessBoard.ParseFen("r3k2r/pppppppp/1N6/8/8/1n6/PPPPPPPP/R3K2R w KQkq - 2 2");
            Assert.AreEqual(1, board.ValidMoves.Count(x => x.Castle));
        }

        
        [Test]
        public void CastleKingsideRookThreatRook()
        {
            ChessBoard board = ChessBoard.ParseFen("r3k2r/ppppppp1/8/8/8/8/PPPPPPP1/R3K2R w KQkq - 2 2");
            Assert.AreEqual(2, board.ValidMoves.Count(x => x.Castle));
        }

        [Test]
        public void CastleQueenRookThreatRook()
        {
            ChessBoard board = ChessBoard.ParseFen("r3k2r/1ppppppp/8/8/8/8/1PPPPPPP/R3K2R w KQkq - 2 2");
            Assert.AreEqual(2, board.ValidMoves.Count(x => x.Castle));
        }

        [Test]
        public void EnPessantTest()
        {
            ChessBoard initial = ChessBoard.ParseFen("6k1/8/8/4pP2/8/8/8/2K5 w - e6 2 2");
            Assert.That(initial.ValidMoves.Any(x => x.EnPassant));

            ChessMove chessMove = initial.ValidMoves.First(x => x.EnPassant);
            ChessBoard after = initial.ExecuteMove(chessMove);

            Assert.AreEqual("6k1/8/4P3/8/8/8/8/2K5 b - - 3 0", after.ToFen());
        }
    }
}