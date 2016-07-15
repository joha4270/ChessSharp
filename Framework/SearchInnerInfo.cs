using System.Threading;

namespace Framework
{
    public class SearchInnerInfo
    {
        public readonly CancellationTokenSource ThreadCancel;
        public readonly ChessBoard CurrentBoard;
        public readonly ChessMove[] Moves;
        public readonly CurrentSearchStatus SearchStatus;
        public readonly ManualResetEventSlim Finished = new ManualResetEventSlim(false);

        public SearchInnerInfo(ChessBoard currentBoard, ChessMove[] moves, CurrentSearchStatus searchStatus)
        {
            CurrentBoard = currentBoard;
            Moves = moves;
            SearchStatus = searchStatus;
            ThreadCancel = new CancellationTokenSource();
        }
    }
}