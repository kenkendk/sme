using System;
using SME;
using System.Linq;
using SME.VHDL;

namespace SME.VHDL.OldComponents
{
    /// <summary>
    /// Implements the Simple Dual Port memory component primitive
    /// </summary>
    [ClockedProcess]
    [SuppressBody]
    public sealed class SimpleDualPortMemory<TAddress, TData> : SimpleProcess
    {
        /// <summary>
        /// The read input bus
        /// </summary>
        public interface IReadIn : IBus
        {
            /// <summary>
            /// Sets the address used to read data
            /// </summary>
            /// <value>The address.</value>
            [InitialValue]
            TAddress Address { get; set; }
            /// <summary>
            /// Gets or sets a value indicating whether reading is enabled.
            /// </summary>
            /// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
            [InitialValue]
            bool Enabled { get; set; }
        }

        /// <summary>
        /// The read output bus
        /// </summary>
        public interface IReadOut : IBus
        {
            /// <summary>
            /// Gets the read data
            /// </summary>
            /// <value>The data.</value>
            TData Data { get; set; }
        }

        /// <summary>
        /// The write input bus
        /// </summary>
        public interface IWriteIn : IBus
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
            TAddress Address { get; set; }
            /// <summary>
            /// Sets the data to write
            /// </summary>
            /// <value>The data.</value>
            TData Data { get; set; }
        }

        /// <summary>
        /// The read input bus
        /// </summary>
        [InputBus]
        public readonly IReadIn ReadIn = Scope.CreateBus<IReadIn>();
        /// <summary>
        /// The read output bus
        /// </summary>
        [OutputBus]
        public readonly IReadOut ReadOut = Scope.CreateBus<IReadOut>();
        /// <summary>
        /// The write input bus
        /// </summary>
        [InputBus]
        public readonly IWriteIn WriteIn = Scope.CreateBus<IWriteIn>();

        /// <summary>
        /// The current simulated memory
        /// </summary>
        private readonly TData[] m_memory;
        /// <summary>
        /// The initial memory contents
        /// </summary>
        private readonly TData[] m_initial;
        /// <summary>
        /// The reset value
        /// </summary>
        private readonly TData m_resetinitial;

        /// <summary>
        /// Converts the address type to an integer
        /// Workaround for not having a "numeric" or "integer" generic constraint
        /// </summary>
        /// <returns>The integer address.</returns>
        /// <param name="adr">The typed address.</param>
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
        public SimpleDualPortMemory(TData[] initial = null, TData initialvalue = default(TData), int elementcount = -1)
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

        /// <summary>
        /// Runs the method for simulation
        /// </summary>
        protected override void OnTick()
        {
            Console.WriteLine("Reading from {0}", ConvertAddress(ReadIn.Address));
            if (ReadIn.Enabled)
                ReadOut.Data = m_memory[ConvertAddress(ReadIn.Address)];

            if (WriteIn.Enabled)
            {
                if (ConvertAddress(WriteIn.Address) == ConvertAddress(ReadIn.Address) && ReadIn.Enabled)
                    throw new ArgumentException("Cannot read and write to the same address in a dual-port setup");
                    
                m_memory[ConvertAddress(WriteIn.Address)] = WriteIn.Data;
            }
        }

        /// <summary>
        /// Creates the include region
        /// </summary>
        /// <returns>The include region.</returns>
        /// <param name="renderer">The renderer instance.</param>
        /// <param name="indentation">The current indentation.</param>
        private string IncludeRegion(RenderStateProcess renderer, int indentation)
        {
            return VHDLHelper.CreateComponentInclude(renderer.Parent.Config, indentation);
        }

        /// <summary>
        /// Creates the signal region
        /// </summary>
        /// <returns>The signal region.</returns>
        /// <param name="renderer">The renderer instance.</param>
        /// <param name="indentation">The current indentation.</param>
        private string SignalRegion(RenderStateProcess renderer, int indentation)
        {
            var self = renderer.Process;
            var outbus = self.OutputBusses.First();
            var inreadbus = self.InputBusses.First(x => typeof(IReadIn).IsAssignableFrom(x.SourceInstance.BusType));
            var inwritebus = self.InputBusses.First(x => typeof(IWriteIn).IsAssignableFrom(x.SourceInstance.BusType));

            var memsize = m_memory.Length * DataWidth;
            var config = new BlockRamConfig(renderer, DataWidth, memsize, false);

            var rdaddr_partial = string.Empty;
            var wraddr_partial = string.Empty;
            string template;

            if (typeof(TAddress) == typeof(int))
            {
                template = $@"
signal RDEN_internal: std_logic;
signal WREN_internal: std_logic;
signal DI_internal : std_logic_vector({DataWidth - 1} downto 0);
signal DO_internal : std_logic_vector({DataWidth - 1} downto 0);
signal RDADDR_internal_partial : std_logic_vector({config.realaddrwidth - 1} downto 0);
signal WRADDR_internal_partial : std_logic_vector({config.realaddrwidth - 1} downto 0);
signal RDADDR_internal : std_logic_vector(31 downto 0);
signal WRADDR_internal : std_logic_vector(31 downto 0);
";
            }
            else
            {
                template = $@"
signal RDEN_internal: std_logic;
signal WREN_internal: std_logic;
signal DI_internal : std_logic_vector({DataWidth - 1} downto 0);
signal DO_internal : std_logic_vector({DataWidth - 1} downto 0);
signal RDADDR_internal : std_logic_vector({config.realaddrwidth - 1} downto 0);
signal WRADDR_internal : std_logic_vector({config.realaddrwidth - 1} downto 0);
";
            }
            return VHDLHelper.ReIndentTemplate(template, indentation);
       }

        /// <summary>
        /// Creates the process region
        /// </summary>
        /// <returns>The process region.</returns>
        /// <param name="renderer">The renderer instance.</param>
        /// <param name="indentation">The current indentation.</param>
        private string ProcessRegion(RenderStateProcess renderer, int indentation)
		{
            var memsize = m_memory.Length * DataWidth;
            var config = new BlockRamConfig(renderer, DataWidth, memsize, false);

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

            var initialvalue = VHDLHelper.GetDataBitString(m_resetinitial, DataWidth);

            var self = renderer.Process;
            var outbus = self.OutputBusses.First();
            var inreadbus = self.InputBusses.First(x => typeof(IReadIn).IsAssignableFrom(x.SourceInstance.BusType));
            var inwritebus = self.InputBusses.First(x => typeof(IWriteIn).IsAssignableFrom(x.SourceInstance.BusType));
            // Apparently, it does not work to do "(others => '1')"
            var westring = new string('1', config.wewidth);

            var addrpadding =
                AddressWidth <= config.realaddrwidth
                ? string.Empty
                : string.Format("\"{0}\" & ", new string('0', (config.realaddrwidth - AddressWidth)));

            var partialaddrsuffix =
                typeof(TAddress) == typeof(int)
                ? "_partial"
                : string.Empty;

            var template =
$@"
{self.InstanceName}_inst : BRAM_SDP_MACRO
generic map (
    BRAM_SIZE => ""{(config.use36k ? "36Kb" : "18Kb")}"", -- Target BRAM, ""18Kb"" or ""36Kb""
    DEVICE => ""{ config.targetdevice }"", --Target device: ""VIRTEX5"", ""VIRTEX6"", ""7SERIES"", ""SPARTAN6""
    WRITE_WIDTH => { DataWidth },    --Valid values are 1 - 72(37 - 72 only valid when BRAM_SIZE = ""36Kb"")
    READ_WIDTH => { DataWidth },     --Valid values are 1 - 72(37 - 72 only valid when BRAM_SIZE = ""36Kb"")
    DO_REG => 0, --Optional output register(0 or 1)
    INIT_FILE => ""NONE"",
    SIM_COLLISION_CHECK => ""GENERATE_X_ONLY"", --Collision check enable ""ALL"", ""WARNING_ONLY"",
                                 --""GENERATE_X_ONLY"" or ""NONE""
    SRVAL => X""{ initialvalue}"", --Set / Reset value for port output
    WRITE_MODE => ""READ_FIRST"", --Specify ""READ_FIRST"" for same clock or synchronous clocks
                               --  Specify ""WRITE_FIRST"" for asynchrononous clocks on ports

    -- The following INIT_xx declarations specify the initial contents of the RAM
{ memlines },

    -- The next set of INITP_xx are for the parity bits
{ paritylines },

    INIT => X""{ initialvalue}"" --Initial values on output port
)   
port map (
    DO => DO_internal,         -- Output read data port, width defined by READ_WIDTH parameter
    DI => DI_internal,         -- Input write data port, width defined by WRITE_WIDTH parameter
    RDADDR => RDADDR_internal{partialaddrsuffix}, -- Input read address, width defined by read port depth    
    RDCLK => CLK,              -- 1-bit input read clock
    RDEN => RDEN_internal,     -- 1-bit input read port enable
    REGCE => '0',   -- 1-bit input read output register enable
    RST => RST,       -- 1-bit input reset
    WE => ""{ westring }"",         -- Input write enable, width defined by write port depth
    WRADDR => WRADDR_internal{partialaddrsuffix}, -- Input write address, width defined by write port depth
    WRCLK => CLK,   -- 1-bit input write clock
    WREN => WREN_internal      -- 1-bit input write port enable
);
-- End of BRAM_SDP_MACRO_inst instantiation

{self.InstanceName}_Helper: process(RST,CLK, RDY)
begin
if RST = '1' then
    FIN <= '0';                        
elsif rising_edge(CLK) then
    FIN <= not RDY;
end if;
end process;

RDEN_internal <= ENB and {Naming.ToValidName(renderer.Parent.GetLocalBusName(inreadbus, self) + "_" + nameof(IReadIn.Enabled)) };
RDADDR_internal <= { addrpadding }std_logic_vector({ Naming.ToValidName(renderer.Parent.GetLocalBusName(inreadbus, self) + "_" + nameof(IReadIn.Address)) });
{ Naming.ToValidName(renderer.Parent.GetLocalBusName(outbus, self) + "+" + nameof(IReadOut.Data)) } <= {renderer.Parent.VHDLWrappedTypeName(outbus.Signals.First())}(DO_internal);

WREN_internal <= ENB and {Naming.ToValidName(renderer.Parent.GetLocalBusName(inwritebus, self) + "_" + nameof(IWriteIn.Enabled)) };
WRADDR_internal <= { addrpadding }std_logic_vector({ Naming.ToValidName(renderer.Parent.GetLocalBusName(inwritebus, self) + "_" + nameof(IWriteIn.Address)) });
DI_internal <= std_logic_vector({ Naming.ToValidName(renderer.Parent.GetLocalBusName(inwritebus, self) + "_" + nameof(IWriteIn.Data)) });
";

            if (partialaddrsuffix != string.Empty)
            {
                template +=
$@"
RDADDR_internal_partial <= RDADDR_internal({config.realaddrwidth} downto 0);
WRADDR_internal_partial <= WRADDR_internal({config.realaddrwidth} downto 0);
";
            }
            return VHDLHelper.ReIndentTemplate(template, indentation);
		}

	}
}
