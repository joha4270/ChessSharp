using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExampleEngine
{
    class Program
    {
        static void Main(string[] args)
        {
            RandomEngine engine = new RandomEngine();
            engine.RunTcp(8801);
        }
    }
}
