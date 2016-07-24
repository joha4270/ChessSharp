using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Framework
{
    public interface IChessHashTable
    {
        ChessMove[] GetHashedMoves(long hash);
        void SetMoveHash(long hash, ChessMove[] moves);
        int MaxSizeMB { get; set; }
        int SizeMB { get; }
    }
}
