using System;
using System.Collections.Generic;
using System.Linq;

namespace SME.VHDL.CustomRenders.Native
{
    /// <summary>
    /// Class for generating a Xilinx simple dual port RAM by using macros.
    /// </summary>
    public class XilinxSimpleDualPortRam : ICustomRenderer
    {
        /// <summary>
        /// Returns the string, which should be written in the include region of the VHDL file.
        /// </summary>
        /// <param name="renderer">The renderer currently rendering VHDL files.</param>
        /// <param name="indentation">The indentation at the current location in the VHDL file.</param>
        public string IncludeRegion(RenderStateProcess renderer, int indentation)
        {
            return VHDLHelper.CreateComponentInclude(renderer.Parent.Config, indentation);
        }

        /// <summary>
        /// Gets a set of signals for communicating with a single blockram instance.
        /// </summary>
        /// <returns>The signal region.</returns>
        /// <param name="config">The configuration to generate the signals for.</param>
        /// <param name="index">The instance index to use, or negative for no indexing.</param>
        /// <param name="overrideAddrWidth">Value for overriding the width of the address bus.</param>
        private string GetSignalRegion(BlockRamConfig config, int index = -1, int overrideAddrWidth = -1)
        {
            var index_suffix = index < 0 ? string.Empty : $"_{index}";

            return $@"
signal RDEN_internal{index_suffix}: std_logic;
signal WREN_internal{index_suffix}: std_logic;
signal DI_internal{index_suffix}: std_logic_vector({config.datawidth  - 1} downto 0);
signal DO_internal{index_suffix}: std_logic_vector({config.datawidth  - 1} downto 0);
signal RDADDR_internal{index_suffix}: std_logic_vector({(overrideAddrWidth <= 0 ? (config.realaddrwidth - 1) : (overrideAddrWidth - 1))} downto 0);
signal WRADDR_internal{index_suffix}: std_logic_vector({(overrideAddrWidth <= 0 ? (config.realaddrwidth - 1) : (overrideAddrWidth - 1))} downto 0);
";
        }

        /// <summary>
        /// Creates VHDL code that chooses the block ram component data results with the top bits.
        /// </summary>
        /// <returns>The output selector.</returns>
        /// <param name="instancename">The name of the instance to create.</param>
        /// <param name="blocks">The number of blocks.</param>
        /// <param name="fullAddressWidth">The full address width.</param>
        /// <param name="blockAddrWidth">The block address width.</param>
        private string GenerateOutputSelector(string instancename, int blocks, int fullAddressWidth, int blockAddrWidth)
        {
            var cases = Enumerable
                .Range(0, blocks)
                .Select(i => $@"    when ""{VHDLHelper.GetDataBitString(i, fullAddressWidth - blockAddrWidth).Substring(32 - (fullAddressWidth - blockAddrWidth))}"" =>
        DO_internal <= DO_internal_{i};");

            return $@"
{instancename}_Output: process(RDADDR_READ_internal, {string.Join(", ", Enumerable.Range(0, blocks).Select(x => $"DO_internal_{x}"))})
begin
    case RDADDR_READ_internal({fullAddressWidth - 1} downto {blockAddrWidth}) is
{string.Join(Environment.NewLine, cases)}
    when others =>
        -- Implicit latching
    end case;
end process;
";
        }

        /// <summary>
        /// Gets the instantiation block for a single blockram instance.
        /// </summary>
        /// <returns>The instantiation region.</returns>
        /// <param name="config">The configuration to generate the instantiation for.</param>
        /// <param name="instancename">The name of the encapsulating instance.</param>
        /// <param name="initialvalue">The initial value for the output</param>
        /// <param name="memdatalines">The lines to initially fill the block RAM with.</param>
        /// <param name="pardatalines">The lines to initially fill the parity bits with.</param>
        /// <param name="index">The instance index to use, or negative for no indexing.</param>
        private string GetInstantiationRegion(BlockRamConfig config, string instancename, string initialvalue, IEnumerable<string> memdatalines, IEnumerable<string> pardatalines, int index = -1)
        {
            var memlines = string.Join(
                "," + Environment.NewLine,
                Enumerable.Range(0, int.MaxValue)
                .Zip(
                    memdatalines,
                    (a, b) => string.Format("    INIT_{0:X2} => X\"{1}\"", a, b)
                )
            );

            var paritylines = string.Join(
                "," + Environment.NewLine,
                Enumerable.Range(0, int.MaxValue)
                .Zip(
                    pardatalines,
                    (a, b) => string.Format("    INITP_{0:X2} => X\"{1}\"", a, b)
                )
            );

            var westring = new string('1', config.wewidth);

            var index_suffix = index < 0 ? string.Empty : $"_{index}";

            return
$@"
{instancename}_inst{index_suffix} : BRAM_SDP_MACRO
generic map (
    BRAM_SIZE => ""{(config.use36k ? "36Kb" : "18Kb")}"", -- Target BRAM, ""18Kb"" or ""36Kb""
    DEVICE => ""{ config.targetdevice }"", --Target device: ""VIRTEX5"", ""VIRTEX6"", ""7SERIES"", ""SPARTAN6""
    WRITE_WIDTH => { config.datawidth },    --Valid values are 1 - 72(37 - 72 only valid when BRAM_SIZE = ""36Kb"")
    READ_WIDTH => { config.datawidth },     --Valid values are 1 - 72(37 - 72 only valid when BRAM_SIZE = ""36Kb"")
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
    DO => DO_internal{index_suffix},         -- Output read data port, width defined by READ_WIDTH parameter
    DI => DI_internal{index_suffix},         -- Input write data port, width defined by WRITE_WIDTH parameter
    RDADDR => RDADDR_internal{index_suffix}, -- Input read address, width defined by read port depth
    RDCLK => CLK,              -- 1-bit input read clock
    RDEN => RDEN_internal{index_suffix},     -- 1-bit input read port enable
    REGCE => '0',   -- 1-bit input read output register enable
    RST => RST,       -- 1-bit input reset
    WE => ""{ westring }"",         -- Input write enable, width defined by write port depth
    WRADDR => WRADDR_internal{index_suffix}, -- Input write address, width defined by write port depth
    WRCLK => CLK,   -- 1-bit input write clock
    WREN => WREN_internal{index_suffix}      -- 1-bit input write port enable
);
-- End of BRAM_SDP_MACRO_inst instantiation
";
        }

        /// <summary>
        /// Returns the string, which should be written in the body region of the VHDL file.
        /// </summary>
        /// <param name="renderer">The renderer currently rendering VHDL files.</param>
        /// <param name="indentation">The indentation at the current location in the VHDL file.</param>
        public string BodyRegion(RenderStateProcess renderer, int indentation)
        {
            var initialdata = (Array)renderer.Process.SharedVariables.First(x => x.Name == "m_memory").DefaultValue;
            var size = initialdata.Length;
            var datawidth = VHDLHelper.GetBitWidthFromType(initialdata.GetType().GetElementType());

            var resetinitial = renderer.Process.LocalBusNames.Keys.Where(x => x.SourceType.Name == nameof(SME.Components.SimpleDualPortMemory<int>.IReadResult)).SelectMany(x => x.Signals).First().DefaultValue;
            var initialvalue = VHDLHelper.GetDataBitString(initialdata.GetType().GetElementType(), resetinitial, datawidth);

            var itemsPr18k = ((18 * 1024) + (datawidth - 1)) / datawidth;
            var in18kBlocks = (size + (itemsPr18k - 1)) / itemsPr18k;

            var fullAddressWidth = (int)Math.Ceiling(Math.Log(size, 2));

            var self = renderer.Process;
            var outbus = self.OutputBusses.First();
            var inreadbus = self.InputBusses.First(x => renderer.Parent.GetLocalBusName(x, renderer.Process) == nameof(SME.Components.SimpleDualPortMemory<int>.ReadControl));
            var inwritebus = self.InputBusses.First(x => renderer.Parent.GetLocalBusName(x, renderer.Process) == nameof(SME.Components.SimpleDualPortMemory<int>.WriteControl));

            string signaltemplate;
            string instancetemplate;
            string gluetemplate;
            string clocktemplate;
            int targetwidth;

            // Single 18k or single 36k item instantiation
            if (in18kBlocks <= 2)
            {
                var config = new BlockRamConfig(renderer, datawidth, datawidth * size, false);
                signaltemplate = GetSignalRegion(config, -1);

                var initlines = VHDLHelper.SplitDataBitStringToMemInit(
                    VHDLHelper.GetDataBitStrings(initialdata),
                    config.datawidth,
                    config.paritybits
                );

                targetwidth = config.realaddrwidth;
                instancetemplate = GetInstantiationRegion(config, renderer.Process.InstanceName, initialvalue, initlines.Item1, initlines.Item2, -1);
                gluetemplate = string.Empty;
                clocktemplate = string.Empty;

            }
            // Multi-unit instantiation
            else
            {
                var addrWidth = (int)Math.Floor(Math.Log(((36 * 1024) + (datawidth - 1)) / datawidth, 2));
                var itemsPrBlock = (int)Math.Pow(2, addrWidth);
                var blocks = (size + (itemsPrBlock - 1)) / itemsPrBlock;

                var sbsignals = new System.Text.StringBuilder();
                var sbinstances = new System.Text.StringBuilder();
                var sbglue = new System.Text.StringBuilder();

                var itemConfig = new BlockRamConfig(renderer, datawidth, itemsPrBlock * datawidth, true);
                sbsignals.Append(GetSignalRegion(itemConfig, -1, fullAddressWidth));
                sbsignals.AppendLine($"signal RDADDR_READ_internal: std_logic_vector({fullAddressWidth - 1} downto 0);");

                for (var i = 0; i < blocks; i++)
                {
                    var config = new BlockRamConfig(renderer, datawidth, itemsPrBlock * datawidth, false);
                    sbsignals.Append(GetSignalRegion(config, i));

                    var initlines = VHDLHelper.SplitDataBitStringToMemInit(
                        VHDLHelper.GetDataBitStrings(initialdata, i * itemsPrBlock, itemsPrBlock),
                        config.datawidth,
                        config.paritybits
                    );

                    sbinstances.Append(GetInstantiationRegion(config, renderer.Process.InstanceName, initialvalue, initlines.Item1, initlines.Item2, i));

                    var indexAsBitString = VHDLHelper.GetDataBitString(i, fullAddressWidth - addrWidth);
                    indexAsBitString = indexAsBitString.Substring(indexAsBitString.Length - (fullAddressWidth - addrWidth));

                    sbglue.AppendLine($@"
RDEN_internal_{i} <= '1' when (RDEN_internal = '1') and (RDADDR_internal({fullAddressWidth - 1} downto {addrWidth}) = ""{indexAsBitString}"") else '0';
WREN_internal_{i} <= '1' when (WREN_internal = '1') and (WRADDR_internal({fullAddressWidth - 1} downto {addrWidth}) = ""{indexAsBitString}"") else '0';

DI_internal_{i} <= DI_internal;
WRADDR_internal_{i} <= WRADDR_internal({addrWidth - 1} downto 0);
RDADDR_internal_{i} <= RDADDR_internal({addrWidth - 1} downto 0);
");
                }

                // Create the output driver
                sbglue.AppendLine(GenerateOutputSelector(self.InstanceName, blocks, fullAddressWidth, addrWidth));

                targetwidth = fullAddressWidth;
                signaltemplate = sbsignals.ToString();
                instancetemplate = sbinstances.ToString();
                gluetemplate = sbglue.ToString();
                clocktemplate = "RDADDR_READ_internal <= RDADDR_internal;";
            }


            var addrpadding =
                fullAddressWidth < targetwidth
                ? string.Format("\"{0}\" & ", new string('0', (targetwidth - fullAddressWidth)))
                : string.Empty;

            var partialaddrsuffix =
                fullAddressWidth > targetwidth
                ? "_partial"
                : string.Empty;

            var template =
$@"
{signaltemplate}

begin

{instancetemplate}

{self.InstanceName}_Helper: process(RST,CLK, RDY)
begin
if RST = '1' then
    FIN <= '0';
elsif rising_edge(CLK) then
    FIN <= not RDY;
    {clocktemplate}
end if;
end process;

{gluetemplate}

RDEN_internal <= ENB and {Naming.ToValidName(renderer.Parent.GetLocalBusName(inreadbus, self) + "_" + nameof(SME.Components.SimpleDualPortMemory<int>.IReadControl.Enabled)) };
WREN_internal <= ENB and {Naming.ToValidName(renderer.Parent.GetLocalBusName(inwritebus, self) + "_" + nameof(SME.Components.SimpleDualPortMemory<int>.IWriteControl.Enabled)) };
DI_internal <= std_logic_vector({ Naming.ToValidName(renderer.Parent.GetLocalBusName(inwritebus, self) + "_" + nameof(SME.Components.SimpleDualPortMemory<int>.IWriteControl.Data)) });
WRADDR_internal <= std_logic_vector(resize(unsigned({ Naming.ToValidName(renderer.Parent.GetLocalBusName(inwritebus, self) + "_" + nameof(SME.Components.SimpleDualPortMemory<int>.IReadControl.Address)) }), {targetwidth}));
RDADDR_internal <= std_logic_vector(resize(unsigned({ Naming.ToValidName(renderer.Parent.GetLocalBusName(inreadbus, self) + "_" + nameof(SME.Components.SimpleDualPortMemory<int>.IWriteControl.Address)) }), {targetwidth}));
{ Naming.ToValidName(renderer.Parent.GetLocalBusName(outbus, self) + "_" + nameof(SME.Components.SimpleDualPortMemory<int>.IReadResult.Data)) } <= {renderer.Parent.VHDLWrappedTypeName(outbus.Signals.First())}(DO_internal);

";

            return VHDLHelper.ReIndentTemplate(template, indentation);
        }
    }
}
