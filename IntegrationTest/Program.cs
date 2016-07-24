using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Framework;

namespace IntegrationTest
{
    class FenTestCase
    {
        public string Fen { get; }
        public int[] Perft { get; }

        public FenTestCase(string t)
        {
            string[] parts = t.Split(';');
            Fen = parts[0].Trim();

            Perft = parts.Skip(1).Select(x => int.Parse(x.Split(' ')[1])).ToArray();
        }
    }

    class SelfTest
    {
        public static Dictionary<string, int> Test(string fen, int depth)
        {
            Dictionary<string, int> result = new Dictionary<string, int>();

            ChessBoard board = ChessBoard.ParseFen(fen);

            foreach (ChessMove move in board.ValidMoves)
            {
                if (depth == 1)
                {
                    result.Add(move.AlgebraicFrom + move.AlgebraicTo, 1);
                }
                else
                {
                    ChessBoard after = board.ExecuteMove(move);
                    var res = PerformanceUtilities.Perft(depth - 1, after);
                    result.Add(move.AlgebraicFrom + move.AlgebraicTo, res.Item2);
                }
            }

            return result;
        }
    }

    class RoceTest
    {
        private readonly Process _process;
        private StreamReader _stdout;
        private StreamWriter _stdin;
        private int _depth;

        public RoceTest()
        {

            _process = new Process
            {
                StartInfo =
                {
                    FileName = "roce39.exe",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                }
            };
            _process.Start();
    
            _stdout = _process.StandardOutput;
            _stdin = _process.StandardInput;

            WaitRoce();

        }

        private string WaitRoce()
        {
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                int read = _stdout.Read();
                if(read == -1)
                { }
                else
                {
                    sb.Append((char) read);
                    bool state = sb.ToString().EndsWith("roce: "); // & sb[sb.Length -1] == Environment.NewLine.Last();
                    if (state)
                    {
                        DebugWrite(">>>>\"" + sb + "\"");
                        return sb.ToString();
                    }
                }
            }
        }

        public void SetBoard(string fen)
        {
            

            string a = $"setboard {fen} {_stdin.NewLine}";
            _stdin.Write(a);
            //_stdin.Write();
            DebugWrite(">>>>\"" + a + "\"");
            _stdin.Flush();
            string dbg = WaitRoce();
            
        }

        public void StartDivide(int depth)
        {
            _depth = depth;
            string b = $"divide {depth}";
            _stdin.WriteLine(b);
            DebugWrite(">>>>\"" + b + "\"");
            _stdin.Flush();
        }

        public Dictionary<string, int> DivideResult()
        {
            Dictionary<string, int> result = new Dictionary<string, int>();
            bool past = false;
            Queue<string> buffer = new Queue<string>();
            while (true)
            {
                string line;
                if (buffer.Count == 0)
                {
                    line = _stdout.ReadLine();
                }
                else
                {
                    line = buffer.Dequeue();
                }
                DebugWrite("<<<<\"" + line + "\"");

                string[] parts = line.Split(' ');

                if (line == "") continue;


                if (parts[0] == "Moves:"  || parts[0] == "Nodes:")
                {
                    WaitRoce();
                    return result;
                }

                result.Add(parts[0], int.Parse(parts[1]));

            }
        }

        

        private void DebugWrite(string info)
        {
            return;
            Console.WriteLine(info);
        }
    }

    class Tree<T>
    {
        public Tree(T value)
        {
            Value = value;
        }

        public T Value { get; }
        public List<Tree<T>> Children { get; } = new List<Tree<T>>();

        private void Print(int depth, bool top, StringBuilder sb)
        {
            if (!top)
            {
                for (int i = 0; i < depth; i++)
                {
                    sb.Append(' ');
                }
            }

            sb.Append(Value);
            depth += Value.ToString().Length;
            if (Children.Count == 0)
            {
                sb.Append(Environment.NewLine);
            }
            else
            {
                sb.Append(" -> ");
                depth += 4;
                Children[0].Print(depth, true, sb);
                for (int i = 1; i < Children.Count; i++)
                {
                    Children[i].Print(depth, false, sb);
                }
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            Print(0, true, sb);

            return sb.ToString();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            int maxDepth = 10;
            Console.SetWindowSize(120, 25);
            Console.SetBufferSize(120, 300);
            List<FenTestCase> tests = File.ReadAllLines("perftsuite.epd").Select(x => new FenTestCase(x)).ToList();
            RoceTest roce = new RoceTest();

            foreach (FenTestCase testCase in tests)
            {
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
                        Console.Write($" {i+1}");
                    }
                    else
                    {
                        var errorTree = Wrong(own, res, testCase.Fen, roce, i + 1 );
                        Console.WriteLine();
                        foreach (Tree<string> tree in errorTree)
                        {
                            Console.WriteLine(tree);
                        }
                        Console.Read();
                        return;
                    }

                    
                }

                Console.WriteLine();
                Console.WriteLine("Passed");
                Console.WriteLine();

            }

            Console.Read();
        }

        private static List<Tree<string>> Wrong(Dictionary<string, int> own, Dictionary<string, int> expected, string fen, RoceTest roce, int depth)
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
                foreach (KeyValuePair<string, int> pair in own)
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

        private static bool Passing(Dictionary<string, int> result, Dictionary<string, int> expected, int expectedTotal)
        {
            int resultTotal = result.Select(x => x.Value).Aggregate((x, y) => x + y);
            if (resultTotal != expectedTotal)
                return false;

            foreach (KeyValuePair<string, int> keyValuePair in result)
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
