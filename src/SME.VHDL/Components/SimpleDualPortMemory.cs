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
            [InitialValue]
            bool Enabled { get; set; }
        }

        public interface IReadOut : IBus
        {
            TData Data { get; set; }
        }

        public interface IWriteIn : IBus
        {
            [InitialValue]
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
        public SimpleDualPortMemory(TData[] initial = null, TData initialvalue = default(TData))
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

        string IVHDLComponent.IncludeRegion(RenderStateProcess renderer, int indentation)
        {
            return VHDLHelper.CreateComponentInclude(renderer.Parent.Config, indentation);
        }

        string IVHDLComponent.SignalRegion(RenderStateProcess renderer, int indentation)
        {
            var self = renderer.Process;
            var outbus = self.OutputBusses.First();
            var inreadbus = self.InputBusses.First(x => typeof(IReadIn).IsAssignableFrom(x.SourceInstance.BusType));
            var inwritebus = self.InputBusses.First(x => typeof(IWriteIn).IsAssignableFrom(x.SourceInstance.BusType));

            var template = $@"
signal RDEN_internal: std_logic;
signal WREN_internal: std_logic;
signal DI_internal : { renderer.Parent.VHDLExportTypeName(inwritebus.Signals.FirstOrDefault(x => string.Equals(x.Name, nameof(IWriteIn.Data))) ) };
signal DO_internal : { renderer.Parent.VHDLExportTypeName(outbus.Signals.FirstOrDefault(x => string.Equals(x.Name, nameof(IReadOut.Data)))) };
signal RDADDR_internal : { renderer.Parent.VHDLExportTypeName(inreadbus.Signals.FirstOrDefault(x => string.Equals(x.Name, nameof(IReadIn.Address)))) };
signal WRADDR_internal : { renderer.Parent.VHDLExportTypeName(inwritebus.Signals.FirstOrDefault(x => string.Equals(x.Name, nameof(IWriteIn.Address)))) };
";
            return VHDLHelper.ReIndentTemplate(template, indentation);
       }

        string IVHDLComponent.ProcessRegion(RenderStateProcess renderer, int indentation)
		{
            var memsize = m_memory.Length * DataWidth;

            if (renderer.Parent.Config.DEVICE_VENDOR != FPGAVendor.Xilinx)
            {
                throw new Exception("Blockram is only supported on Xlinix devices for now");
            }
            else
            {
                //TODO: Support output register
                //TODO: Support initial output value

                if (memsize > 36 * 1024)
                    throw new Exception($"Unable to generate block ram with {memsize} bits as the device only supports up to {36 * 1024} bits");

                var use36k = (memsize > 18 * 1024) || DataWidth >= 37;
                var instancemem = use36k ? 1024 * 36 : 1024 * 18;

                var targetdevice = "7SERIES";
                var linewidth = 256;

                int realaddrwidth;
                int paritybits;
                int wewidth;

                if (DataWidth == 1)
                {
                    paritybits = 0;
                    wewidth = 1;
                    realaddrwidth = use36k ? 15 : 14;
                }
                else if (DataWidth == 2)
                {
                    paritybits = 0;
                    wewidth = 1;
                    realaddrwidth = use36k ? 14 : 13;
                }
                else if (DataWidth <= 4)
                {
                    paritybits = 0;
                    wewidth = 1;
                    realaddrwidth = use36k ? 13 : 12;

                }
                else if (DataWidth <= 9)
                {
                    paritybits = Math.Max(DataWidth - 8, 0);
                    wewidth = 1;
                    realaddrwidth = use36k ? 12 : 11;
                }
                else if (DataWidth <= 18)
                {
                    paritybits = Math.Max(DataWidth - 16, 0);
                    wewidth = 2;
                    realaddrwidth = use36k ? 11 : 10;

                }
                else if (DataWidth <= 36)
                {
                    paritybits = Math.Max(DataWidth - 32, 0);
                    wewidth = 4;
                    realaddrwidth = use36k ? 10 : 9;

                }
                else if (DataWidth <= 72)
                {
                    paritybits = Math.Max(DataWidth - 64, 0);
                    wewidth = 8;
                    realaddrwidth = 9;
                }
                else
                {
                    // TODO: Fix this by adding multiple block ram devices
                    throw new Exception("Xilinx devices do not support more than 72 bit data width");
                }

                var initlines = VHDLHelper.SplitDataBitStringToMemInit(
                    VHDLHelper.GetDataBitString(m_initial, instancemem),
                    DataWidth,
                    paritybits
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

                var initialvalue = VHDLHelper.GetDataBitString(new TData[] { m_resetinitial }, DataWidth);

                var self = renderer.Process;
                var outbus = self.OutputBusses.First();
                var inreadbus = self.InputBusses.First(x => typeof(IReadIn).IsAssignableFrom(x.SourceInstance.BusType));
                var inwritebus = self.InputBusses.First(x => typeof(IWriteIn).IsAssignableFrom(x.SourceInstance.BusType));
                // Apparently, it does not work to do "(others => '1')"
                var westring = new string('1', wewidth);

                var template =
    $@"
{self.InstanceName}_inst : BRAM_SDP_MACRO
generic map (
    BRAM_SIZE => ""{(use36k ? "36Kb" : "18Kb")}"", -- Target BRAM, ""18Kb"" or ""36Kb""
    DEVICE => ""{ targetdevice }"", --Target device: ""VIRTEX5"", ""VIRTEX6"", ""7SERIES"", ""SPARTAN6""
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
    RDADDR => RDADDR_internal, -- Input read address, width defined by read port depth    
    RDCLK => CLK,              -- 1-bit input read clock
    RDEN => RDEN_internal,     -- 1-bit input read port enable
    REGCE => '0',   -- 1-bit input read output register enable
    RST => RST,       -- 1-bit input reset
    WE => ""{ westring }"",         -- Input write enable, width defined by write port depth
    WRADDR => WRADDR_internal, -- Input write address, width defined by write port depth
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

RDEN_internal <= ENB and {Naming.ToValidName(renderer.Parent.GetLocalBusName(inreadbus, self) + "_Enabled") };
RDADDR_internal <= std_logic_vector({ Naming.ToValidName(renderer.Parent.GetLocalBusName(inreadbus, self) + "_Address") });
{ Naming.ToValidName(renderer.Parent.GetLocalBusName(outbus, self) + "_Data") } <= {renderer.Parent.VHDLWrappedTypeName(outbus.Signals.First())}(DO_internal);

WREN_internal <= ENB and {Naming.ToValidName(renderer.Parent.GetLocalBusName(inwritebus, self) + "_Enabled") };
WRADDR_internal <= std_logic_vector({ Naming.ToValidName(renderer.Parent.GetLocalBusName(inwritebus, self) + "_Address") });
DI_internal <= std_logic_vector({ Naming.ToValidName(renderer.Parent.GetLocalBusName(inwritebus, self) + "_Data") });

                ";
                return VHDLHelper.ReIndentTemplate(template, indentation);
            }
		}

	}
}
