using System;
using SME;
using SME.VHDL;
using System.Linq;

namespace SME.VHDL.Components
{
	[ClockedProcess]
	[SuppressBody]
	public sealed class TrueDualPortMemory<TAddress, TData> : SimpleProcess
	{
		public interface IInputA : IBus
		{
			[InitialValue]
			bool WriteMode { get; set; }
			[InitialValue]
			bool WriteEnabled { get; set; }
			[InitialValue]
			TAddress Address { get; set; }
			TData Data { get; set; }
		}

		public interface IInputB : IBus
		{
			[InitialValue]
			bool WriteMode { get; set; }
			[InitialValue]
			bool WriteEnabled { get; set; }
			[InitialValue]
			TAddress Address { get; set; }
			TData Data { get; set; }
		}

		public interface IOutputA : IBus
		{
			TData Data { get; set; }
		}

		public interface IOutputB : IBus
		{
			TData Data { get; set; }
		}

        [InputBus]
        public readonly IInputA InA = Scope.CreateBus<IInputA>();
		[InputBus]
		public readonly IInputB InB = Scope.CreateBus<IInputB>();

		[OutputBus]
		public readonly IOutputA OutA = Scope.CreateBus<IOutputA>();
		[OutputBus]
        public readonly IOutputB OutB = Scope.CreateBus<IOutputB>();

        private readonly TData[] m_memory;

        private readonly TData[] m_initial;

        // Workaround for not having a "numeric" or "integer" generic constraint
        private int ConvertAddress(TAddress adr)
        {
            return (int)(object)adr;
        }


		/// <summary>
		/// Initializes a new instance of the <see cref="T:SME.VHDL.Components.TrueDualPortMemory`2"/> class.
		/// </summary>
		/// <param name="datawidthA">The width (in bits) of the A data bus.</param>
		/// <param name="addresswidthA">The width (in bits) of the A address bus.</param>
		/// <param name="datawidthB">The width (in bits) of the B data bus.</param>
		/// <param name="addresswidthB">The width (in bits) of the B address bus.</param>
        /// <param name="initial">The initial memory contents</param>
        public TrueDualPortMemory(int datawidthA, int addresswidthA, int datawidthB, int addresswidthB, TData[] initial = null)
            : base()
        {
            if (datawidthA < 0)
                throw new ArgumentOutOfRangeException(nameof(datawidthA), datawidthA, $"{nameof(datawidthA)} must be a positive integer");
			if (addresswidthA < 0)
				throw new ArgumentOutOfRangeException(nameof(addresswidthA), addresswidthA, $"{nameof(addresswidthA)} must be a positive integer");
			if (datawidthB < 0)
				throw new ArgumentOutOfRangeException(nameof(datawidthB), datawidthB, $"{nameof(datawidthB)} must be a positive integer");
			if (addresswidthB < 0)
				throw new ArgumentOutOfRangeException(nameof(addresswidthB), addresswidthB, $"{nameof(addresswidthB)} must be a positive integer");
            
			DataWidthA = datawidthA;
			AddressWidthA = addresswidthA;
			DataWidthB = datawidthB;
			AddressWidthB = addresswidthB;
			m_memory = new TData[(int)Math.Pow(2, Math.Max(addresswidthA, addresswidthB))];
            m_initial = initial;

            if (initial != null && initial.Length > m_memory.Length)
                throw new ArgumentException($"You are attempting to set an initial memory with {initial.Length}, but the with {Math.Max(addresswidthA, addresswidthB)} bits you can only store {m_memory.Length} elements");
            if (initial != null)
                Array.Copy(initial, m_memory, initial.Length);
		}

		/// <summary>
		/// The width (in bits) of the A data bus.
		/// </summary>
		public readonly int DataWidthA;
		/// <summary>
		/// The width (in bits) of the A address bus.
		/// </summary>
		public readonly int AddressWidthA;
		/// <summary>
		/// The width (in bits) of the B data bus.
		/// </summary>
		public readonly int DataWidthB;
		/// <summary>
		/// The width (in bits) of the B address bus.
		/// </summary>
		public readonly int AddressWidthB;

		protected override void OnTick()
		{
			if (InA.WriteMode)
			{
				if (InA.WriteEnabled)
					m_memory[ConvertAddress(InA.Address)] = InA.Data;
			}
			else
			{
				OutA.Data = m_memory[ConvertAddress(InA.Address)];
			}

			if (InB.WriteMode)
			{
				if (InB.WriteEnabled)
					m_memory[ConvertAddress(InB.Address)] = InB.Data;
			}
			else
			{
				OutB.Data = m_memory[ConvertAddress(InB.Address)];
			}
		}

	}
}
