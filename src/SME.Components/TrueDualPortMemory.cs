using System;
namespace SME.Components
{
    /// <summary>
    /// Implementation of a true dual-port memory resource in read-first mode.
    /// </summary>
    [ClockedProcess]
    public class TrueDualPortMemory<T> : SimpleProcess
    {
        /// <summary>
        /// The controller bus for a port.
        /// </summary>
        public interface IControl : IBus
        {
            /// <summary>
            /// Sets a value indicating whether the address is used for writing or not.
            /// </summary>
            /// <value><c>true</c> if is writing; otherwise, <c>false</c>.</value>
            [InitialValue]
            bool IsWriting { get; set; }
            /// <summary>
            /// Sets a value indicating whether this bus is enabled.
            /// </summary>
            /// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
            [InitialValue]
            bool Enabled { get; set; }
            /// <summary>
            /// Sets the address used to read or write.
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
        /// The read result from a port.
        /// </summary>
        public interface IReadResult : IBus
        {
            /// <summary>
            /// Gets the last data read from a port.
            /// </summary>
            /// <value>The data.</value>
            T Data { get; set; }
        }

        /// <summary>
        /// The control bus for port A.
        /// </summary>
        [InputBus]
        public readonly IControl ControlA = Scope.CreateBus<IControl>();
        /// <summary>
        /// The control bus for port B.
        /// </summary>
        [InputBus]
        public readonly IControl ControlB = Scope.CreateBus<IControl>();

        /// <summary>
        /// The result of reading from port A.
        /// </summary>
        [OutputBus]
        public readonly IReadResult ReadResultA = Scope.CreateBus<IReadResult>();
        /// <summary>
        /// The result of reading from port B.
        /// </summary>
        [OutputBus]
        public readonly IReadResult ReadResultB = Scope.CreateBus<IReadResult>();

        /// <summary>
        /// The stored memory.
        /// </summary>
        [Signal]
        public T[] m_memory;

        /// <summary>
        /// Flag indicating whether a warning message regarding clashing read/write addresses have been issued.
        /// </summary>
        [Ignore]
        private bool warned = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SME.Components.TrueDualPortMemory`1"/> class.
        /// </summary>
        /// <param name="size">The size of the allocated memory area.</param>
        /// <param name="initial">The initial memory contents (optional).</param>
        public TrueDualPortMemory(int size, T[] initial = null)
        {
            m_memory = new T[size];
            if (initial != null)
                Array.Copy(initial, 0, m_memory, 0, Math.Min(initial.Length, size));
        }

        /// <summary>
        /// Performs the operations when the signals are ready.
        /// </summary>
        protected override void OnTick()
        {
            SimulationOnly(() =>
            {
                if (ControlA.Enabled && ControlB.Enabled && ControlA.Address == ControlB.Address)
                {
                    if (ControlA.IsWriting && ControlB.IsWriting)
                        throw new Exception("Both ports are writing the same memory address");

                    if (!warned && (ControlA.IsWriting || ControlB.IsWriting))
                    {
                        warned = true;
                        Console.WriteLine("Warning: reading and writing to the same address in a dual-port setup. Actual RAM will be in read first mode.");
                    }
                }
            });

            if (ControlA.Enabled)
                ReadResultA.Data = m_memory[ControlA.Address];

            if (ControlB.Enabled)
                ReadResultB.Data = m_memory[ControlB.Address];

            if (ControlA.Enabled && ControlA.IsWriting)
                m_memory[ControlA.Address] = ControlA.Data;

            if (ControlB.Enabled && ControlB.IsWriting)
                m_memory[ControlB.Address] = ControlB.Data;
        }
    }
}
