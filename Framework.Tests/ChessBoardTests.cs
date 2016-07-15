using NUnit.Framework;
using Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Tests
{
    [TestFixture()]
    public class ChessBoardTests
    {
        [Test()]
        public void ChessBoardPerftStartingTest()
        {
            PerftTest(new ChessBoard(), new[]
            {
                new PerftRow(20, 0, 0, 0, 0, 0, 0, new Dictionary<string, int>
                {
                    {"Na3", 1},
                    {"Nc3", 1},
                    {"Nf3", 1},
                    {"Nh3", 1},
                    {"a3", 1},
                    {"a4", 1},
                    {"b3", 1},
                    {"b4", 1},
                    {"c3", 1},
                    {"c4", 1},
                    {"d3", 1},
                    {"d4", 1},
                    {"e3", 1},
                    {"e4", 1},
                    {"f3", 1},
                    {"f4", 1},
                    {"g3", 1},
                    {"g4", 1},
                    {"h3", 1},
                    {"h4", 1},
                }),
                new PerftRow(400, 0, 0, 0, 0, 0, 0, new Dictionary<string, int>
                {
                    {"Na3", 20},
                    {"Nc3", 20},
                    {"Nf3", 20},
                    {"Nh3", 20},
                    {"a3", 20},
                    {"a4", 20},
                    {"b3", 20},
                    {"b4", 20},
                    {"c3", 20},
                    {"c4", 20},
                    {"d3", 20},
                    {"d4", 20},
                    {"e3", 20},
                    {"e4", 20},
                    {"f3", 20},
                    {"f4", 20},
                    {"g3", 20},
                    {"g4", 20},
                    {"h3", 20},
                    {"h4", 20},
                }),
                new PerftRow(8902, 34, 0, 0, 0, 12, 0, new Dictionary<string, int>
                {
                    {"Na3", 400},
                    {"Nc3", 440},
                    {"Nf3", 440},
                    {"Nh3", 400},
                    {"a3", 380},
                    {"a4", 420},
                    {"b3", 420},
                    {"b4", 421},
                    {"c3", 420},
                    {"c4", 441},
                    {"d3", 539},
                    {"d4", 560},
                    {"e3", 599},
                    {"e4", 600},
                    {"f3", 380},
                    {"f4", 401},
                    {"g3", 420},
                    {"g4", 421},
                    {"h3", 380},
                    {"h4", 420},
                }),
                new PerftRow(197281, 1576, 0, 0, 0, 469, 8, new Dictionary<string, int>
                {
                    {"Na3", 8885},
                    {"Nc3", 9755},
                    {"Nf3", 9748},
                    {"Nh3", 8881},
                    {"a3", 8457},
                    {"a4", 9329},
                    {"b3", 9345},
                    {"b4", 9332},
                    {"c3", 9272},
                    {"c4", 9744},
                    {"d3", 11959},
                    {"d4", 12435},
                    {"e3", 13134},
                    {"e4", 13160},
                    {"f3", 8457},
                    {"f4", 8929},
                    {"g3", 9345},
                    {"g4", 9328},
                    {"h3", 8457},
                    {"h4", 9329},
                }),
                new PerftRow(4865609, 82719, 258, 0, 0, 27351, 347, new Dictionary<string, int>
                {
                    {"Na3", 198572},
                    {"Nc3", 234656},
                    {"Nf3", 233491},
                    {"Nh3", 198502},
                    {"a3", 181046},
                    {"a4", 217832},
                    {"b3", 215255},
                    {"b4", 216145},
                    {"c3", 222861},
                    {"c4", 240082},
                    {"d3", 328511},
                    {"d4", 361790},
                    {"e3", 402988},
                    {"e4", 405385},
                    {"f3", 178889},
                    {"f4", 198473},
                    {"g3", 217210},
                    {"g4", 214048},
                    {"h3", 181044},
                    {"h4", 218829},
                }),
                //Due to ram usage and no deduplicacation between boards this eats more ram that i have
                /*new PerftRow(119060324, 2812008, 5248, 0, 0, 809099, 10828, new Dictionary<string, int>
                {
                    {"Na3", 4856835},
                    {"Nc3", 5708064},
                    {"Nf3", 5723523},
                    {"Nh3", 4877234},
                    {"a3", 4463267},
                    {"a4", 5363555},
                    {"b3", 5310358},
                    {"b4", 5293555},
                    {"c3", 5417640},
                    {"c4", 5866666},
                    {"d3", 8073082},
                    {"d4", 8879566},
                    {"e3", 9726018},
                    {"e4", 9771632},
                    {"f3", 4404141},
                    {"f4", 4890429},
                    {"g3", 5346260},
                    {"g4", 5239875},
                    {"h3", 4463070},
                    {"h4", 5385554},
                }), */ 
            });
        }

        private void PerftTest(ChessBoard board, PerftRow[] rows)
        {
            Dictionary<string, int> results = new Dictionary<string, int>();
            List<Tuple<ChessBoard, string>> current = new List<Tuple<ChessBoard, string>> {new Tuple<ChessBoard, string>(board, null)};
            List<Tuple<ChessBoard, ChessMove, string>> nodes = new List<Tuple<ChessBoard, ChessMove, string>>();

            foreach (PerftRow row in rows)
            {
                int captures = 0, ep = 0, castle = 0, promote = 0, check = 0, mate = 0;
                foreach (Tuple<ChessBoard, string> chessBoard in current)
                {
                    foreach (ChessMove move in chessBoard.Item1.ValidMoves)
                    {
                        //results.Increment(move.ToString());
                        nodes.Add(new Tuple<ChessBoard, ChessMove, string>(chessBoard.Item1, move, chessBoard.Item2 ?? move.ToString()));

                        if (move.Capture != ChessPiece.Empty) captures++;
                        if (move.EnPassant) ep++;
                        if (move.Castle) castle++;
                        //if (move.Promotion != null) promote++;
                        if (move.Check) check++;
                        if (move.CheckMate) mate++;
                    }
                }

                TestContext.WriteLine($"         {"Nodes".PadLeft(12)} {"Captures".PadLeft(12)} {"EnPassant".PadLeft(12)} {"Castles".PadLeft(12)} {"Promotions".PadLeft(12)} {"Checks".PadLeft(12)} {"Mates".PadLeft(12)}");
                TestContext.WriteLine($"Expected {row.Nodes,12} {row.Captures,12} {row.EnPessant,12} {row.Castles,12} {row.Promotions,12} {row.Checks,12} {row.Mates,12}");
                TestContext.WriteLine($"Had      {nodes.Count,12} {captures,12} {ep,12} {castle,12} {promote,12} {check,12} {mate,12}");
                TestContext.WriteLine($"Ilegal moves tested = {ChessBoard.counter}");
                TestContext.WriteLine($"gc count = {GC.GetTotalMemory(false)}");
                //TestContext.WriteLine(string.Join("\n",
                //    results.OrderBy(x => x.Key).Select(x => x.Key + "  " + x.Value)));

                if (row.Start != null)
                {
                    var v = nodes.GroupBy(x => x.Item3).ToDictionary(x => x.Key, tuples => tuples.Count());
                    TestContext.WriteLine(string.Join("\n",
                        v.Where(x => x.Value != row.Start[x.Key]).Select(x => $"{x.Key} {x.Value} {row.Start[x.Key]} {x.Value - row.Start[x.Key]}")));
                }
                //TestContext.WriteLine(string.Join("\n",
                //    nodes.GroupBy(x => x.Item3).OrderBy(x => x.Key).Select(x => x.Key + " " + x.Count())));

                
                Assert.AreEqual(row.Nodes, nodes.Count);
                Assert.AreEqual(row.Captures, captures);
                Assert.AreEqual(row.EnPessant, ep);
                Assert.AreEqual(row.Castles, castle);
                Assert.AreEqual(row.Promotions, promote);
                Assert.AreEqual(row.Checks, check);
                Assert.AreEqual(row.Mates, mate);

                TestContext.WriteLine();

                current.Clear();
                results.Clear();
                
                foreach (Tuple<ChessBoard, ChessMove, string> node in nodes)
                {
                    if (row == rows.Last()) break;

                    current.Add(new Tuple<ChessBoard, string>(node.Item1.ExecuteMove(node.Item2), node.Item3));
                }
                nodes.Clear();
            }

            Assert.Pass();
        }

        private class PerftRow
        {
            public Dictionary<string, int> Start { get; set; }
            public int Nodes, Captures, EnPessant, Castles, Promotions, Checks, Mates;

            public PerftRow(int nodes, int captures, int enPessant, int castles, int promotions, int checks, int mates, Dictionary<string, int> start = null)
            {
                Start = start;
                Nodes = nodes;
                Captures = captures;
                EnPessant = enPessant;
                Castles = castles;
                Promotions = promotions;
                Checks = checks;
                Mates = mates;
            }
        }
    }
}