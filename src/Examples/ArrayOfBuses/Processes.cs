using SME;
using System.Linq;
using System.Collections.Generic;

namespace ArrayOfBuses
{
    // -----
    // First test: one to one proc with array of buses for input
    // -----
    [ClockedProcess]
    public class ReduceAdd : SimpleProcess
    {
        [InputBus]
        public ValueBus[] input = new ValueBus[Constants.SIZE];

        [OutputBus]
        public ValueBus output = Scope.CreateBus<ValueBus>();

        public ReduceAdd(int[]? bias = null)
        {
            if (bias != null)
                this.bias = bias;
            else
                for (int i = 0; i < Constants.SIZE; i++)
                    this.bias[i] = i;
        }

        private int[] bias = new int[Constants.SIZE];

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
        public ValueBus[] network_input = new ValueBus[Constants.SIZE];

        [InputBus]
        public ValueBus network_output;

        public ReduceAddTester()
        {
            for (int i = 0; i < network_input.Length; i++)
            {
                network_input[i] = Scope.CreateBus<ValueBus>();
            }
        }

        private int[] data = new int[Constants.SIZE];

        public override async System.Threading.Tasks.Task Run()
        {
            await ClockAsync();

            // Run with multiple inputs
            for (int i = 0; i < 10; i++)
            {
                // Fill input data
                Helpers.fill(ref data);

                // Set input data
                for (int j = 0; j < network_input.Length; j++)
                {
                    network_input[j].Value = data[j];
                    network_input[j].valid = true;
                }

                // Wait for output
                await ClockAsync();
                for (int j = 0; j < network_input.Length; j++)
                {
                    network_input[j].valid = false;
                }
                await ClockAsync();
                while (!network_output.valid)
                    await ClockAsync();

                // Check output
                int expected = Helpers.sum(data) + Helpers.sum(Enumerable.Range(0, Constants.SIZE).ToArray());
                int actual = network_output.Value;
                System.Diagnostics.Debug.Assert(expected == actual,
                    $"Error: Expected {expected}, got {actual}");
            }
        }
    }

    // -----
    // Second test: one to one proc with array of buses for output (TODO)
    // -----

    // -----
    // Third test: one to many proc with array of buses for input (TODO)
    // -----

    // -----
    // Fourth test: one to many proc with array of buses for output
    // -----
    [ClockedProcess]
    public class Identity : SimpleProcess
    {
        [InputBus]
        public ValueBus input;

        [OutputBus]
        public ValueBus output = Scope.CreateBus<ValueBus>();

        public Identity(int id)
        {
            this.id = id;
        }

        private int id;

        protected override void OnTick()
        {
            output.valid = input.valid;
            output.Value = input.Value + id;
        }
    }

    // TODO better name / better test
    public class SplitterTester : SimulationProcess
    {
        [OutputBus]
        public ValueBus[] network_input = new ValueBus[Constants.SIZE];

        [InputBus]
        public ValueBus[] network_output = new ValueBus[Constants.SIZE];

        public SplitterTester()
        {
            for (int i = 0; i < network_input.Length; i++)
            {
                network_input[i] = Scope.CreateBus<ValueBus>();
            }
        }

        int[] data = new int[Constants.SIZE];

        // TODO test with multiple iterations.
        public override async System.Threading.Tasks.Task Run()
        {
            await ClockAsync();

            for (int i = 0; i < network_input.Length; i++)
            {
                network_input[i].valid = false;
                network_input[i].Value = 0;
            }

            await ClockAsync();

            Helpers.fill(ref data);
            for (int i = 0; i < network_input.Length; i++)
            {
                network_input[i].valid = true;
                network_input[i].Value = data[i];
            }

            await ClockAsync();

            for (int i = 0; i < network_input.Length; i++)
            {
                network_input[i].valid = false;
                network_input[i].Value = 0;
            }

            while (!network_output[0].valid)
                await ClockAsync();

            for (int i = 0; i < network_output.Length; i++)
            {
                int expected = data[i] + i;
                int actual = network_output[i].Value;
                System.Diagnostics.Debug.Assert(expected == actual,
                    $"Error: Expected {expected}, got {actual}");
            }
        }
    }

    [ClockedProcess]
    public class Broadcaster : SimpleProcess
    {
        [InputBus]
        public ValueBus input;

        [OutputBus]
        public ValueBus[] output = new ValueBus[Constants.SIZE];

        public Broadcaster()
        {
            for (int i = 0; i < output.Length; i++)
                output[i] = Scope.CreateBus<ValueBus>();
        }

        protected override void OnTick()
        {
            for (int i = 0; i < output.Length; i++)
            {
                output[i].valid = input.valid;
                output[i].Value = input.Value;
            }
        }
    }

    public class BroadcasterTester : SimulationProcess
    {
        [OutputBus]
        public ValueBus network_input = Scope.CreateBus<ValueBus>();

        [InputBus]
        public ValueBus network_output;

        public BroadcasterTester()
        {
            Helpers.fill(ref data);
        }

        int[] data = new int[1];

        // TODO test with multiple iterations.
        public override async System.Threading.Tasks.Task Run()
        {
            await ClockAsync();

            network_input.valid = false;
            network_input.Value = 0;

            await ClockAsync();

            network_input.valid = true;
            network_input.Value = data[0];

            await ClockAsync();

            network_input.valid = false;
            network_input.Value = 0;

            while (!network_output.valid)
                await ClockAsync();

            int expected = data[0] * Constants.SIZE + Helpers.sum(Enumerable.Range(0, Constants.SIZE).ToArray());
            int actual = network_output.Value;
            System.Diagnostics.Debug.Assert(expected == actual,
                $"Error: Expected {expected}, got {actual}");
        }
    }

    // TODO test with the array of buses being shuffled to test whether the FirstOrDefault works
    // TODO test with array of buses for both input and output (many to many)
}