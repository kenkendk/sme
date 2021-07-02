using System;
namespace SME.Components
{
    /// <summary>
    /// Implementation of a single-port memory resource.
    /// </summary>
    [ClockedProcess]
    public class SinglePortMemory<T> : SimpleProcess
    {
        /// <summary>
        /// The control interface.
        /// </summary>
        public interface IControl : IBus
        {
            /// <summary>
            /// Gets or sets a value indicating whether this <see cref="T:SME.Components.SinglePortMemory`1.IControl"/> is enabled.
            /// </summary>
            /// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
            [InitialValue]
            bool Enabled { get; set; }
            /// <summary>
            /// Gets or sets a value indicating whether this <see cref="T:SME.Components.SinglePortMemory`1.IControl"/> is writing.
            /// </summary>
            /// <value><c>true</c> if is writing; otherwise, <c>false</c>.</value>
            [InitialValue]
            bool IsWriting { get; set; }
            /// <summary>
            /// Gets or sets the memory address to read or write.
            /// </summary>
            /// <value>The address.</value>
            int Address { get; set; }
            /// <summary>
            /// Gets or sets the data to write.
            /// </summary>
            /// <value>The data.</value>
            T Data { get; set; }
        }

        /// <summary>
        /// The result of a read operation.
        /// </summary>
        public interface IReadResult : IBus
        {
            /// <summary>
            /// Gets or sets the last data element read.
            /// </summary>
            /// <value>The data.</value>
            T Data { get; set; }
        }

        /// <summary>
        /// The control bus.
        /// </summary>
        [InputBus]
        public readonly IControl Control = Scope.CreateBus<IControl>();
        /// <summary>
        /// The result bus.
        /// </summary>
        [OutputBus]
        public readonly IReadResult ReadResult = Scope.CreateBus<IReadResult>();

        /// <summary>
        /// The memory instance.
        /// </summary>
        [Signal]
        public T[] m_memory;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SME.Components.SinglePortMemory`1"/> class.
        /// </summary>
        /// <param name="size">The size of the allocated memory area.</param>
        /// <param name="initial">The initial memory contents (optional).</param>
        public SinglePortMemory(int size, T[] initial = null)
        {
            m_memory = new T[size];
            if (initial != null)
                Array.Copy(initial, 0, m_memory, 0, Math.Min(initial.Length, size));
        }

        /// <summary>
        /// Runs the process.
        /// </summary>
        protected override void OnTick()
        {
            if (Control.Enabled)
            {
                ReadResult.Data = m_memory[Control.Address];

                if (Control.IsWriting)
                    m_memory[Control.Address] = Control.Data;
            }
        }

    }
}
