using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Framework.Tests
{
    [TestFixture]
    class PerftPerformance
    {
        public static int MaxPerftDepth = 6;
        [Test]
        [TestCase(0, 1)]
        [TestCase(1, 20)]
        [TestCase(2, 400)]
        [TestCase(3, 8902)]
        [TestCase(4, 197281)]
        [TestCase(5, 4865609)]
        [TestCase(6, 119060324)]
        public void Nodes(int depth, int count)
        {
            if (depth > MaxPerftDepth) Assert.Ignore();
            var v = PerformanceUtilities.Perft(depth);

            Assert.AreEqual(v.Item2, count);
            TestContext.WriteLine($"Counted {v.Item2} nodes (depth {depth}) in {v.Item1}");
        }

        [TestCase(1, 48)]
        [TestCase(2, 2039)]
        [TestCase(3, 97862)]
        [TestCase(4, 4085603)]
        [TestCase(5, 193690690)]
        public void KiwiPete(int depth, int count)
        {
            if(depth > MaxPerftDepth) Assert.Ignore();
            ChessBoard board = ChessBoard.ParseFen("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 5");

            var v = PerformanceUtilities.Perft(depth, board);

            Assert.AreEqual(count, v.Item2);
            TestContext.WriteLine($"Counted {v.Item2} nodes (depth {depth}) in {v.Item1}");
        }

        [TestCase(1, 2)]
        [TestCase(2, 91)]
        [TestCase(3, 3162)]
        [TestCase(4, 128013)]
        [TestCase(5, 4993637)]
        public void KiwiPeteCastle(int depth, int castles)
        {
            if (depth > MaxPerftDepth) Assert.Ignore();
            ChessBoard board = ChessBoard.ParseFen("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 5");

            var v = PerformanceUtilities.Perft(depth, x => x.Castle ? 1 : 0, board);

            Assert.AreEqual(castles, v.Item2);
        }
    }
}
