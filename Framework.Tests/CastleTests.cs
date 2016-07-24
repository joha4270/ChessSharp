using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Framework.Tests
{
    [TestFixture]
    class CastleTests
    {
        [Test]
        public void CastleKingSide()
        {
            ChessBoard board = ChessBoard.ParseFen("r3k2r/8/8/8/8/8/8/4K3 b k - 0 1");
            ChessMove move = board.ValidMoves.First(x => x.Castle);
            Assert.AreEqual("r4rk1/8/8/8/8/8/8/4K3 w - - 1 2",board.ExecuteMove(move).ToFen());
        }

        [Test]
        public void CastleQueenSide()
        {
            ChessBoard board = ChessBoard.ParseFen("r3k2r/8/8/8/8/8/8/4K3 b q - 0 1");
            ChessMove move = board.ValidMoves.First(x => x.Castle);
            Assert.AreEqual("2kr3r/8/8/8/8/8/8/4K3 w - - 1 2", board.ExecuteMove(move).ToFen());
        }
    }
}
