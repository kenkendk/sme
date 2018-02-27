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
                    new SinglePortBlockRamTester<UInt10, UInt10>(rnd),
                    new SinglePortBlockRamTester<UInt10, UInt11>(rnd),
                    new SinglePortBlockRamTester<UInt10, UInt12>(rnd),
                    new SinglePortBlockRamTester<UInt10, UInt13>(rnd),
                    new SinglePortBlockRamTester<UInt10, UInt14>(rnd),
                    new SinglePortBlockRamTester<UInt10, UInt15>(rnd),
                    new SinglePortBlockRamTester<UInt10, UInt16>(rnd),
                    new SinglePortBlockRamTester<UInt10, UInt17>(rnd),
                    new SinglePortBlockRamTester<UInt10, UInt18>(rnd),

                    new SimpleDualPortBlockRamTester<UInt10, UInt10>(rnd),
                    new SimpleDualPortBlockRamTester<UInt10, UInt11>(rnd),
                    new SimpleDualPortBlockRamTester<UInt10, UInt12>(rnd),
                    new SimpleDualPortBlockRamTester<UInt10, UInt13>(rnd),
                    new SimpleDualPortBlockRamTester<UInt10, UInt14>(rnd),
                    new SimpleDualPortBlockRamTester<UInt10, UInt15>(rnd),
                    new SimpleDualPortBlockRamTester<UInt10, UInt16>(rnd),
                    new SimpleDualPortBlockRamTester<UInt10, UInt17>(rnd),
                    new SimpleDualPortBlockRamTester<UInt10, UInt18>(rnd),

                    new TrueDualPortBlockRamTester<UInt10, UInt10>(rnd),
                    new TrueDualPortBlockRamTester<UInt10, UInt11>(rnd),
                    new TrueDualPortBlockRamTester<UInt10, UInt12>(rnd),
                    new TrueDualPortBlockRamTester<UInt10, UInt13>(rnd),
                    new TrueDualPortBlockRamTester<UInt10, UInt14>(rnd),
                    new TrueDualPortBlockRamTester<UInt10, UInt15>(rnd),
                    new TrueDualPortBlockRamTester<UInt10, UInt16>(rnd),
                    new TrueDualPortBlockRamTester<UInt10, UInt17>(rnd),
                    new TrueDualPortBlockRamTester<UInt10, UInt18>(rnd)
               );
            }
        }
    }
}
