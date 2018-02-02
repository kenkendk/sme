using System;
using SME;
using SME.VHDL;

namespace ExternalComponent
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            using (var sim = new Simulation())
            {
                sim.BuildCSVFile()
                //.BuildGraph()
                .BuildVHDL()
                //.BuildCPP()
               .Run(new BlockRamTester<UInt11, UInt9>());
            }
        }
    }
}
