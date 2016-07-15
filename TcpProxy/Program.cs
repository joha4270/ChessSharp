using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TcpProxy
{
    class Program
    {
        const int BUFFER_SIZE = 4096;
        static void Main(string[] args)
        {
            try
            {
                string hostname;
                int port;
                if (args.Length == 1)
                {
                    if (args[0].Contains(':'))
                    {
                        string[] parts = args[0].Split(':');
                        hostname = parts[0];
                        if (!int.TryParse(parts[1], out port)) return;
                    }
                    else
                    {
                        hostname = "127.0.0.1";
                        if (!int.TryParse(args[0], out port)) return;
                    }
                }
                else
                {
                    hostname = "127.0.0.1";
                    port = 8801;
                }

                TcpClient client = null;

                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        client = new TcpClient(hostname, port);
                        break;
                    }
                    catch (Exception)
                    {
                        Thread.Sleep(1000 * i * i);
                    }
                }
                if (client == null) return;

                Stream stdin = Console.OpenStandardInput();
                Stream stdout = Console.OpenStandardOutput();

                NetworkStream ns = client.GetStream();

                byte[] stdinbuff = new byte[BUFFER_SIZE];
                byte[] stdoutbuff = new byte[BUFFER_SIZE];
                Task<int> waitStdin = null;
                Task<int> waitStdout = null;
                while (client.Connected)
                {
                    waitStdin = waitStdin ?? stdin.ReadAsync(stdinbuff, 0, BUFFER_SIZE);
                    waitStdout = waitStdout ?? ns.ReadAsync(stdoutbuff, 0, BUFFER_SIZE);

                    Task.WaitAny(new Task[]{waitStdin, waitStdout}.Where(x => x != null).ToArray(), 100);

                    if (waitStdout?.IsCompleted ?? false)
                    {
                        stdout.Write(stdoutbuff, 0, waitStdout.Result);
                        waitStdout = null;
                    }

                    if (waitStdin?.IsCompleted ?? false)
                    {
                        ns.Write(stdinbuff, 0, waitStdin.Result);
                        waitStdin = null;
                    }

                }

                Console.WriteLine("info string connection lost");
            }
            catch (Exception ex)
            {
                Console.WriteLine("info string " + ex);
            }
           
        }
    }
}
