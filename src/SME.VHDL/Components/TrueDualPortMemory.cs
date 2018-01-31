using System;
using SME;
using SME.VHDL;
using System.Linq;

namespace SME.VHDL.Components
{
	[ClockedProcess]
	[SuppressBody]
    public sealed class TrueDualPortMemory<TAddress, TData> : SimpleProcess, IVHDLComponent
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
            return Convert.ToInt32(adr);
        }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SME.VHDL.Components.TrueDualPortMemory`2"/> class.
		/// </summary>
        /// <param name="initial">The initial memory contents</param>
        public TrueDualPortMemory(TData[] initial = null)
            : base()
        {
            var dataWidth = VHDLHelper.GetBitWidthFromType(typeof(TData));
            var addrWidth = VHDLHelper.GetBitWidthFromType(typeof(TAddress));

            DataWidthA = dataWidth;
            AddressWidthA = addrWidth;
            DataWidthB = dataWidth;
            AddressWidthB = addrWidth;
            m_memory = new TData[(int)Math.Pow(2, addrWidth)];
            m_initial = initial;

            if (initial != null && initial.Length > m_memory.Length)
                throw new ArgumentException($"You are attempting to set an initial memory with {initial.Length}, but the with {addrWidth} bits you can only store {m_memory.Length} elements");
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

        /// <summary>
        /// Performs the operations when the signals are ready
        /// </summary>
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

        string IVHDLComponent.IncludeRegion(RenderStateProcess renderer, int indentation)
        {
            return VHDLHelper.CreateComponentInclude(renderer.Parent.Config, indentation);
        }

        string IVHDLComponent.SignalRegion(RenderStateProcess renderer, int indentation)
        {
            var self = renderer.Process;
            var template =
$@"
COMPONENT {self.InstanceName}
    PORT (
    clka : IN STD_LOGIC;
    ena : IN STD_LOGIC;
    wea : IN STD_LOGIC_VECTOR(0 DOWNTO 0);
    addra : IN STD_LOGIC_VECTOR({AddressWidthA - 1} DOWNTO 0);
    dina : IN STD_LOGIC_VECTOR({DataWidthA - 1} DOWNTO 0);
    clkb : IN STD_LOGIC;
    enb : IN STD_LOGIC;
    addrb : IN STD_LOGIC_VECTOR({AddressWidthB - 1} DOWNTO 0);
    doutb : OUT STD_LOGIC_VECTOR({DataWidthB - 1} DOWNTO 0)
    );
END COMPONENT;
";

            return VHDLHelper.ReIndentTemplate(template, indentation);
        }

        string IVHDLComponent.ProcessRegion(RenderStateProcess renderer, int indentation)
        {
            var self = renderer.Process;
            var template =
$@"
{self.InstanceName}_implementation: {self.InstanceName}
PORT MAP (
    clka => CLK,
    ena => {self.InstanceName}_IWriteIn_Enabled,
    wea => (others => '1'),
    addra => {self.InstanceName}_IWriteIn_Address({AddressWidthA - 1} DOWNTO 0),
    dina => {self.InstanceName}_IWriteIn_Data({DataWidthB - 1} DOWNTO 0),
    clkb => CLK,
    enb => '1',
    addrb => {self.InstanceName}_IReadIn_Address({AddressWidthA - 1} DOWNTO 0),
    doutb => {0}_IReadOut_Data({DataWidthB - 1} DOWNTO 0)
);
";
            return VHDLHelper.ReIndentTemplate(template, indentation);

        }
	}
}
