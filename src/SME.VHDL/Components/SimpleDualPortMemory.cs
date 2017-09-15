using System;
using SME;
using System.Linq;
using SME.VHDL;

namespace SME.VHDL.Components
{
	[ClockedProcess]
	[SuppressBody]
	public sealed class SimpleDualPortMemory<TAddress, TData> : SimpleProcess, IVHDLComponent
	{
		public interface IReadIn : IBus
		{
			[InitialValue]
			TAddress Address { get; set; }
		}

		public interface IReadOut : IBus
		{
			TData Data { get; set; }
		}

		public interface IWriteIn : IBus
		{
			[InitialValue(false)]
			bool Enabled { get; set; }
			TAddress Address { get; set; }
			TData Data { get; set; }
		}

		[InputBus]
        public readonly IReadIn ReadIn = Scope.CreateBus<IReadIn>();
		[OutputBus]
		public readonly IReadOut ReadOut = Scope.CreateBus<IReadOut>();
		[InputBus]
		public readonly IWriteIn WriteIn = Scope.CreateBus<IWriteIn>();

        private readonly TData[] m_memory;
		private readonly TData[] m_initial;

		// Workaround for not having a "numeric" or "integer" generic constraint
		private int ConvertAddress(TAddress adr)
        {
            return (int)(object)adr;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SME.VHDL.Components.SimpleDualPortMemory`2"/> class.
        /// </summary>
        /// <param name="datawidth">The width (in bits) of the data bus.</param>
        /// <param name="addresswidth">The width (in bits) of the address bus.</param>
        /// <param name="initial">The initial memory to use</param>
        public SimpleDualPortMemory(int datawidth, int addresswidth, TData[] initial = null)
            : base()
        {
			if (datawidth < 0)
				throw new ArgumentOutOfRangeException(nameof(datawidth), datawidth, $"{nameof(datawidth)} must be a positive integer");
			if (addresswidth < 0)
				throw new ArgumentOutOfRangeException(nameof(addresswidth), addresswidth, $"{nameof(addresswidth)} must be a positive integer");
			
            DataWidth = datawidth;
            AddressWidth = addresswidth;
            m_memory = new TData[(int)Math.Pow(2, addresswidth)];

			m_initial = initial;

			if (initial != null && initial.Length > m_memory.Length)
				throw new ArgumentException($"You are attempting to set an initial memory with {initial.Length}, but the with {addresswidth} bits you can only store {m_memory.Length} elements");
			if (initial != null)
				Array.Copy(initial, m_memory, initial.Length);
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
			if (WriteIn.Enabled)
				m_memory[ConvertAddress(WriteIn.Address)] = WriteIn.Data;

			ReadOut.Data = m_memory[ConvertAddress(ReadIn.Address)];
		}

		private string FormatOutString(string componentname, int indentation, string str)
		{
			var ind = new string(' ', indentation);
			return ind + string.Join(Environment.NewLine + ind, string.Format(str.Trim(), Naming.ToValidName(this.GetType().Name), componentname, DataWidth - 1, AddressWidth - 1).Replace("\t", new string(' ', 4)).Replace("\r", "").Split(new string[] { "\n" }, StringSplitOptions.None));
		}

		string IVHDLComponent.SignalRegion(string componentname, int indentation)
		{
			return FormatOutString(componentname, indentation, @"
COMPONENT {1}
	PORT (
	clka : IN STD_LOGIC;
	ena : IN STD_LOGIC;
	wea : IN STD_LOGIC_VECTOR(0 DOWNTO 0);
	addra : IN STD_LOGIC_VECTOR({3} DOWNTO 0);
	dina : IN STD_LOGIC_VECTOR({2} DOWNTO 0);
	clkb : IN STD_LOGIC;
	enb : IN STD_LOGIC;
	addrb : IN STD_LOGIC_VECTOR({3} DOWNTO 0);
	doutb : OUT STD_LOGIC_VECTOR({2} DOWNTO 0)
	);
END COMPONENT;
"
			);
		}

		string IVHDLComponent.ProcessRegion(string componentname, int indentation)
		{
			return FormatOutString(componentname, indentation, @"
{0}_implementation: {1}
PORT MAP (
    clka => CLK,
    ena => {0}_IWriteIn_Enabled,
    wea => (others => '1'),
    addra => {0}_IWriteIn_Address({3} DOWNTO 0),
    dina => {0}_IWriteIn_Data({2} DOWNTO 0),
    clkb => CLK,
    enb => '1',
    addrb => {0}_IReadIn_Address({3} DOWNTO 0),
    doutb => {0}_IReadOut_Data({2} DOWNTO 0)
);

"
			);

		}



	}
}
