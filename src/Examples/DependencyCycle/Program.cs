using System;
using SME;

namespace DependencyCycle
{
    class Program
    {
        static void Main(string[] args)
        {
            // Test an unclocked dependency cycle
            try 
            {
                using (var sim = new Simulation())
                {
                    var dummy = new Dummy();
                    var first = new UnclockedId();
                    new Cycle(first, 10);

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
                    var dummy = new Dummy();
                    var proc = new UnclockedId();

                    sim.Run();
                }
                throw new Exception("The simulation should have thrown an exception");
            }
            catch (SME.DependencyGraph.NoWritingParentsException) { }

            // Test a cycle with a clocked process
            using (var sim = new Simulation())
            {
                var dummy = new Dummy();

                var first = new ClockedId();
                new Cycle(first, 10);

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
