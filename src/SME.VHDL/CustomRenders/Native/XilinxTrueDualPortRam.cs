using System;
using System.Collections.Generic;
using System.Linq;

namespace SME.VHDL.CustomRenders.Native
{
    /// <summary>
    /// Class for generating a Xilinx single port RAM by using macros.
    /// </summary>
    public class XilinxTrueDualPortRam : ICustomRenderer
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
signal ENA_internal{index_suffix}: std_logic;
signal WEA_internal{index_suffix}: std_logic_vector({config.wewidth - 1} downto 0);
signal DIA_internal{index_suffix}: std_logic_vector({config.datawidth - 1} downto 0);
signal DOA_internal{index_suffix}: std_logic_vector({config.datawidth - 1} downto 0);
signal ADDRA_internal{index_suffix}: std_logic_vector({(overrideAddrWidth <= 0 ? (config.realaddrwidth - 1) : (overrideAddrWidth - 1))} downto 0);

signal ENB_internal{index_suffix}: std_logic;
signal WEB_internal{index_suffix}: std_logic_vector({config.wewidth - 1} downto 0);
signal DIB_internal{index_suffix}: std_logic_vector({config.datawidth - 1} downto 0);
signal DOB_internal{index_suffix}: std_logic_vector({config.datawidth - 1} downto 0);
signal ADDRB_internal{index_suffix}: std_logic_vector({(overrideAddrWidth <= 0 ? (config.realaddrwidth - 1) : (overrideAddrWidth - 1))} downto 0);
";
        }

        /// <summary>
        /// Creates VHDL code that chooses the block ram component data results with the top bits.
        /// </summary>
        /// <returns>The output selector.</returns>
        /// <param name="instancename">The name of the instance to create.</param>
        /// <param name="port">The port to use</param>
        /// <param name="blocks">The number of blocks.</param>
        /// <param name="fullAddressWidth">The full address width.</param>
        /// <param name="blockAddrWidth">The block address width.</param>
        private string GenerateOutputSelector(string instancename, string port, int blocks, int fullAddressWidth, int blockAddrWidth)
        {
            var cases = Enumerable
                .Range(0, blocks)
                .Select(i => $@"    when ""{VHDLHelper.GetDataBitString(i, fullAddressWidth - blockAddrWidth).Substring(32 - (fullAddressWidth - blockAddrWidth))}"" =>
        DO{port}_internal <= DO{port}_internal_{i};");

            return $@"
{instancename}{port}_Output: process(ADDR{port}_READ_internal, {string.Join(", ", Enumerable.Range(0, blocks).Select(x => $"DO{port}_internal_{x}"))})
begin
    case ADDR{port}_READ_internal({fullAddressWidth - 1} downto {blockAddrWidth}) is
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
{instancename}_inst{index_suffix} : BRAM_TDP_MACRO
generic map (
    BRAM_SIZE => ""{(config.use36k ? "36Kb" : "18Kb")}"", -- Target BRAM, ""18Kb"" or ""36Kb""
    DEVICE => ""{ config.targetdevice }"", --Target device: ""VIRTEX5"", ""VIRTEX6"", ""7SERIES"", ""SPARTAN6""
    DOA_REG => 0, --Optional port A output register(0 or 1)
    DOB_REG => 0, --Optional port B output register(0 or 1)
    INIT_FILE => ""NONE"",
    READ_WIDTH_A => { config.datawidth },     -- Valid values are 1 - 36(19 - 36 only valid when BRAM_SIZE = ""36Kb"")
    READ_WIDTH_B => { config.datawidth },     -- Valid values are 1 - 36(19 - 36 only valid when BRAM_SIZE = ""36Kb"")
    SIM_COLLISION_CHECK => ""GENERATE_X_ONLY"", --Collision check enable ""ALL"", ""WARNING_ONLY"",
                                 --""GENERATE_X_ONLY"" or ""NONE""
    SRVAL_A => X""{ initialvalue}"", --Set / Reset value for A port output
    SRVAL_B => X""{ initialvalue}"", --Set / Reset value for B port output
    WRITE_MODE_A => ""READ_FIRST"", -- ""WRITE_FIRST"", ""READ_FIRST"" or ""NO_CHANGE""
    WRITE_MODE_B => ""READ_FIRST"", -- ""WRITE_FIRST"", ""READ_FIRST"" or ""NO_CHANGE""
    WRITE_WIDTH_A => { config.datawidth },     -- Valid values are 1 - 36(19 - 36 only valid when BRAM_SIZE = ""36Kb"")
    WRITE_WIDTH_B => { config.datawidth },     -- Valid values are 1 - 36(19 - 36 only valid when BRAM_SIZE = ""36Kb"")

    -- The following INIT_xx declarations specify the initial contents of the RAM
{ memlines },

    -- The next set of INITP_xx are for the parity bits
{ paritylines },

    INIT_A => X""{ initialvalue}"", --Initial values on A output port
    INIT_B => X""{ initialvalue}""  --Initial values on B output port
)
port map (
    DOA => DOA_internal{index_suffix},         -- Output port-A, width defined by READ_WIDTH_A parameter
    DOB => DOB_internal{index_suffix},         -- Output port-B, width defined by READ_WIDTH_B parameter
    ADDRA => ADDRA_internal{index_suffix},     -- Input port-A address, width defined by Port A depth
    ADDRB => ADDRB_internal{index_suffix},     -- Input port-B address, width defined by Port B depth
    CLKA => CLK,                 -- 1-bit input port-A clock
    CLKB => CLK,                 -- 1-bit input port-B clock
    DIA => DIA_internal{index_suffix},         -- Input port-A data, width defined by WRITE_WIDTH_A parameter
    DIB => DIB_internal{index_suffix},         -- Input port-B data, width defined by WRITE_WIDTH_B parameter
    ENA => ENA_internal{index_suffix},         -- 1-bit input port-A enable
    ENB => ENB_internal{index_suffix},         -- 1-bit input port-B enable
    REGCEA => '0',               -- 1-bit input port-A register enable
    REGCEB => '0',               -- 1-bit input port-B register enable
    RSTA => RST,                 -- 1-bit input port-A reset
    RSTB => RST,                 -- 1-bit input port-B reset
    WEA => WEA_internal{index_suffix},         -- Input port-A write enable, width defined by port A depth
    WEB => WEB_internal{index_suffix}          -- Input port-B write enable, width defined by port B depth
);
-- End of BRAM_TDP_MACRO_inst instantiation
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

            var resetinitial = renderer.Process.LocalBusNames.Keys.Where(x => x.SourceType.Name == nameof(SME.Components.TrueDualPortMemory<int>.IReadResult)).SelectMany(x => x.Signals).First(x => x.Name == nameof(SME.Components.TrueDualPortMemory<int>.IReadResult.Data)).DefaultValue;
            var initialvalue = VHDLHelper.GetDataBitString(initialdata.GetType().GetElementType(), resetinitial, datawidth);

            var itemsPr18k = ((18 * 1024) + (datawidth - 1)) / datawidth;
            var in18kBlocks = (size + (itemsPr18k - 1)) / itemsPr18k;

            var fullAddressWidth = (int)Math.Ceiling(Math.Log(size, 2));

            var self = renderer.Process;
            var outabus = self.OutputBusses.First(x => renderer.Parent.GetLocalBusName(x, renderer.Process) == nameof(SME.Components.TrueDualPortMemory<int>.ReadResultA));
            var outbbus = self.OutputBusses.First(x => renderer.Parent.GetLocalBusName(x, renderer.Process) == nameof(SME.Components.TrueDualPortMemory<int>.ReadResultB));

            var inabus = self.InputBusses.First(x => renderer.Parent.GetLocalBusName(x, renderer.Process) == nameof(SME.Components.TrueDualPortMemory<int>.ControlA));
            var inbbus = self.InputBusses.First(x => renderer.Parent.GetLocalBusName(x, renderer.Process) == nameof(SME.Components.TrueDualPortMemory<int>.ControlB));

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
                sbsignals.AppendLine($"signal ADDRA_READ_internal: std_logic_vector({fullAddressWidth - 1} downto 0);");
                sbsignals.AppendLine($"signal ADDRB_READ_internal: std_logic_vector({fullAddressWidth - 1} downto 0);");

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
ENA_internal_{i} <= '1' when (ENA_internal = '1') and (ADDRA_internal({fullAddressWidth - 1} downto {addrWidth}) = ""{indexAsBitString}"") else '0';
WEA_internal_{i} <= WEA_internal;
DIA_internal_{i} <= DIA_internal;
ADDRA_internal_{i} <= ADDRA_internal({addrWidth - 1} downto 0);

ENB_internal_{i} <= '1' when (ENB_internal = '1') and (ADDRB_internal({fullAddressWidth - 1} downto {addrWidth}) = ""{indexAsBitString}"") else '0';
WEB_internal_{i} <= WEB_internal;
DIB_internal_{i} <= DIB_internal;
ADDRB_internal_{i} <= ADDRB_internal({addrWidth - 1} downto 0);
                    ");
                }

                // Create the output driver
                sbglue.AppendLine(GenerateOutputSelector(self.InstanceName, "A", blocks, fullAddressWidth, addrWidth));
                sbglue.AppendLine(GenerateOutputSelector(self.InstanceName, "B", blocks, fullAddressWidth, addrWidth));

                targetwidth = fullAddressWidth;
                signaltemplate = sbsignals.ToString();
                instancetemplate = sbinstances.ToString();
                gluetemplate = sbglue.ToString();
                clocktemplate = $"ADDRA_READ_internal <= ADDRA_internal;{Environment.NewLine}    ADDRB_READ_internal <= ADDRB_internal;";
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

ENA_internal <= ENB and {Naming.ToValidName(renderer.Parent.GetLocalBusName(inabus, self) + "_" + nameof(SME.Components.TrueDualPortMemory<int>.IControl.Enabled)) };
WEA_internal <= (others => ENA_internal and {Naming.ToValidName(renderer.Parent.GetLocalBusName(inabus, self) + "_" + nameof(SME.Components.TrueDualPortMemory<int>.IControl.IsWriting)) });
DIA_internal <= std_logic_vector({ Naming.ToValidName(renderer.Parent.GetLocalBusName(inabus, self) + "_" + nameof(SME.Components.TrueDualPortMemory<int>.IControl.Data)) });
ADDRA_internal <= std_logic_vector(resize(unsigned({ Naming.ToValidName(renderer.Parent.GetLocalBusName(inabus, self) + "_" + nameof(SME.Components.TrueDualPortMemory<int>.IControl.Address)) }), {targetwidth}));
{ Naming.ToValidName(renderer.Parent.GetLocalBusName(outabus, self) + "_" + nameof(SME.Components.TrueDualPortMemory<int>.IReadResult.Data)) } <= {renderer.Parent.VHDLWrappedTypeName(outabus.Signals.First())}(DOA_internal);

ENB_internal <= ENB and {Naming.ToValidName(renderer.Parent.GetLocalBusName(inbbus, self) + "_" + nameof(SME.Components.TrueDualPortMemory<int>.IControl.Enabled)) };
WEB_internal <= (others => ENB_internal and {Naming.ToValidName(renderer.Parent.GetLocalBusName(inbbus, self) + "_" + nameof(SME.Components.TrueDualPortMemory<int>.IControl.IsWriting)) });
DIB_internal <= std_logic_vector({ Naming.ToValidName(renderer.Parent.GetLocalBusName(inbbus, self) + "_" + nameof(SME.Components.TrueDualPortMemory<int>.IControl.Data)) });
ADDRB_internal <= std_logic_vector(resize(unsigned({ Naming.ToValidName(renderer.Parent.GetLocalBusName(inbbus, self) + "_" + nameof(SME.Components.TrueDualPortMemory<int>.IControl.Address)) }), {targetwidth}));
{ Naming.ToValidName(renderer.Parent.GetLocalBusName(outbbus, self) + "_" + nameof(SME.Components.TrueDualPortMemory<int>.IReadResult.Data)) } <= {renderer.Parent.VHDLWrappedTypeName(outbbus.Signals.First())}(DOB_internal);

";

            return VHDLHelper.ReIndentTemplate(template, indentation);
        }
    }
}
