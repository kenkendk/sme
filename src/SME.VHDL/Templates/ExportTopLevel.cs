using System.Linq;
using SME;
using SME.VHDL;
using System.Text;
using System.Collections.Generic;
using SME.AST;
using System;

namespace SME.VHDL.Templates
{

    public class ExportTopLevel : BaseTemplate
    {

        public readonly Network Network;
		public readonly RenderState RS;

		public ExportTopLevel(RenderState renderer)
		{
			RS = renderer;
			Network = renderer.Network;
		}

        public override string TransformText()
        {
            GenerationEnvironment = null;

            Write(@"library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

-- library SYSTEM_TYPES;
use work.SYSTEM_TYPES.ALL;

-- library CUSTOM_TYPES;
use work.CUSTOM_TYPES.ALL;

-- User defined packages here
-- #### USER-DATA-IMPORTS-START
-- #### USER-DATA-IMPORTS-END

");

            var networkname = ToStringHelper.ToStringWithCulture( Network.Name );
            Write($"entity {networkname}_export is\n");
            Write("    port(\n");

            foreach (var bus in Network.Busses.Where(x => x.IsTopLevelInput || x.IsTopLevelOutput))
            {
                var signaltype = "inout";

                if (bus.IsTopLevelInput && !bus.IsTopLevelOutput)
                    signaltype = "in";
                else if (bus.IsTopLevelOutput && !bus.IsTopLevelInput)
                    signaltype = "out";

                var busname = ToStringHelper.ToStringWithCulture( bus.Name );
                Write($"        -- Top-level bus {busname} signals\n");
                foreach (var signal in bus.Signals)
                {
                    var instancename = ToStringHelper.ToStringWithCulture( bus.InstanceName );
                    var signalname = ToStringHelper.ToStringWithCulture( signal.Name );
                    var signaltypename = ToStringHelper.ToStringWithCulture( signaltype );
                    var vhdltype = ToStringHelper.ToStringWithCulture( RS.VHDLExportTypeName(signal) );
                    Write($"        {instancename}_{signalname}: {signaltypename} {vhdltype};\n");
                }
                Write("\n");
            }

            Write(
@"        -- User defined signals here
        -- #### USER-DATA-ENTITYSIGNALS-START
        -- #### USER-DATA-ENTITYSIGNALS-END

        -- Enable signal
        ENB : in STD_LOGIC;

        -- Reset signal
        RST : in STD_LOGIC;

        -- Finished signal
        FIN : out Std_logic;

        -- Clock signal
        CLK : in STD_LOGIC
    );
");

            Write($"end {networkname}_export;\n");

            var converted_outputs = new HashSet<AST.Signal>();

            foreach (var bus in Network.Busses.Where(x => (x.IsTopLevelOutput && !x.IsTopLevelInput) || (x.IsTopLevelInput && x.IsTopLevelOutput)))
                foreach(var signal in bus.Signals)
                {
                    var vt = RS.VHDLType(signal);
                    if (vt.IsSigned || vt.IsUnsigned)
                        converted_outputs.Add(signal);
                }

            Write("\n");
            Write($"architecture RTL of {networkname}_export is\n");
            Write(@"
    -- User defined signals here
    -- #### USER-DATA-SIGNALS-START
    -- #### USER-DATA-SIGNALS-END

");

            if (converted_outputs.Count > 0)
            {
                Write("    -- Intermediate conversion signal to convert internal types to external ones\n");
                foreach(var signal in converted_outputs)
                {
                    var busname = ToStringHelper.ToStringWithCulture( ((AST.Bus)signal.Parent).InstanceName );
                    var signalname = ToStringHelper.ToStringWithCulture( signal.Name );
                    var signaltype = ToStringHelper.ToStringWithCulture( RS.VHDLWrappedTypeName(signal) );
                    Write($"    signal tmp_{busname}_{signalname} : {signaltype};\n");
                }
                Write("\n");
            }

            Write("begin\n");

            if (converted_outputs.Count > 0)
            {
                Write("\n    -- Carry converted signals from entity to wrapped outputs\n");
                foreach(var signal in converted_outputs)
                {
                    var busname = ToStringHelper.ToStringWithCulture( ((AST.Bus)signal.Parent).InstanceName );
                    var signalname = ToStringHelper.ToStringWithCulture( signal.Name );
                    Write($"    {busname}_{signalname} <= std_logic_vector(tmp_{busname}_{signalname});\n");
                }
                Write("\n");
            }

            Write($"    -- Entity {networkname} signals\n");
            Write($"    {networkname}: entity work.{networkname}\n");
            Write("    port map (\n");

            foreach (var bus in Network.Busses.Where(x => x.IsTopLevelInput || x.IsTopLevelOutput))
            {
                var direction = "Input/Output";
                if (bus.IsTopLevelInput && !bus.IsTopLevelOutput)
                    direction = "Input";
                else if (bus.IsTopLevelOutput && !bus.IsTopLevelInput)
                    direction = "Output";

                var directionname = ToStringHelper.ToStringWithCulture( direction );
                var busname = ToStringHelper.ToStringWithCulture( bus.Name );
                Write($"        -- {directionname} bus {busname}\n");

                foreach(var signal in bus.Signals)
                {
                    var vt = RS.VHDLType(signal);
                    var instancename = ToStringHelper.ToStringWithCulture( bus.InstanceName );
                    var signalname = ToStringHelper.ToStringWithCulture( signal.Name );

                    var external = $"{instancename}_{signalname}";
                    if (converted_outputs.Contains(signal))
                        external = $"tmp_{external}";
                    else if (vt.IsUnsigned)
                        external = $"unsigned({external})";
                    else if (vt.IsSigned)
                        external = $"signed({external})";

                    Write($"        {instancename}_{signalname} => {external},\n");
                }
                Write("\n");
            }

            Write(
@"        ENB => ENB,
        RST => RST,
        FIN => FIN,
        CLK => CLK
    );

-- User defined processes here
-- #### USER-DATA-CODE-START
-- #### USER-DATA-CODE-END

end RTL;");

            return GenerationEnvironment.ToString();
        }

    }

}
