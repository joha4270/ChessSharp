using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Framework;

namespace IntegrationTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string wstring;

            if (args.Length != 0)
            {
                wstring = args[0];
            }
            else
            {
                wstring = Console.ReadLine();
            }
            switch (wstring)
            {
                default:
                case "selftest":
                    TestEngine();
                    break;
                case "performance":
                    TestPerformance();
                    break;
            }

        }

        private static void TestPerformance()
        {
            long[] perftResult =
            {
                1, 20, 400, 8902, 197281, 4865609, 119060324, 3195901860, 84998978956, 2439530234167,
                69352859712417, 2097651003696806, 62854969236701747, 1981066775000396239
            };

            if (ChessBoard.HashTableImplentation == null)
            {
                Console.WriteLine("Not using a HashTable");
            }
            else
            {
                Console.WriteLine("Using {0} as HashTable", ChessBoard.HashTableImplentation.GetType());
            }

            for (int depth = 1;; depth++)
            {
                Console.WriteLine("Doing Perft for depth {0}", depth);

                var result = PerformanceUtilities.Perft(depth);
                int knps = (int) (result.Item2/result.Item1.TotalSeconds)/1000;

                Console.WriteLine("{0} nodes at depth {1} examined in {2:c}. {3}k Nodes per Second", result.Item2, depth,
                    result.Item1.ToString(@"m\:ss"), knps);
                if (perftResult.Length > depth && perftResult[depth] != result.Item2)
                {
                    Console.WriteLine("ERROR: Expected {0} nodes", perftResult[depth]);
                }
                DefaultHashTable hashTable = ChessBoard.HashTableImplentation as DefaultHashTable;
                if (hashTable != null)
                {
                    double ratio = hashTable.Hit/(double) (hashTable.Hit + hashTable.Miss);
                    Console.WriteLine("Hash table size {0}MB. {1} hits {2} miss ({3})", hashTable.SizeMB, hashTable.Hit,
                        hashTable.Miss, ratio);
                }

                Console.WriteLine();
            }
        }

        private static void TestEngine()
        {
            int maxDepth = 10;
            Console.SetWindowSize(120, 25);
            Console.SetBufferSize(120, 300);
            Console.CursorLeft = 0;
            List<FenTestCase> tests = File.ReadAllLines("perftsuite.epd").Select(x => new FenTestCase(x)).ToList();
            RoceTest roce = new RoceTest();

            foreach (FenTestCase testCase in tests)
            {
                bool wrong = false;
                Console.Write($"Testing \"{testCase.Fen}\" in depth...");
                roce.SetBoard(testCase.Fen);
                for (int i = 0; i < testCase.Perft.Length; i++)
                {
                    if (i > maxDepth) break;

                    roce.StartDivide(i + 1);
                    var own = SelfTest.Test(testCase.Fen, i + 1);
                    var res = roce.DivideResult();

                    if (Passing(own, res, testCase.Perft[i]))
                    {
                        Console.Write($" {i + 1}");
                    }
                    else
                    {
                        var errorTree = Wrong(own, res, testCase.Fen, roce, i + 1);
                        Console.WriteLine();
                        foreach (Tree<string> tree in errorTree)
                        {
                            Console.WriteLine(tree);
                        }
                        Console.Read();
                        wrong = true;
                        break;
                    }
                }

                if (wrong == false)
                {
                    Console.WriteLine();
                    Console.WriteLine("Passed");
                }
                Console.WriteLine();
            }

            Console.Read();
        }

        private static List<Tree<string>> Wrong(Dictionary<string, long> own, Dictionary<string, long> expected, string fen, RoceTest roce, int depth)
        {

            var extra = own.Where(x => !expected.ContainsKey(x.Key)).Select(x => x.Key).ToList();
            var missing = expected.Where(x => !own.ContainsKey(x.Key)).Select(x => x.Key).ToList();

            List<Tree<string>> ret = new List<Tree<string>>();
            if (extra.Count > 0 || missing.Count > 0)
            {
                foreach (string s in extra)
                {
                    ret.Add(new Tree<string>("+[" + s + "]"));
                }

                foreach (string s in missing)
                {
                    ret.Add(new Tree<string>("-[" + s + "]"));
                }

                
            }
            else
            {
                foreach (KeyValuePair<string, long> pair in own)
                {
                    if (expected[pair.Key] != pair.Value)
                    {
                        Tree<string> abc = new Tree<string>(pair.Key);
                        ChessBoard board = ChessBoard.ParseFen(fen);
                        board =
                            board.ExecuteMove(board.ValidMoves.First(x => x.AlgebraicFrom + x.AlgebraicTo == pair.Key));

                        string f2 = board.ToFen();
                        roce.SetBoard(f2);
                        roce.StartDivide(depth - 1);
                        var self = SelfTest.Test(f2, depth - 1);
                        var real = roce.DivideResult();
                        foreach (var VARIABLE in Wrong(self, real, f2, roce, depth - 1))
                        {
                            abc.Children.Add(VARIABLE);
                        }

                        ret.Add(abc);
                    }
                }
            }

            return ret;
        }

        private static bool Passing(Dictionary<string, long> result, Dictionary<string, long> expected, long expectedTotal)
        {
            long resultTotal = result.Select(x => x.Value).Aggregate((x, y) => x + y);
            if (resultTotal != expectedTotal)
                return false;

            foreach (KeyValuePair<string, long> keyValuePair in result)
            {
                if (expected.ContainsKey(keyValuePair.Key))
                {
                    if (keyValuePair.Value != expected[keyValuePair.Key])
                        return false;
                }
                else
                {
                    return false;
                }
            }

            return expected.All(x => result.ContainsKey(x.Key));
        }
    }
}
