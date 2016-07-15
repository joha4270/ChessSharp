using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ThreadState = System.Threading.ThreadState;

namespace Framework
{
    public abstract class AbstractChessEngine
    {
        /// <summary>
        /// Run the chess engine with UCI using the console as interface. 
        /// This will block the calling thread until the engine exits.
        /// Calls to Engine Functions are not gaurentheth to be on the same thread as the one calling this function.
        /// </summary>
        public void RunConsole()
        {
            Stream stdin = Console.OpenStandardInput();
            Stream stdout = Console.OpenStandardOutput();

            RunInner(new StreamReader(stdin), new StreamWriter(stdout));
        }

        /// <summary>
        /// Run the chess engine with UCI using a TCP connection as interface.
        /// This will block the calling thread until the engine exits.
        /// Calls to Engine Functions are not gaurentheth to be on the same thread as the one calling this function.
        /// </summary>
        /// <param name="port">The Port to listen on</param>
        public void RunTcp(int port)
        {
            TcpListener listner = null;
            TcpClient client = null;
            try
            {
                _consoleFree = true;
                LogLine($"Listening for clients on {port}");
                listner = TcpListener.Create(port);
                listner.Start();

                client = listner.AcceptTcpClient();
                LogLine("Client connected");
                listner.Stop();
                Stream innerStream = client.GetStream();

                RunInner(new StreamReader(innerStream), new StreamWriter(innerStream));
            }
            finally
            {
                listner?.Stop();
                client?.Close();
            }
        }

        /// <summary>
        /// If true, the engine should print additional debug info
        /// </summary>
        protected bool Debug { get; private set; }

        /// <summary>
        /// Sends information to the gui
        /// </summary>
        /// <param name="info">The information to send</param>
        protected void WriteInfo(string info)
        {
            _writeQueue.Enqueue(info);
        }

        /// <summary>
        /// Sends information to the gui, if debug mode is on
        /// </summary>
        /// <param name="debug">The debug informartion to send</param>
        protected void WriteDebug(string debug)
        {
            if(Debug)
                WriteInfo(debug);
        }

        /// <summary>
        /// Tells the engine to start searching for the best move. Once it has found the best move it should be saved in status and this function should return
        /// </summary>
        /// <param name="board">The ChessBoard</param>
        /// <param name="status">A class for comunicating with the running engine. 
        /// The engine should periodically update this with the latest best move and search details. 
        /// If the search is interupted, it will take a move from status or fortifeit the game.</param>
        /// <param name="history">A history of moves that was made to the board. The board before all those moves might not match a starting board</param>
        /// <param name="stopRequested">Tells the engine if it is requested to cancel searching. If it does not do so in a timely manner, it is forcefully interupted</param>
        protected abstract void Search(ChessBoard board, CurrentSearchStatus status, ChessMove[] history, CancellationToken stopRequested);

        /// <summary>
        /// This method will be called if the game that the engine is supposed to be playing is changed. This is not garenteeth to be called
        /// </summary>
        protected virtual void GameChanged() { }

        /// <summary>
        /// Setup a new engine
        /// </summary>
        /// <param name="name">The name and version of the chess engine</param>
        /// <param name="author">The name of the person who wrote the chess engine</param>
        /// <param name="options">A list options for the engine, that can be set from the gui</param>
        protected AbstractChessEngine(string name, string author, List<AbstractEngineOption> options = null)
        {
            _name = name;
            _author = author;
            _options = options?.ToDictionary(x => x.Name) ?? new Dictionary<string, AbstractEngineOption>();
            
        }

        protected void Log(string log)
        {
            if(_consoleFree)
                Console.Write(log);
        }

        /// <summary>
        /// Writes a line to a log somewhere. No gurantee that said log exists
        /// </summary>
        /// <param name="log"></param>
        protected void LogLine(string log)
        {
            if (_consoleFree)
            {
                Log(log);
                Log(Environment.NewLine);
            }
        }

        //begin implementation
        private readonly object _syncRoot = new object();
        private bool _consoleFree; //TODO: should be same way Debug/Trace works with a consolewriter
        private readonly string _name;
        private readonly string _author;
        private readonly Dictionary<string, AbstractEngineOption> _options;
        private readonly ConcurrentQueue<string> _writeQueue = new ConcurrentQueue<string>();
        private readonly ConcurrentQueue<LaterAction> _mainThreadQueue = new ConcurrentQueue<LaterAction>();
        private Thread _otherThread;

        
        private readonly object _searchSync = new object();
        private readonly ManualResetEventSlim _startSearchEvent = new ManualResetEventSlim(false);
        private readonly ManualResetEventSlim _searchFinishedEvent = new ManualResetEventSlim(false);
        private SearchInnerInfo _searchInnerInfo;
        
        private SearchInnerInfo SearchInnerInfo
        {
            get { lock(_searchSync) { return _searchInnerInfo;} }
            set
            {
                lock (_searchSync)
                {
                    _searchInnerInfo?.ThreadCancel.Cancel();
                    _searchInnerInfo = value;
                    //_startSearchEvent.Set();
                }
            }
         }

        private readonly object _engineStateLock = new object();
        private EngineState _engineState = EngineState.Idle;
        private EngineState State
        {
            get { lock(_engineStateLock) { return _engineState;} }
            set { lock (_engineStateLock) { _engineState = value;} }
        }


        private void RunInner(TextReader recive, TextWriter write)
        {
            string connect = recive.ReadLine();

            if (connect == "uci")
            {
                RunUci(recive, write);
            }
            else
            {
                throw new UnsupportedProtocolException("Only UCI is supported");
            }
        }

        private void RunUci(TextReader recive, TextWriter write)
        {
            write.WriteLine("id name " + _name);
            write.WriteLine("id author " + _author);

            //TODO: ensure default options are present

            foreach (KeyValuePair<string, AbstractEngineOption> option in _options)
            {
                write.WriteLine(option.Value.Present());
            }

            //TODO: init support code

            //Todo: Setup message handling loop

            write.WriteLine("uciok");
            write.Flush();

            Task<string> inString = null;

            while (true)
            {
                if (inString?.IsCompleted ?? false)
                {
                    string command = inString.Result;
                    inString = null;
                    //Handle command
                    //TODO: setup engine working

                    LogLine(">" + command);

                    string[] commandParts = command.Split((char[]) null, StringSplitOptions.RemoveEmptyEntries);
                    switch (commandParts[0])
                    {
                        case "isready":
                            Log("heart");
                            write.WriteLine("readyok");
                            LogLine("beat...");
                            break;

                        case "quit":
                            AskQuit(TimeSpan.FromMilliseconds(5000));
                            break;

                        case "setoption":
                        {
                            if (commandParts.Length == 1)
                            {
                                LogLine("ERROR: setoption missing name");
                            }
                            else
                            {
                                StringBuilder sb = new StringBuilder();
                                bool name = true;
                                int count = 2;
                                while (commandParts.Length > count && name)
                                {
                                    if (commandParts[count] != "value")
                                    {
                                        sb.Append(commandParts[count]);
                                        sb.Append(' ');
                                    }
                                    else
                                    {
                                        name = false;
                                    }
                                }
                                //TODO: rewrite to use string.Split("name")
                                string value = string.Join(" ", commandParts.Skip(count));

                                AbstractEngineOption option;
                                if (_options.TryGetValue(sb.ToString(), out option))
                                {
                                    option.Public = value;
                                }
                                else
                                {
                                    LogLine("ERROR: unknown option");
                                }
                            }
                        }
                            break;

                        case "debug":
                            Debug = commandParts[1] == "on";
                            break;

                        case "stop":
                            AskQuit(TimeSpan.FromMilliseconds(1000));
                            break;

                        case "ucinewgame":
                            if (State != EngineState.Idle) Panic("new game while searching!");
                            AskQuit(TimeSpan.FromMilliseconds(1000));
                            AskReset();
                            break;

                        case "position":
                        {
                            //TODO: just update current if that is all that happened
                            ChessBoard initial;
                            int skip;
                            if (commandParts[1] == "startpos")
                            {
                                initial = new ChessBoard();
                                skip = 3;
                            }
                            else
                            {
                                string fen = string.Join(" ", commandParts.Skip(2).Take(6));
                                LogLine("playing fen " + fen);
                                initial = ChessBoard.ParseFen(fen);
                                skip = 8;
                            }

                            List<ChessMove> moves = new List<ChessMove>();
                                Stopwatch wtf = Stopwatch.StartNew();
                            foreach (string move in commandParts.Skip(skip))
                            {
                                Stopwatch sw2 = Stopwatch.StartNew();
                                IReadOnlyList<ChessMove> chessMoves = initial.ValidMoves;
                                    LogLine($"  Generated in {sw2.Elapsed}"); sw2.Restart();
                                    ChessMove next = chessMoves.First(m => m.AlgebraicFrom + m.AlgebraicTo == move);
                                LogLine($"  Matched in {sw2.Elapsed}");
                                sw2.Restart();
                                initial = initial.ExecuteMove(next);
                                LogLine($"  Executed in {sw2.Elapsed}");

                                moves.Add(next);
                                //TODO: this can theoretically throw an exception, but in that case move generation and GUI are in disagreement about what moves are possible
                                //Aka shit is FUBAR for new
                            }
                            LogLine($"Parsed in {wtf.Elapsed}");

                            SearchInnerInfo = new SearchInnerInfo(initial, moves.ToArray(), new CurrentSearchStatus());
                        }
                            break;

                        case "go":
                        {
                            //TODO: actually handle this
                            AskCalculate();
                        }
                            break;

                        default:
                            LogLine("ERROR: unknown command: " + command);
                            break;
                    }
                }

                if (inString == null)
                    inString = recive.ReadLineAsync();

                string writeMsg;
                if (_writeQueue.TryDequeue(out writeMsg))
                {
                    write.Write("info string ");
                    write.WriteLine(writeMsg);
                }

                EnsureThread();

                LaterAction later;
                if (_mainThreadQueue.TryPeek(out later))
                {
                    if (later.When > DateTime.Now && _mainThreadQueue.TryDequeue(out later))
                    {
                        later.Action();
                    }
                }

                if (_searchFinishedEvent.IsSet)
                {
                    _searchFinishedEvent.Reset();
                    ChessMove? own = SearchInnerInfo.SearchStatus.BestMove;
                    ChessMove? ponder = SearchInnerInfo.SearchStatus.EnemyGuess;

                    if (own != null)
                    {
                        write.Write("bestmove ");
                        write.Write(own.Value.AlgebraicFrom + own.Value.AlgebraicTo);
                        LogLine("moving " + own.Value.AlgebraicFrom + own.Value.AlgebraicTo);
                        if (ponder != null)
                        {
                            write.Write(" ponder ");
                            write.Write(ponder.Value.AlgebraicFrom + ponder.Value.AlgebraicTo);
                            LogLine("ponder " + ponder.Value.AlgebraicFrom + ponder.Value.AlgebraicTo);
                        }
                        write.WriteLine();
                    }
                }

                write.Flush();
                //TODO: send info periodically

            }
        }

        private void WorkerEntryPoint()
        {
            

            while (true)
            {
                try
                {
                    State = EngineState.Idle;
                    _startSearchEvent.Wait(); 
                    _startSearchEvent.Reset();
                    State = EngineState.CalculatingSearch;
                    Search(SearchInnerInfo.CurrentBoard, SearchInnerInfo.SearchStatus, SearchInnerInfo.Moves, SearchInnerInfo.ThreadCancel.Token);
                    State = EngineState.Idle;
                    SearchInnerInfo.Finished.Set();
                    _searchFinishedEvent.Set();
                }
                catch (Exception ex) when(!(ex is ThreadAbortException))
                {
                    Panic(ex.ToString());
                }
            }
        }

        private void Panic(string information)
        {
            WriteInfo("ERROR: recived potentially dangerous command" + information);
            LogLine("ERROR: recived potentially dangerous command" + information);
            if (Debugger.IsAttached)
                Debugger.Break();
        }

        private void EnsureThread()
        {
            if (_otherThread == null || _otherThread.ThreadState == ThreadState.Aborted)
            {
                bool n = _otherThread == null;
                _otherThread = new Thread(WorkerEntryPoint);
                _otherThread.Start();

                //if(!n)
            }
            
            //TODO: do something for 0 (NOW?) and <0 (just ask?)
        }

        private void AskReset()
        {
            _mainThreadQueue.Enqueue(new LaterAction(DateTime.Now,GameChanged));
        }

        private void AskCalculate()
        {
            _startSearchEvent.Set();
        }

        private void AskQuit(TimeSpan deadline)
        {
            if (deadline > TimeSpan.Zero)
            {
                _searchInnerInfo?.ThreadCancel.CancelAfter(deadline);
                if (!(_searchInnerInfo?.Finished.Wait(deadline) ?? true))
                {
                    _otherThread.Abort();
                    _searchFinishedEvent.Set();
                    _otherThread = new Thread(WorkerEntryPoint);
                    _otherThread.Start();
                }
                //_mainThreadQueue.Enqueue(new LaterAction(DateTime.Now + deadline, () =>));
                //_mainThreadQueue.Enqueue(new LaterAction(DateTime.Now + deadline + TimeSpan.FromMilliseconds(1), () =>
                //{
                //    EnsureThread();
                //}));
            }
            //throw new NotImplementedException();
        }
    }

    internal class LaterAction
    {
        public DateTime When { get; set; }
        public Action Action { get; set; }

        public LaterAction(DateTime when, Action action)
        {
            if(action == null) throw new ArgumentNullException();
            When = when;
            Action = action;
        }
    }

    internal enum EngineState
    {
        Idle,
        CalculatingSearch,
        CalculatingPonder
    }
}
