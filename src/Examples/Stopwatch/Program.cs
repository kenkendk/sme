using System;
using SME;

namespace Stopwatch
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            new Simulation()
                .BuildCSVFile()
                .BuildVHDL()
                .Run(
                    new Stopwatch(),
                    new Counter(),
                    new Tester());
        }
    }
}
