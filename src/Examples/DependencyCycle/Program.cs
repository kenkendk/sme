using System;
using SME;

namespace DependencyCycle
{
    class Program
    {
        static void Main(string[] args)
        {
            // The sizes to test
            int num_procs = 10;
            int num_cycles = 100;

            // Test an unclocked dependency cycle
            try
            {
                using (var sim = new Simulation())
                {
                    var dummy = new Dummy(num_cycles);
                    var first = new UnclockedId();
                    new Cycle(first, num_procs);

                    sim.Run();
                }
                throw new Exception("The simulation should have thrown an exception.");
            }
            catch (SME.DependencyGraph.UnclockedCycleException) { }

            // Test an unclocked process with no parents
            try
            {
                using (var sim = new Simulation())
                {
                    var dummy = new Dummy(num_cycles);
                    var proc = new UnclockedId();

                    sim.Run();
                }
                throw new Exception("The simulation should have thrown an exception");
            }
            catch (SME.DependencyGraph.NoWritingParentsException) { }

            // Test a cycle with a clocked process
            using (var sim = new Simulation())
            {
                var dummy = new Dummy(num_cycles);

                var first = new ClockedId();
                new Cycle(first, num_procs);

                sim
                    .BuildCSVFile()
                    .BuildVHDL()
                    .Run();
            }
        }
    }

    public class Cycle
    {
        public Cycle(Id start, int count)
        {
            var last = start;
            for (int i = 0; i < count; i++)
            {
                var current = new UnclockedId();
                current.input = last.output;
                last = current;
            }
            start.input = last.output;
        }
    }

}
