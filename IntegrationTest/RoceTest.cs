using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace IntegrationTest
{
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

        public Dictionary<string, long> DivideResult()
        {
            Dictionary<string, long> result = new Dictionary<string, long>();
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

                result.Add(parts[0], long.Parse(parts[1]));

            }
        }

        

        private void DebugWrite(string info)
        {
            return;
            Console.WriteLine(info);
        }
    }
}