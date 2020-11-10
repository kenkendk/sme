using System;
using System.Threading.Tasks;
using SME;

namespace SimpleComponents
{
    /// <summary>
    /// Example implementation of a FIFO buffer
    /// </summary>
    public class SimpleFifoBuffer<T> : SimpleProcess
    {
        /// <summary>
        /// The input bus
        /// </summary>
        [TopLevelInputBus]
        public interface IInputBus : IBus
        {
            /// <summary>
            /// Gets or sets a value indicating whether the value is valid.
            /// </summary>
            [InitialValue]
            bool Valid { get; set; }

            /// <summary>
            /// Gets or sets the value to insert into the buffer.
            /// </summary>
            T Value { get; set; }

            /// <summary>
            /// A flag indicating if the output is consumed
            /// </summary>
            [InitialValue]
            bool Read { get; set; }
        }

        /// <summary>
        /// The output bus
        /// </summary>
        [TopLevelOutputBus]
        public interface IOutputBus : IBus
        {
            /// <summary>
            /// Gets or sets a value indicating whether the value is valid.
            /// </summary>
            [InitialValue]
            bool Valid { get; set; }
            /// <summary>
            /// Gets or sets the value read from the buffer.
            /// </summary>
            T Value { get; set; }

            /// <summary>
            /// Gets or sets a value indicating if the buffer is full
            /// </summary>
            [InitialValue]
            bool Filled { get; set; }

            /// <summary>
            /// Gets or sets the buffer index, used for debugging the AST
            /// </summary>
            /// <value>The index.</value>
            byte Index { get; set; }
        }

        /// <summary>
        /// The buffer storing the intermediate values
        /// </summary>
        private T[] m_buffer;
        /// <summary>
        /// The number of elements in the buffer
        /// </summary>
        private int m_count;
        /// <summary>
        /// The first element in the buffer
        /// </summary>
        private int m_head;
        /// <summary>
        /// Test element for the AST parser, not used in code
        /// </summary>
        //private T m_test;
        /// <summary>
        /// The index value
        /// </summary>
        private readonly byte m_index;

        /// <summary>
        /// A dummy static variable for testing statically initialized variables in the AST and code generator
        /// </summary>
        //private static readonly int m_dummy_static = 4;

        /// <summary>
        /// A dummy static array for testing statically initialized arrays in the AST and code generator
        /// </summary>
        private static readonly byte[] m_dummy_static_const_array = new byte[] { 1, 2 };

        /// <summary>
        /// An instance counter ofr debugging with multiple components
        /// </summary>
        private static byte __instance_number = 0;

        /// <summary>
        /// The input bus.
        /// </summary>
        [InputBus]
        public readonly IInputBus Input;

        /// <summary>
        /// The output bus.
        /// </summary>
        [OutputBus]
        public readonly IOutputBus Output;


        /// <summary>
        /// Initializes a new instance of the <see cref="T:SimpleComponents.SimpleFifoBuffer`1"/> class.
        /// </summary>
        /// <param name="depth">The number of elements in the buffer.</param>
        public SimpleFifoBuffer(int depth, string inputbusname = null, string outputbusname = null)
        {
            m_buffer = new T[depth];
            m_index = __instance_number++;
            Input = Scope.CreateOrLoadBus<IInputBus>(inputbusname, forceCreate: inputbusname == null);
            Output = Scope.CreateOrLoadBus<IOutputBus>(outputbusname, forceCreate: outputbusname == null);
        }

        /// <summary>
        /// Activation on clock tick
        /// </summary>
        protected override void OnTick()
        {
            // Output the index to signal which instance this is (for debugging multiple components)
            Output.Index = m_index;

            if (Input.Read && m_count > 0)
            {
                m_count--;
                m_head = (m_head + 1) % m_buffer.Length;
            }

            SimulationOnly(() => {
                if (Input.Valid && m_count >= m_buffer.Length)
                    Console.WriteLine("Buffer overflow attempted");
            });

            if (Input.Valid && m_count < m_buffer.Length)
            {
                m_buffer[(m_head + m_count) % m_buffer.Length] = Input.Value;
                m_count++;
            }

            if (m_count > 0)
            {
                Output.Valid = true;
                Output.Value = m_buffer[m_head];
            }
            else
            {
                Output.Valid = false;
            }

            Output.Filled = m_count == m_buffer.Length;
        }
    }
}
