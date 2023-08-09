using System;
using SME;
using SME.VHDL;

namespace BitWidth
{
    public class MainClass
    {
        public static void Main()
        {
            using (var sim = new Simulation())
            {
                var add = new Add();
                var add_tester = new AddTester(100, (a, b) => (SME.VHDL.UInt10)(a + b));

                add.a = add_tester.network_a;
                add.b = add_tester.network_b;
                add_tester.network_c = add.c;

                sim
                    .AddTopLevelInputs(add_tester.network_a, add_tester.network_b)
                    .AddTopLevelOutputs(add_tester.network_c)
                    .BuildCSVFile()
                    .BuildVHDL()
                    .Run();
            }
        }
    }
}
