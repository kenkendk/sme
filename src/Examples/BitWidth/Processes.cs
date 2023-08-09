using System;
using SME;
using SME.VHDL;

namespace BitWidth
{
    [ClockedProcess]
    public class Add : SimpleProcess
    {
        [InputBus]
        public ValueBus a;

        [InputBus]
        public ValueBus b;

        [OutputBus]
        public ValueBus c = Scope.CreateBus<ValueBus>();

        protected override void OnTick()
        {
            if (a.Valid && b.Valid)
            {
                c.Value = (SME.VHDL.UInt10)(a.Value + b.Value);
                c.Valid = true;
            }
            else
            {
                c.Valid = false;
            }
        }
    }

    public class AddTester : SimulationProcess
    {
        [OutputBus]
        public ValueBus network_a = Scope.CreateBus<ValueBus>();
        [OutputBus]
        public ValueBus network_b = Scope.CreateBus<ValueBus>();

        [InputBus]
        public ValueBus network_c;

        private SME.VHDL.UInt10[] data_a, data_b, data_c;
        private Func<SME.VHDL.UInt10, SME.VHDL.UInt10, SME.VHDL.UInt10> expected_func;
        private Random rng = new Random();

        public AddTester(int data_size, Func<SME.VHDL.UInt10, SME.VHDL.UInt10, SME.VHDL.UInt10> expected_func)
        {
            this.data_a = new SME.VHDL.UInt10[data_size];
            this.data_b = new SME.VHDL.UInt10[data_size];
            this.data_c = new SME.VHDL.UInt10[data_size];
            this.expected_func = expected_func;
        }

        public async override System.Threading.Tasks.Task Run()
        {
            // Initialize the network
            await ClockAsync();

            // Generate random data and compute expected result
            for (int i = 0; i < data_a.Length; i++)
            {
                data_a[i] = (SME.VHDL.UInt10)rng.Next();
                data_b[i] = (SME.VHDL.UInt10)rng.Next();
                data_c[i] = expected_func(data_a[i], data_b[i]);
            }

            // Send data to the network, along with receiving the result
            for (int i = 0; i < data_a.Length; i++)
            {
                network_a.Valid = true;
                network_a.Value = data_a[i];
                network_b.Valid = true;
                network_b.Value = data_b[i];
                await ClockAsync();
                network_a.Valid = false;
                network_b.Valid = false;
                while (!network_c.Valid)
                    await ClockAsync();

                SME.VHDL.UInt10 expected = data_c[i];
                SME.VHDL.UInt10 actual = network_c.Value;
                System.Diagnostics.Debug.Assert(expected.Equals(actual),
                    $"Error: Expected {expected}, got {actual}");
            }
        }
    }
}