using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Tests
{
    internal static class DictionaryExtensions
    {
        public static void Increment<T>(this Dictionary<T, int> value, T key)
        {
            if (value.ContainsKey(key))
            {
                value[key]++;
            }
            else
            {
                value[key] = 1;
            }
        }

    }
}
