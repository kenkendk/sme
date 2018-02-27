using System;
using System.Linq;

namespace SME.VHDL.Components
{
    [ClockedProcess]
    [SuppressBody]
    public class SinglePortMemory<TAddress, TData> : SimpleProcess, IVHDLComponent
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
        public SinglePortMemory(TData[] initial = null, TData initialvalue = default(TData))
            : base()
        {
            DataWidth = VHDLHelper.GetBitWidthFromType(typeof(TData));
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

        string IVHDLComponent.IncludeRegion(RenderStateProcess renderer, int indentation)
        {
            return VHDLHelper.CreateComponentInclude(renderer.Parent.Config, indentation);
        }

        string IVHDLComponent.SignalRegion(RenderStateProcess renderer, int indentation)
        {
            var self = renderer.Process;
            var outbus = self.OutputBusses.First();
            var inbus = self.InputBusses.First();

            var memsize = m_memory.Length * DataWidth;
            var config = new BlockRamConfig(renderer, DataWidth, memsize, false);

            var template = $@"
signal EN_internal: std_logic;
signal WE_internal: std_logic_vector({config.wewidth - 1} downto 0);
signal DI_internal : std_logic_vector({DataWidth - 1} downto 0);
signal DO_internal : std_logic_vector({DataWidth - 1} downto 0);
signal ADDR_internal : std_logic_vector({config.realaddrwidth - 1} downto 0);
";
            return VHDLHelper.ReIndentTemplate(template, indentation);
        }

        string IVHDLComponent.ProcessRegion(RenderStateProcess renderer, int indentation)
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
            var inbus = self.InputBusses.First();

            var addrpadding =
                AddressWidth == config.realaddrwidth
                ? string.Empty
                : string.Format("\"{0}\" & ", new string('0', (config.realaddrwidth - AddressWidth)));

            var template =
$@"
{self.InstanceName}_inst : BRAM_SINGLE_MACRO
generic map (
    BRAM_SIZE => ""{(config.use36k ? "36Kb" : "18Kb")}"", -- Target BRAM, ""18Kb"" or ""36Kb""
    DEVICE => ""{ config.targetdevice }"", --Target device: ""VIRTEX5"", ""VIRTEX6"", ""7SERIES"", ""SPARTAN6""
    DO_REG => 0, --Optional output register(0 or 1)
    WRITE_WIDTH => { DataWidth },    --Valid values are 1 - 72(37 - 72 only valid when BRAM_SIZE = ""36Kb"")
    READ_WIDTH => { DataWidth },     --Valid values are 1 - 72(37 - 72 only valid when BRAM_SIZE = ""36Kb"")
    INIT_FILE => ""NONE"",
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
    ADDR => ADDR_internal,     -- Input address, width defined by read/write port depth    
    CLK => CLK,                -- 1-bit input clock
    EN => EN_internal,         -- 1-bit input enable
    REGCE => '0',              -- 1-bit input read output register enable
    RST => RST,                -- 1-bit input reset
    WE => WE_internal          -- Input write enable, width defined by write port depth
);
-- End of BRAM_SINGLE_MACRO instantiation

{self.InstanceName}_Helper: process(RST,CLK, RDY)
begin
if RST = '1' then
    FIN <= '0';                        
elsif rising_edge(CLK) then
    FIN <= not RDY;
end if;
end process;

EN_internal <= ENB and {Naming.ToValidName(renderer.Parent.GetLocalBusName(inbus, self) + "_" + nameof(IInput.Enabled)) };
WE_internal <= (others => EN_internal and {Naming.ToValidName(renderer.Parent.GetLocalBusName(inbus, self) + "_" + nameof(IInput.IsWriting)) });
DI_internal <= std_logic_vector({ Naming.ToValidName(renderer.Parent.GetLocalBusName(inbus, self) + "_" + nameof(IInput.Data)) });
ADDR_internal <= { addrpadding }std_logic_vector({ Naming.ToValidName(renderer.Parent.GetLocalBusName(inbus, self) + "_" + nameof(IInput.Address)) });
{ Naming.ToValidName(renderer.Parent.GetLocalBusName(outbus, self) + "+" + nameof(IOutput.Data)) } <= {renderer.Parent.VHDLWrappedTypeName(outbus.Signals.First())}(DO_internal);

            ";
            return VHDLHelper.ReIndentTemplate(template, indentation);
        }
    }
}
