using System;
using SME;
using SME.VHDL;

namespace ExternalComponent
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            var rnd = false;
            using (var sim = new Simulation())
            {
                sim.BuildCSVFile()
                //.BuildGraph()
                .BuildVHDL()
                //.BuildCPP()
               .Run(
                    new BlockRamTester<UInt10, UInt10>(rnd),
                    new BlockRamTester<UInt10, UInt11>(rnd),
                    new BlockRamTester<UInt10, UInt12>(rnd),
                    new BlockRamTester<UInt10, UInt13>(rnd),
                    new BlockRamTester<UInt10, UInt14>(rnd),
                    new BlockRamTester<UInt10, UInt15>(rnd),
                    new BlockRamTester<UInt10, UInt16>(rnd),
                    new BlockRamTester<UInt10, UInt17>(rnd),
                    new BlockRamTester<UInt10, UInt18>(rnd)
               );
            }
        }
    }
}
