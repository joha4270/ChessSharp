using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework
{
    static class RandomExtensions
    {
        public static long NextLong(this Random random)
        {
            byte[] bf = new byte[8];
            random.NextBytes(bf);
            return BitConverter.ToInt64(bf, 0);
        }
    }
}
