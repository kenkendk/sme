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
                var blocksize = 4096;
                
                sim.BuildCSVFile()
                //.BuildGraph()
                .BuildVHDL()
                //.BuildCPP()
               .Run(
                    //new SinglePortBlockRamTester<UInt10>(blocksize, rnd),
                    //new SinglePortBlockRamTester<UInt11>(blocksize, rnd),
                    //new SinglePortBlockRamTester<UInt12>(blocksize, rnd),
                    //new SinglePortBlockRamTester<UInt13>(blocksize, rnd),
                    //new SinglePortBlockRamTester<UInt14>(blocksize, rnd),
                    //new SinglePortBlockRamTester<UInt15>(blocksize, rnd),
                    //new SinglePortBlockRamTester<UInt16>(blocksize, rnd),
                    //new SinglePortBlockRamTester<UInt17>(blocksize, rnd),
                    //new SinglePortBlockRamTester<UInt18>(blocksize, rnd),
                    //new SinglePortBlockRamTester<UInt18>(blocksize, rnd),

                    new SimpleDualPortBlockRamTester<UInt10>(blocksize, rnd)
                    //new SimpleDualPortBlockRamTester<UInt11>(blocksize, rnd),
                    //new SimpleDualPortBlockRamTester<UInt12>(blocksize, rnd),
                    //new SimpleDualPortBlockRamTester<UInt13>(blocksize, rnd),
                    //new SimpleDualPortBlockRamTester<UInt14>(blocksize, rnd),
                    //new SimpleDualPortBlockRamTester<UInt15>(blocksize, rnd),
                    //new SimpleDualPortBlockRamTester<UInt16>(blocksize, rnd),
                    //new SimpleDualPortBlockRamTester<UInt17>(blocksize, rnd),
                    //new SimpleDualPortBlockRamTester<UInt18>(blocksize, rnd),
                    //new SimpleDualPortBlockRamTester<UInt18>(blocksize, rnd),

                    //new TrueDualPortBlockRamTester<UInt10>(blocksize, rnd),
                    //new TrueDualPortBlockRamTester<UInt11>(blocksize, rnd),
                    //new TrueDualPortBlockRamTester<UInt12>(blocksize, rnd),
                    //new TrueDualPortBlockRamTester<UInt13>(blocksize, rnd),
                    //new TrueDualPortBlockRamTester<UInt14>(blocksize, rnd),
                    //new TrueDualPortBlockRamTester<UInt15>(blocksize, rnd),
                    //new TrueDualPortBlockRamTester<UInt16>(blocksize, rnd),
                    //new TrueDualPortBlockRamTester<UInt17>(blocksize, rnd),
                    //new TrueDualPortBlockRamTester<UInt18>(blocksize, rnd),
                    //new TrueDualPortBlockRamTester<UInt18>(blocksize, rnd),
               );
            }
        }
    }
}
