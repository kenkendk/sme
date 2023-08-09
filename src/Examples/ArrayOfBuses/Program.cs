using SME;

namespace ArrayOfBuses
{
    public static class Constants
    {
        public const int SIZE = 3;
        public static Random rng = new Random();
    }

    public static class Helpers
    {
        // Helper function to fill an array with random data
        public static void fill(ref int[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = Constants.rng.Next(0, 100);
            }
        }

        public static int sum(int[] data)
        {
            int sum = 0;
            for (int i = 0; i < data.Length; i++)
            {
                sum += data[i];
            }
            return sum;
        }
    }

    class MainClass
    {
        public static void Main(string[] args)
        {
            using (var sim = new Simulation())
            {
                // -----
                // First test: one to one proc with array of buses for input
                // -----
                // Instantiate the processes
                var reduce = new ReduceAdd();
                var reduce_tester = new ReduceAddTester();

                // Connect the processes
                for (int i = 0; i < Constants.SIZE; i++)
                {
                    reduce.input[i] = reduce_tester.network_input[i];
                }
                reduce_tester.network_output = reduce.output;

                // -----
                // Second test: one to many proc with array of buses for input
                // -----

                // -----
                // Third test: many to one proc with array of buses for input
                // -----

                // -----
                // Fourth test: many to many proc with array of buses for input
                // -----
                var idents = new Identity[Constants.SIZE];
                for (int i = 0; i < Constants.SIZE; i++)
                    idents[i] = new Identity(i);
                var splitter_tester = new SplitterTester();
                for (int i = 0; i < Constants.SIZE; i++)
                {
                    // The problem is that during process parsing, a bus might
                    // be registered in the network bus map as an array of buses
                    // and then later be used as a single bus. This is a problem,
                    // as some of the references will point to the array, which
                    // might not be the case, as one process is using it as a
                    // single bus.
                    splitter_tester.network_output[i] = idents[i].output;
                    idents[i].input = splitter_tester.network_input[i];
                }

                var broadcaster = new Broadcaster();
                var idents2 = new Identity[Constants.SIZE];
                for (int i = 0; i < Constants.SIZE; i++)
                    idents2[i] = new Identity(i);
                var reduce2 = new ReduceAdd(new int[Constants.SIZE].Select(x => 0).ToArray());
                var broadcaster_tester = new BroadcasterTester();
                broadcaster.input = broadcaster_tester.network_input;
                for (int i = 0; i < Constants.SIZE; i++)
                {
                    idents2[i].input = broadcaster.output[i];
                    reduce2.input[i] = idents2[i].output;
                }
                broadcaster_tester.network_output = reduce2.output;

                // Build the top level inputs and outputs
                List<IBus> top_level_inputs = new List<IBus>();
                top_level_inputs.AddRange(reduce_tester.network_input);
                top_level_inputs.AddRange(splitter_tester.network_input);
                top_level_inputs.Add(broadcaster_tester.network_input);

                List<IBus> top_level_outputs = new List<IBus>();
                top_level_outputs.Add(reduce.output);
                top_level_outputs.AddRange(splitter_tester.network_output);
                top_level_outputs.Add(broadcaster_tester.network_output);

                // Run the simulation
                sim
                    .AddTopLevelInputs(top_level_inputs.ToArray())
                    .AddTopLevelOutputs(top_level_outputs.ToArray())
                    .BuildCSVFile()
                    .BuildGraph()
                    .BuildVHDL()
                    .Run();
            }
        }
    }
}