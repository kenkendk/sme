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
                var mem = new Memory("programs/fib");
                var tester = new Tester(mem.mem, "programs/fib.output");

                cpu.memout = mem.output;
                mem.input = cpu.memin;
                tester.term = cpu.terminate;

                Simulation.Current
                    .AddTopLevelOutputs(cpu.terminate)
                    .BuildCSVFile()
                    .BuildVHDL()
                    .Run();
            }
        }
    }
}
