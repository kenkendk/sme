using System;
using SME;
using System.Linq;
using SME.VHDL;

namespace SME.VHDL.Components
{
	[ClockedProcess]
	[SuppressBody]
	public abstract class SimpleDualPortMemory<TAddress, TData> : SimpleProcess, IVHDLComponent
	{
		public SimpleDualPortMemory()
			: this(Clock.DefaultClock)
		{
		}

		public SimpleDualPortMemory(Clock clock)
			: base(clock)
		{
			Setup(clock);
		}

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
		[NoAutoLoad]
		protected IReadIn ReadIn;
		[OutputBus]
		[NoAutoLoad]
		protected IReadOut ReadOut;
		[InputBus]
		[NoAutoLoad]
		protected IWriteIn WriteIn;

		[Ignore]
		protected TData[] m_memory = new TData[1024];

		protected abstract int ConvertAddress(TAddress adr);
		protected abstract void Setup(Clock clock);

		protected abstract int DataWidth { get; }
		protected abstract int AddressWidth { get; }

		protected void SetBusses<TReadIn, TReadOut, TWriteIn>(Clock clock)
			where TReadIn : class, IReadIn
			where TReadOut : class, IReadOut
			where TWriteIn : class, IWriteIn
		{
			string default_namespace = null;
			var nsattr = this.GetType().GetCustomAttributes(typeof(NamespaceAttribute), true).FirstOrDefault() as NamespaceAttribute;
			if (nsattr != null)
				default_namespace = nsattr.Name;

			ReadIn = BusManager.GetBus<TReadIn>(clock, default_namespace, false);
			ReadOut = BusManager.GetBus<TReadOut>(clock, default_namespace, false);
			WriteIn = BusManager.GetBus<TWriteIn>(clock, default_namespace, false);
			ReloadBusMaps();
		}

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
