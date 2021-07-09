using SME;

namespace SimpleMIPS
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            using (var sim = new Simulation())
            {
                { // Fib
                    var cpu = new CPU();
                    var mem = new Memory("programs/fib");
                    var tester = new Tester(mem.mem, "programs/fib.output");

                    cpu.memout = mem.output;
                    mem.input = cpu.memin;
                    tester.term = cpu.terminate;

                    sim.AddTopLevelOutputs(cpu.terminate);
                }

                { // Instruction tester
                    var cpu = new CPU();
                    var mem = new Memory("programs/instr_test");
                    var tester = new Tester(mem.mem, "programs/instr_test.output");

                    cpu.memout = mem.output;
                    mem.input = cpu.memin;
                    tester.term = cpu.terminate;

                    sim.AddTopLevelOutputs(cpu.terminate);
                }

                sim
                    .BuildCSVFile()
                    .BuildVHDL()
                    .Run();
            }
        }
    }
}
