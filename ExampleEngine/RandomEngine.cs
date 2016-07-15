using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Framework;

namespace ExampleEngine
{
    class RandomEngine : AbstractChessEngine
    {
        readonly Random _randomGenerator = new Random(    );
        public RandomEngine() : base("RandomChess 3.7", "Johannes Elgaard", new List<AbstractEngineOption>())
        {
        }

        protected override void Search(ChessBoard board, CurrentSearchStatus status, ChessMove[] history, CancellationToken stopRequested)
        {
            //Generate all possible moves from the initial board
            IReadOnlyList<ChessMove> moves = board.ValidMoves;

            //Find a random one of those
            int randomIndex = _randomGenerator.Next(moves.Count);

            //Save the new best move. 
            //Normally the engine would evauluate what move is best by looking at it in depth
            //but doing so is complicated
            status.BestMove = moves[randomIndex];

            //Finding chess moves is hard work. Give the thread some rest
            Thread.Sleep(1000);
            
            //Return from this function. This indicates that we found the best move
            return;
        }
    }
}
