using System;
using System.Linq;

namespace SME.VHDL.OldComponents
{
    [ClockedProcess]
    [SuppressBody]
    public class SinglePortMemory<TAddress, TData> : SimpleProcess
    {
        public interface IInput : IBus
        {
            [InitialValue]
            bool Enabled { get; set; }
            [InitialValue]
            bool IsWriting { get; set; }
            TAddress Address { get; set; }
            TData Data { get; set; }
        }

        public interface IOutput : IBus
        {
            TData Data { get; set; }
        }

        [InputBus]
        public readonly IInput Input = Scope.CreateBus<IInput>();
        [OutputBus]
        public readonly IOutput Output = Scope.CreateBus<IOutput>();

        private readonly TData[] m_memory;
        private readonly TData[] m_initial;
        private readonly TData m_resetinitial;

        // Workaround for not having a "numeric" or "integer" generic constraint
        private int ConvertAddress(TAddress adr)
        {
            return int.Parse(adr.ToString(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SME.VHDL.Components.SimpleDualPortMemory`2"/> class.
        /// </summary>
        /// <param name="initial">The initial memory to use</param>
        /// <param name="initialvalue">The initial output value on the output port</param>
        /// <param name="elementcount">The number of elements to use. This parameter is ignored unless the <typeparamref name="TAddress"/> parameter is an <see cref="int"/></param>
        public SinglePortMemory(TData[] initial = null, TData initialvalue = default(TData), int elementcount = -1)
            : base()
        {
            DataWidth = VHDLHelper.GetBitWidthFromType(typeof(TData));
            if (typeof(TAddress) == typeof(int))
            {
                if (elementcount <= 0)
                    throw new ArgumentOutOfRangeException(nameof(elementcount), elementcount, $"When using an {typeof(int)} address, the {nameof(elementcount)} parameter must be set");
                AddressWidth = (int)Math.Ceiling(Math.Log(elementcount, 2));
            }
            else
                AddressWidth = VHDLHelper.GetBitWidthFromType(typeof(TAddress));

            m_memory = new TData[(int)Math.Pow(2, AddressWidth)];

            m_initial = initial;

            if (initial != null && initial.Length > m_memory.Length)
                throw new ArgumentException($"You are attempting to set an initial memory with {initial.Length}, but the with {AddressWidth} bits you can only store {m_memory.Length} elements");
            if (initial != null)
                Array.Copy(initial, m_memory, initial.Length);

            //ReadOut.Data = m_resetinitial = initialvalue;
        }

        /// <summary>
        /// The width (in bits) of the data bus.
        /// </summary>
        public readonly int DataWidth;
        /// <summary>
        /// The width (in bits) of the address bus.
        /// </summary>
        public readonly int AddressWidth;


        protected override void OnTick()
        {
            if (Input.Enabled)
            {
                Output.Data = m_memory[ConvertAddress(Input.Address)];

                if (Input.IsWriting)
                    m_memory[ConvertAddress(Input.Address)] = Input.Data;
            }
        }


    }
}
