using SME;

namespace ArrayOfBuses
{
    /// Tests many to one buses
    public class ReduceAdd : SimpleProcess
    {
        [InputBus]
        public ValueBus[] input;

        [OutputBus]
        public ValueBus output = Scope.CreateBus<ValueBus>();

        public ReduceAdd(int size)
        {
            input = new ValueBus[size];
            bias = new int[size];
            for (int i = 0; i < size; i++)
            {
                bias[i] = i;
            }
        }

        private int[] bias;

        protected override void OnTick()
        {
            int sum = 0;
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i].valid)
                    sum += input[i].Value + bias[i];
            }
            output.valid = input[0].valid;
            output.Value = sum;
        }
    }

    public class ReduceAddTester : SimulationProcess
    {
        [OutputBus]
        public ValueBus[] network_input;

        [InputBus]
        public ValueBus network_output;

        public ReduceAddTester(int size)
        {
            network_input = new ValueBus[size];
            for (int i = 0; i < size; i++)
            {
                network_input[i] = Scope.CreateBus<ValueBus>();
            }
        }

        private Random rng = new Random();
        private void fill(ref int[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = rng.Next(0, 100);
            }
        }

        private int sum(int[] data)
        {
            int sum = 0;
            for (int i = 0; i < data.Length; i++)
            {
                sum += data[i] + i;
            }
            return sum;
        }

        public override async System.Threading.Tasks.Task Run()
        {
            await ClockAsync();

            // Define input data
            int[] data = new int[network_input.Length];

            // Run with multiple inputs
            for (int i = 0; i < 10; i++)
            {
                // Fill input data
                fill(ref data);

                // Set input data
                for (int j = 0; j < network_input.Length; j++)
                {
                    network_input[j].Value = data[j];
                    network_input[j].valid = true;
                }

                // Wait for output
                await ClockAsync();
                while (!network_output.valid)
                    await ClockAsync();

                // Check output
                int expected = sum(data);
                int actual = network_output.Value;
                System.Diagnostics.Debug.Assert(expected == actual, $"Error: Expected {expected}, got {actual}");
            }
        }
    }

    // TODO test with array of buses for output (one to many)
    // TODO test with the array of buses being shuffled to test whether the FirstOrDefault works
    // TODO test with array of buses for both input and output (many to many)
}