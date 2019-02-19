using System;
using SME;

namespace SimpleMIPS
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            using (new Simulation()) 
            {
                var cpu = new CPU();
                var mem = new Memory("fib");
                var tester = new Tester();

                Simulation.Current
                          .BuildCSVFile()
                          .BuildVHDL()
                          .Run();
            }
        }
    }
}
