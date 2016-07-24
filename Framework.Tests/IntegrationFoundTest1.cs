using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Framework.Tests
{
    [TestFixture]
    class IntegrationFoundTest1
    {
        [Test]
        public void PeasentCheck()
        {
            Assert.AreEqual(5, ChessBoard.ParseFen("rnbq1bnr/pppkpppp/2Pp4/8/8/8/PP1PPPPP/RNBQKBNR b KQkq - 1 2").ValidMoves.Count);
        }

        [Test]
        public void KingMoveCastleAvailableTest()
        {
            ChessBoard board = ChessBoard.ParseFen("r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 0");
            ChessMove m = new ChessMove(ChessBoard.GetInternalState(board), new InnerChessMove(25, 26), false, false);

            ChessBoard b2 = board.ExecuteMove(m);

            Assert.AreEqual("r3k2r/8/8/8/8/8/8/R4K1R b kq - 1 0", b2.ToFen());
        }

        [Test]
        public void CantCastleOverPawn()
        {
            ChessBoard b = ChessBoard.ParseFen("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q2/PPPBBPpP/1R2K2R w Kkq - 0 3");
            Assert.AreEqual(0, b.ValidMoves.Count(x => x.Castle));
        }
    }
}
