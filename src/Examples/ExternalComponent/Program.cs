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
                int blocksize = 100; //{ 100, 512, 1024, 2000 };
                bool rnd = true;

                new SinglePortBlockRamTester<UInt10>(blocksize, rnd, make_top_level: true);
                new SinglePortBlockRamTester<int>(blocksize, rnd, make_top_level: true);
                new SinglePortBlockRamTester<ulong>(blocksize, rnd, make_top_level: true);

                new SimpleDualPortBlockRamTester<UInt10>(blocksize, rnd, make_top_level: true);
                new SimpleDualPortBlockRamTester<int>(blocksize, rnd, make_top_level: true);
                new SimpleDualPortBlockRamTester<ulong>(blocksize, rnd, make_top_level: true);

                new TrueDualPortBlockRamTester<UInt10>(blocksize, rnd, make_top_level: true);
                new TrueDualPortBlockRamTester<int>(blocksize, rnd, make_top_level: true);
                new TrueDualPortBlockRamTester<ulong>(blocksize, rnd, make_top_level: true);

                rnd = false;

                new SinglePortBlockRamTester<UInt10>(blocksize, rnd, make_top_level: true);
                new SinglePortBlockRamTester<int>(blocksize, rnd, make_top_level: true);
                new SinglePortBlockRamTester<ulong>(blocksize, rnd, make_top_level: true);

                new SimpleDualPortBlockRamTester<UInt10>(blocksize, rnd, make_top_level: true);
                new SimpleDualPortBlockRamTester<int>(blocksize, rnd, make_top_level: true);
                new SimpleDualPortBlockRamTester<ulong>(blocksize, rnd, make_top_level: true);

                new TrueDualPortBlockRamTester<UInt10>(blocksize, rnd, make_top_level: true);
                new TrueDualPortBlockRamTester<int>(blocksize, rnd, make_top_level: true);
                new TrueDualPortBlockRamTester<ulong>(blocksize, rnd, make_top_level: true);

                sim
                    .BuildCSVFile()
                    .BuildGraph()
                    .BuildVHDL()
                    //.BuildCPP()
                    .Run();
            }
        }
    }
}
