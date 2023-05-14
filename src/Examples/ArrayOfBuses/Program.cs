using SME;

namespace ArrayOfBuses
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            using (var sim = new Simulation())
            {
                // Set the size of the test
                const int size = 100;

                // Instantiate the processes
                var tester = new ReduceAddTester(size);
                var reduce = new ReduceAdd(size);

                // Connect the processes
                for (int i = 0; i < size; i++)
                {
                    reduce.input[i] = tester.network_input[i];
                }
                tester.network_output = reduce.output;

                // Run the simulation
                sim
                    .AddTopLevelInputs(tester.network_input)
                    .AddTopLevelOutputs(reduce.output)
                    .BuildCSVFile()
                    .BuildGraph()
                    .BuildVHDL()
                    .Run();
            }
        }
    }
}