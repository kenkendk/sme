using System;
using System.Collections.Generic;
using System.Linq;

namespace SME.VHDL.CustomRenders.Native
{
    /// <summary>
    /// Class for generating a Xilinx single port RAM by using macros.
    /// </summary>
    public class XilinxSinglePortRam : ICustomRenderer
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
signal EN_internal{index_suffix}: std_logic;
signal WE_internal{index_suffix}: std_logic_vector({config.wewidth - 1} downto 0);
signal DI_internal{index_suffix}: std_logic_vector({config.datawidth - 1} downto 0);
signal DO_internal{index_suffix}: std_logic_vector({config.datawidth - 1} downto 0);
signal ADDR_internal{index_suffix}: std_logic_vector({(overrideAddrWidth <= 0 ? (config.realaddrwidth - 1) : (overrideAddrWidth - 1))} downto 0);
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
{instancename}_Output: process(ADDR_READ_internal, {string.Join(", ", Enumerable.Range(0, blocks).Select(x => $"DO_internal_{x}"))})
begin
    case ADDR_READ_internal({fullAddressWidth - 1} downto {blockAddrWidth}) is
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

            var index_suffix = index < 0 ? string.Empty : $"_{index}";

            return
$@"
{instancename}_inst{index_suffix} : BRAM_SINGLE_MACRO
generic map (
    BRAM_SIZE => ""{(config.use36k ? "36Kb" : "18Kb")}"", -- Target BRAM, ""18Kb"" or ""36Kb""
    DEVICE => ""{ config.targetdevice }"", --Target device: ""VIRTEX5"", ""VIRTEX6"", ""7SERIES"", ""SPARTAN6""
    DO_REG => 0, --Optional output register(0 or 1)
    WRITE_WIDTH => { config.datawidth },    --Valid values are 1 - 72(37 - 72 only valid when BRAM_SIZE = ""36Kb"")
    READ_WIDTH => { config.datawidth },     --Valid values are 1 - 72(37 - 72 only valid when BRAM_SIZE = ""36Kb"")
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
    DO => DO_internal{index_suffix},         -- Output read data port, width defined by READ_WIDTH parameter
    DI => DI_internal{index_suffix},         -- Input write data port, width defined by WRITE_WIDTH parameter
    ADDR => ADDR_internal{index_suffix},     -- Input address, width defined by read/write port depth
    CLK => CLK,                -- 1-bit input clock
    EN => EN_internal{index_suffix},         -- 1-bit input enable
    REGCE => '0',              -- 1-bit input read output register enable
    RST => RST,                -- 1-bit input reset
    WE => WE_internal{index_suffix}          -- Input write enable, width defined by write port depth
);
-- End of BRAM_SINGLE_MACRO instantiation
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

            var resetinitial = renderer.Process.LocalBusNames.Keys.Where(x => x.SourceType.Name == nameof(SME.Components.SinglePortMemory<int>.IReadResult)).SelectMany(x => x.Signals).First().DefaultValue;
            var initialvalue = VHDLHelper.GetDataBitString(initialdata.GetType().GetElementType(), resetinitial, datawidth);

            var itemsPr18k = ((18 * 1024) + (datawidth - 1)) / datawidth;
            var in18kBlocks = (size + (itemsPr18k - 1)) / itemsPr18k;

            var fullAddressWidth = (int)Math.Ceiling(Math.Log(size, 2));

            var self = renderer.Process;
            var outbus = self.OutputBusses.First();
            var inbus = self.InputBusses.First();

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
                sbsignals.AppendLine($"signal ADDR_READ_internal: std_logic_vector({fullAddressWidth - 1} downto 0);");

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
EN_internal_{i} <= '1' when (EN_internal = '1') and (ADDR_internal({fullAddressWidth - 1} downto {addrWidth}) = ""{indexAsBitString}"") else '0';
WE_internal_{i} <= WE_internal;
DI_internal_{i} <= DI_internal;
ADDR_internal_{i} <= ADDR_internal({addrWidth - 1} downto 0);
");
                }

                // Create the output driver
                sbglue.AppendLine(GenerateOutputSelector(self.InstanceName, blocks, fullAddressWidth, addrWidth));

                targetwidth = fullAddressWidth;
                signaltemplate = sbsignals.ToString();
                instancetemplate = sbinstances.ToString();
                gluetemplate = sbglue.ToString();
                clocktemplate = "ADDR_READ_internal <= ADDR_internal;";
            }

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

EN_internal <= ENB and {Naming.ToValidName(renderer.Parent.GetLocalBusName(inbus, self) + "_" + nameof(SME.Components.SinglePortMemory<int>.IControl.Enabled)) };
WE_internal <= (others => EN_internal and {Naming.ToValidName(renderer.Parent.GetLocalBusName(inbus, self) + "_" + nameof(SME.Components.SinglePortMemory<int>.IControl.IsWriting)) });
DI_internal <= std_logic_vector({ Naming.ToValidName(renderer.Parent.GetLocalBusName(inbus, self) + "_" + nameof(SME.Components.SinglePortMemory<int>.IControl.Data)) });
ADDR_internal <= std_logic_vector(resize(unsigned({ Naming.ToValidName(renderer.Parent.GetLocalBusName(inbus, self) + "_" + nameof(SME.Components.SinglePortMemory<int>.IControl.Address)) }), {targetwidth}));
{ Naming.ToValidName(renderer.Parent.GetLocalBusName(outbus, self) + "_" + nameof(SME.Components.SinglePortMemory<int>.IReadResult.Data)) } <= {renderer.Parent.VHDLWrappedTypeName(outbus.Signals.First())}(DO_internal);

";

            return VHDLHelper.ReIndentTemplate(template, indentation);
        }
    }
}
