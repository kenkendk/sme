using System;
using System.Threading.Tasks;
using SME;

namespace SimpleComponents
{
    public class ComponentTester : SimulationProcess
    {
        SimpleFifoBuffer<byte>.IInputBus Control = Scope.CreateOrLoadBus<SimpleFifoBuffer<byte>.IInputBus>();
        SimpleFifoBuffer<byte>.IOutputBus Data = Scope.CreateOrLoadBus<SimpleFifoBuffer<byte>.IOutputBus>();

        private readonly byte m_index;

		public ComponentTester(byte index)
        {
            m_index = index;
        }

        public override async Task Run()
        {
            await ClockAsync();

            Control.Valid = true;
            Control.Value = m_index;

			await ClockAsync();

			Control.Valid = true;
            Control.Value = (byte)(m_index + 1);
            if (!Data.Valid)
                throw new Exception("Expected the output to be ready after write");
            if (Data.Value != m_index)
                throw new Exception($"Expected the output value to be {m_index}");

			await ClockAsync();

            Control.Valid = false;

            if (!Data.Filled)
                throw new Exception("Expected the buffer to be filled after writing two values");
			if (!Data.Valid)
				throw new Exception("Expected the output to be ready after write");
			if (Data.Value != m_index)
				throw new Exception($"Expected the output value to be {m_index}");

			Control.Read = true;

            await ClockAsync();

			if (Data.Filled)
				throw new Exception("Expected the buffer to not be filled after reading a value");
			if (!Data.Valid)
				throw new Exception("Expected the output to be ready after write");
			if (Data.Value != m_index + 1)
				throw new Exception($"Expected the output value to be {m_index + 1}");

			await ClockAsync();

			if (Data.Valid)
				throw new Exception("Expected the output to be empty after reading two values");

            Console.WriteLine($"Correctly tested fifo buffer with {m_index}");
		}
    }
}
