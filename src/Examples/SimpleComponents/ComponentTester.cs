using System;
using System.Threading.Tasks;
using SME;

namespace SimpleComponents
{
    /// <summary>
    /// The tester for the FifoBuffer components
    /// </summary>
    public class ComponentTester : SimulationProcess
    {
        /// <summary>
        /// The control channel for the buffer
        /// </summary>
        SimpleFifoBuffer<byte>.IInputBus Control = Scope.CreateOrLoadBus<SimpleFifoBuffer<byte>.IInputBus>();
        /// <summary>
        /// The data channel for the buffer
        /// </summary>
        SimpleFifoBuffer<byte>.IOutputBus Data = Scope.CreateOrLoadBus<SimpleFifoBuffer<byte>.IOutputBus>();

        /// <summary>
        /// The assigned index for testing
        /// </summary>
        private readonly byte m_index;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SimpleComponents.ComponentTester"/> class.
        /// </summary>
        /// <param name="offset">The index to use.</param>
        public ComponentTester(byte offset)
        {
            m_index = offset;
        }

        /// <summary>
        /// Runs the testing methods
        /// </summary>
        /// <returns>The awaitable task.</returns>
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
                throw new Exception($"Expected the output value to be {m_index} but it was {Data.Value}");

            Control.Read = true;

            await ClockAsync();

            if (Data.Filled)
                throw new Exception("Expected the buffer to not be filled after reading a value");
            if (!Data.Valid)
                throw new Exception("Expected the output to be ready after write");
            if (Data.Value != m_index + 1)
                throw new Exception($"Expected the output value to be {m_index + 1} but it was {Data.Value}");

            await ClockAsync();

            if (Data.Valid)
                throw new Exception("Expected the output to be empty after reading two values");

            Console.WriteLine($"Correctly tested fifo buffer with {m_index}");
        }
    }
}
