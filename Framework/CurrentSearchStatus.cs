using System.Collections.Generic;

namespace Framework
{
    public class CurrentSearchStatus
    {
        /// <summary>
        /// Current best possible move as far as the engine is aware. Make sure to update this regularily either directly or by calling BestLine
        /// </summary>
        public ChessMove? BestMove { get; set; }

        /// <summary>
        /// Current best move the enemy can make afterwards as far as the engine is aware
        /// </summary>
        public ChessMove? EnemyGuess { get; set; }

        /// <summary>
        /// Current guess on how the game is going to play out. The engine can ignore this
        /// </summary>
        /// <param name="moveLine">An enumerable of future moves as the engine estimates it</param>
        public void BestLine(IEnumerable<ChessMove> moveLine) { }


    }
}