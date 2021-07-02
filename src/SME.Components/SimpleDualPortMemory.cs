using System;
namespace SME.Components
{
    /// <summary>
    /// Implementation of a simple dual-port memory resource.
    /// </summary>
    [ClockedProcess]
    public class SimpleDualPortMemory<T> : SimpleProcess
    {
        /// <summary>
        /// The read input bus.
        /// </summary>
        public interface IReadControl : IBus
        {
            /// <summary>
            /// Sets the address used to read data.
            /// </summary>
            /// <value>The address.</value>
            int Address { get; set; }
            /// <summary>
            /// Gets or sets a value indicating whether reading is enabled.
            /// </summary>
            /// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
            [InitialValue]
            bool Enabled { get; set; }
        }

        /// <summary>
        /// The read output bus.
        /// </summary>
        public interface IReadResult : IBus
        {
            /// <summary>
            /// Gets the read data.
            /// </summary>
            /// <value>The data.</value>
            T Data { get; set; }
        }

        /// <summary>
        /// The write input bus.
        /// </summary>
        public interface IWriteControl : IBus
        {
            /// <summary>
            /// Gets or sets a value indicating whether writing is enabled.
            /// </summary>
            /// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
            [InitialValue]
            bool Enabled { get; set; }
            /// <summary>
            /// Sets the address to write to.
            /// </summary>
            /// <value>The address.</value>
            int Address { get; set; }
            /// <summary>
            /// Sets the data to write.
            /// </summary>
            /// <value>The data.</value>
            T Data { get; set; }
        }

        /// <summary>
        /// The read input bus.
        /// </summary>
        [InputBus]
        public readonly IReadControl ReadControl = Scope.CreateBus<IReadControl>();
        /// <summary>
        /// The read output bus.
        /// </summary>
        [OutputBus]
        public readonly IReadResult ReadResult = Scope.CreateBus<IReadResult>();
        /// <summary>
        /// The write input bus.
        /// </summary>
        [InputBus]
        public readonly IWriteControl WriteControl = Scope.CreateBus<IWriteControl>();

        /// <summary>
        /// The current simulated memory.
        /// </summary>
        [Signal]
        public T[] m_memory;

        /// <summary>
        /// Flag indicating whether a warning message regarding clashing read/write addresses have been issued.
        /// </summary>
        [Ignore]
        private bool warned = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SME.Components.SimpleDualPortMemory`1"/> class.
        /// </summary>
        /// <param name="size">The size of the allocated memory area.</param>
        /// <param name="initial">The initial memory contents (optional).</param>
        public SimpleDualPortMemory(int size, T[] initial = null)
        {
            m_memory = new T[size];
            if (initial != null)
                Array.Copy(initial, 0, m_memory, 0, Math.Min(initial.Length, size));
        }

        /// <summary>
        /// Runs the method for simulation.
        /// </summary>
        protected override void OnTick()
        {
            SimulationOnly(() =>
            {
                if (!warned && ReadControl.Enabled && WriteControl.Enabled && WriteControl.Address == ReadControl.Address)
                {
                    warned = true;
                    Console.WriteLine("Warning: reading and writing the same address in a dual-port setup. Actual RAM will be in read first mode.");
                }
            });

            if (ReadControl.Enabled)
                ReadResult.Data = m_memory[ReadControl.Address];

            if (WriteControl.Enabled)
                m_memory[WriteControl.Address] = WriteControl.Data;
        }
    }
}
