using System;
using SME;
using SME.VHDL;
using System.Linq;

namespace SME.VHDL.OldComponents
{
	[ClockedProcess]
	[SuppressBody]
    public sealed class TrueDualPortMemory<TAddress, TData> : SimpleProcess
	{
		public interface IInputA : IBus
		{
			[InitialValue]
			bool IsWriting { get; set; }
			[InitialValue]
			bool Enabled { get; set; }
			[InitialValue]
			TAddress Address { get; set; }
			TData Data { get; set; }
		}

		public interface IInputB : IBus
		{
			[InitialValue]
            bool IsWriting { get; set; }
			[InitialValue]
			bool Enabled { get; set; }
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
        private readonly TData m_resetinitial;

        // Workaround for not having a "numeric" or "integer" generic constraint
        private int ConvertAddress(TAddress adr)
        {
            return int.Parse(adr.ToString(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture);
        }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SME.VHDL.Components.TrueDualPortMemory`2"/> class.
		/// </summary>
        /// <param name="initial">The initial memory contents</param>
        /// <param name="initialvalue">The initial output value on the output port</param>
        /// <param name="elementcount">The number of elements to use. This parameter is ignored unless the <typeparamref name="TAddress"/> parameter is an <see cref="int"/></param>
        public TrueDualPortMemory(TData[] initial = null, TData initialvalue = default(TData), int elementcount = -1)
            : base()
        {
            var dataWidth = VHDLHelper.GetBitWidthFromType(typeof(TData));
            int addrWidth;
            if (typeof(TAddress) == typeof(int))
            {
                if (elementcount <= 0)
                    throw new ArgumentOutOfRangeException(nameof(elementcount), elementcount, $"When using an {typeof(int)} address, the {nameof(elementcount)} parameter must be set");
                addrWidth = (int)Math.Ceiling(Math.Log(elementcount, 2));
            }
            else
                addrWidth = VHDLHelper.GetBitWidthFromType(typeof(TAddress));

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
            if (InA.Enabled && InB.Enabled && ConvertAddress(InA.Address) == ConvertAddress(InB.Address))
            {
                if (InA.IsWriting && InB.IsWriting)
                    throw new Exception("Both ports are writing the same memory address");
                
                if (InA.IsWriting == !InB.IsWriting)
                    throw new Exception("Conflicting read and write to the same memory address");
            }

            if (InA.Enabled)
            {
                OutA.Data = m_memory[ConvertAddress(InA.Address)];
                if (InA.IsWriting)
                    m_memory[ConvertAddress(InA.Address)] = InA.Data;
            }

            if (InB.Enabled)
			{
                OutB.Data = m_memory[ConvertAddress(InB.Address)];
                if (InB.IsWriting)
					m_memory[ConvertAddress(InB.Address)] = InB.Data;
			}

		}

        private string IncludeRegion(RenderStateProcess renderer, int indentation)
        {
            return VHDLHelper.CreateComponentInclude(renderer.Parent.Config, indentation);
        }

        private string SignalRegion(RenderStateProcess renderer, int indentation)
        {
            var memsize = m_memory.Length * DataWidthA;
            var config = new BlockRamConfig(renderer, DataWidthA, memsize, true);

            var self = renderer.Process;
            var outbusa = self.OutputBusses.First(x => typeof(IOutputA).IsAssignableFrom(x.SourceInstance.BusType));
            var outbusb = self.OutputBusses.First(x => typeof(IOutputB).IsAssignableFrom(x.SourceInstance.BusType));
            var inbusa = self.InputBusses.First(x => typeof(IInputA).IsAssignableFrom(x.SourceInstance.BusType));
            var inbusb = self.InputBusses.First(x => typeof(IInputB).IsAssignableFrom(x.SourceInstance.BusType));

            string template;

            if (typeof(TAddress) == typeof(int))
            {
                template = $@"
signal ENA_internal: std_logic;
signal WEA_internal: std_logic_vector({config.wewidth - 1} downto 0);
signal DIA_internal : std_logic_vector({DataWidthA - 1} downto 0);
signal DOA_internal : std_logic_vector({DataWidthA - 1} downto 0);
signal ADDRA_internal_partial : std_logic_vector({config.realaddrwidth - 1} downto 0);
signal ADDRA_internal : std_logic_vector(31 downto 0);

signal ENB_internal: std_logic;
signal WEB_internal: std_logic_vector({config.wewidth - 1} downto 0);
signal DIB_internal : std_logic_vector({DataWidthB - 1} downto 0);
signal DOB_internal : std_logic_vector({DataWidthB - 1} downto 0);
signal ADDRB_internal_partial : std_logic_vector({config.realaddrwidth - 1} downto 0);
signal ADDRB_internal : std_logic_vector(31 downto 0);
";
            }
            else
            {
                template = $@"
signal ENA_internal: std_logic;
signal WEA_internal: std_logic_vector({config.wewidth - 1} downto 0);
signal DIA_internal : std_logic_vector({DataWidthA - 1} downto 0);
signal DOA_internal : std_logic_vector({DataWidthA - 1} downto 0);
signal ADDRA_internal : std_logic_vector({config.realaddrwidth - 1} downto 0);

signal ENB_internal: std_logic;
signal WEB_internal: std_logic_vector({config.wewidth - 1} downto 0);
signal DIB_internal : std_logic_vector({DataWidthB - 1} downto 0);
signal DOB_internal : std_logic_vector({DataWidthB - 1} downto 0);
signal ADDRB_internal : std_logic_vector({config.realaddrwidth - 1} downto 0);
";
            }
            return VHDLHelper.ReIndentTemplate(template, indentation);
        }

        private string ProcessRegion(RenderStateProcess renderer, int indentation)
        {
            var memsize = m_memory.Length * DataWidthA;
            var config = new BlockRamConfig(renderer, DataWidthA, memsize, true);

            var initlines = VHDLHelper.SplitDataBitStringToMemInit(
                VHDLHelper.GetDataBitStrings(m_initial),
                config.datawidth,
                config.paritybits
            );

            var memlines = string.Join(
                "," + Environment.NewLine,
                Enumerable.Range(0, int.MaxValue)
                .Zip(
                    initlines.Item1,
                    (a, b) => string.Format("    INIT_{0:X2} => X\"{1}\"", a, b)
                )
            );

            var paritylines = string.Join(
                "," + Environment.NewLine,
                Enumerable.Range(0, int.MaxValue)
                .Zip(
                    initlines.Item2,
                    (a, b) => string.Format("    INITP_{0:X2} => X\"{1}\"", a, b)
                )
            );

            var initialvalue = VHDLHelper.GetDataBitString(m_resetinitial, DataWidthA);

            var self = renderer.Process;
            var outbusa = self.OutputBusses.First(x => typeof(IOutputA).IsAssignableFrom(x.SourceInstance.BusType));
            var outbusb = self.OutputBusses.First(x => typeof(IOutputB).IsAssignableFrom(x.SourceInstance.BusType));
            var inbusa = self.InputBusses.First(x => typeof(IInputA).IsAssignableFrom(x.SourceInstance.BusType));
            var inbusb = self.InputBusses.First(x => typeof(IInputB).IsAssignableFrom(x.SourceInstance.BusType));

            var addrpadding =
                AddressWidthA < config.realaddrwidth
                ? string.Empty
                : string.Format("\"{0}\" & ", new string('0', (config.realaddrwidth - AddressWidthA)));

            var partialaddrsuffix =
                typeof(TAddress) == typeof(int)
                ? "_partial"
                : string.Empty;

            var template =
$@"
{self.InstanceName}_inst : BRAM_TDP_MACRO
generic map (
    BRAM_SIZE => ""{(config.use36k ? "36Kb" : "18Kb")}"", -- Target BRAM, ""18Kb"" or ""36Kb""
    DEVICE => ""{ config.targetdevice }"", --Target device: ""VIRTEX5"", ""VIRTEX6"", ""7SERIES"", ""SPARTAN6""
    DOA_REG => 0, --Optional port A output register(0 or 1)
    DOB_REG => 0, --Optional port B output register(0 or 1)
    INIT_FILE => ""NONE"",
    READ_WIDTH_A => { DataWidthA },     -- Valid values are 1 - 36(19 - 36 only valid when BRAM_SIZE = ""36Kb"")
    READ_WIDTH_B => { DataWidthA },     -- Valid values are 1 - 36(19 - 36 only valid when BRAM_SIZE = ""36Kb"")
    SIM_COLLISION_CHECK => ""GENERATE_X_ONLY"", --Collision check enable ""ALL"", ""WARNING_ONLY"",
                                 --""GENERATE_X_ONLY"" or ""NONE""
    SRVAL_A => X""{ initialvalue}"", --Set / Reset value for A port output
    SRVAL_B => X""{ initialvalue}"", --Set / Reset value for B port output
    WRITE_MODE_A => ""READ_FIRST"", -- ""WRITE_FIRST"", ""READ_FIRST"" or ""NO_CHANGE""
    WRITE_MODE_B => ""READ_FIRST"", -- ""WRITE_FIRST"", ""READ_FIRST"" or ""NO_CHANGE""
    WRITE_WIDTH_A => { DataWidthA },     -- Valid values are 1 - 36(19 - 36 only valid when BRAM_SIZE = ""36Kb"")
    WRITE_WIDTH_B => { DataWidthA },     -- Valid values are 1 - 36(19 - 36 only valid when BRAM_SIZE = ""36Kb"")

    -- The following INIT_xx declarations specify the initial contents of the RAM
{ memlines },

    -- The next set of INITP_xx are for the parity bits
{ paritylines },

    INIT_A => X""{ initialvalue}"", --Initial values on A output port
    INIT_B => X""{ initialvalue}""  --Initial values on B output port
)   
port map (
    DOA => DOA_internal,         -- Output port-A, width defined by READ_WIDTH_A parameter
    DOB => DOB_internal,         -- Output port-B, width defined by READ_WIDTH_B parameter
    ADDRA => ADDRA_internal{partialaddrsuffix},     -- Input port-A address, width defined by Port A depth
    ADDRB => ADDRB_internal{partialaddrsuffix},     -- Input port-B address, width defined by Port B depth
    CLKA => CLK,                 -- 1-bit input port-A clock
    CLKB => CLK,                 -- 1-bit input port-B clock
    DIA => DIA_internal,         -- Input port-A data, width defined by WRITE_WIDTH_A parameter
    DIB => DIB_internal,         -- Input port-B data, width defined by WRITE_WIDTH_B parameter
    ENA => ENA_internal,         -- 1-bit input port-A enable
    ENB => ENB_internal,         -- 1-bit input port-B enable
    REGCEA => '0',               -- 1-bit input port-A register enable
    REGCEB => '0',               -- 1-bit input port-B register enable
    RSTA => RST,                 -- 1-bit input port-A reset
    RSTB => RST,                 -- 1-bit input port-B reset
    WEA => WEA_internal,         -- Input port-A write enable, width defined by port A depth
    WEB => WEB_internal          -- Input port-B write enable, width defined by port B depth
);
-- End of BRAM_TDP_MACRO_inst instantiation

{self.InstanceName}_Helper: process(RST,CLK, RDY)
begin
    if RST = '1' then
        FIN <= '0';                        
    elsif rising_edge(CLK) then
        FIN <= not RDY;
    end if;
end process;

ENA_internal <= ENB and {Naming.ToValidName(renderer.Parent.GetLocalBusName(inbusa, self) + "_" + nameof(IInputA.Enabled)) };
WEA_internal <= (others => ENA_internal and {Naming.ToValidName(renderer.Parent.GetLocalBusName(inbusa, self) + "_" + nameof(IInputA.IsWriting)) });
ADDRA_internal <= { addrpadding }std_logic_vector({ Naming.ToValidName(renderer.Parent.GetLocalBusName(inbusa, self) + "_" + nameof(IInputA.Address)) });
DIA_internal <= std_logic_vector({ Naming.ToValidName(renderer.Parent.GetLocalBusName(inbusa, self) + "_" + nameof(IInputA.Data)) });
{ Naming.ToValidName(renderer.Parent.GetLocalBusName(outbusa, self) + "_" + nameof(IOutputA.Data)) } <= {renderer.Parent.VHDLWrappedTypeName(outbusa.Signals.First())}(DOA_internal);

ENB_internal <= ENB and {Naming.ToValidName(renderer.Parent.GetLocalBusName(inbusb, self) + "_" + nameof(IInputB.Enabled)) };
WEB_internal <= (others => ENB_internal and {Naming.ToValidName(renderer.Parent.GetLocalBusName(inbusb, self) + "_" + nameof(IInputB.IsWriting)) });
ADDRB_internal <= { addrpadding }std_logic_vector({ Naming.ToValidName(renderer.Parent.GetLocalBusName(inbusb, self) + "_" + nameof(IInputB.Address)) });
DIB_internal <= std_logic_vector({ Naming.ToValidName(renderer.Parent.GetLocalBusName(inbusb, self) + "_" + nameof(IInputB.Data)) });
{ Naming.ToValidName(renderer.Parent.GetLocalBusName(outbusb, self) + "_" + nameof(IOutputB.Data)) } <= {renderer.Parent.VHDLWrappedTypeName(outbusa.Signals.First())}(DOB_internal);
";

            if (partialaddrsuffix != string.Empty)
            {
                template +=
$@"
ADDRA_internal_partial <= ADDRA_internal({config.realaddrwidth} downto 0);
ADDRB_internal_partial <= ADDRB_internal({config.realaddrwidth} downto 0);
";
            }
            return VHDLHelper.ReIndentTemplate(template, indentation);
        }
	}
}
