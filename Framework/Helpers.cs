using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework
{
    internal static class Helpers
    {
        public static IEnumerable<int> ValidPositions()
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    yield return 21 + i*10 + j;
                }
            }
        }
    }
}
