using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Framework
{
    public class DefaultHashTable : IChessHashTable
    {
        static DefaultHashTable()
        {
            chessMoveSize = System.Runtime.InteropServices.Marshal.SizeOf<ChessMove>();
        }

        private ReaderWriterLock _lock = new ReaderWriterLock();
        private static int chessMoveSize;
        private int _sizeb;
        private ConcurrentDictionary<long, ChessMove[]> _hashTable = new ConcurrentDictionary<long, ChessMove[]>();

        public ChessMove[] GetHashedMoves(long hash)
        {
            
            ChessMove[] res;
            if (_hashTable.TryGetValue(hash, out res))
            {
                Hit++;
                return res;
            }
            Miss++;
            return null;
        }

        public void SetMoveHash(long hash, ChessMove[] moves)
        {
            if (_hashTable.TryAdd(hash, moves))
            {
                _sizeb += 4 + 8 + (moves.Length*chessMoveSize);
                if (SizeMB > MaxSizeMB)
                {
                    PruneHashTable();
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private void PruneHashTable()
        {
            _hashTable.Clear();
            _sizeb = 0;
            return;
            Random r = new Random();
            var temp = _hashTable.Where(x => r.Next(0,10) != 0).ToList();
            _hashTable.Clear();
            _sizeb = 0;
            foreach (KeyValuePair<long, ChessMove[]> keyValuePair in temp)
            {
                _hashTable.TryAdd(keyValuePair.Key, keyValuePair.Value);
                _sizeb += 4 + 8 + (keyValuePair.Value.Length * chessMoveSize);
            }
            
        }

        public long Hit { get; private set; } = 0;

        public long Miss { get; private set; } = 0;

        public int MaxSizeMB { get; set; } = 1024;

        public int SizeMB => _sizeb/(1024*1024) + 1;
    }
}