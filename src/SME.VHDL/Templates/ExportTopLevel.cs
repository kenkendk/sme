using System;
using System.Linq;
using System.Collections.Generic;
using SME.AST;

namespace SME.VHDL.Templates
{
    /// <summary>
    /// Template for generating the top level export file.
    /// </summary>
    public class ExportTopLevel : BaseTemplate
    {
        /// <summary>
        /// The network to export.
        /// </summary>
        public readonly Network Network;
        /// <summary>
        /// The current render state.
        /// </summary>
        public readonly RenderState RS;

        /// <summary>
        /// Constructs a new instance of the export top level template.
        /// </summary>
        /// <param name="renderer">The render state to render in.</param>
        public ExportTopLevel(RenderState renderer)
        {
            RS = renderer;
            Network = renderer.Network;
        }

        /// <summary>
        /// Writes the template to the VHDL file.
        /// </summary>
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

                var multiplier = bus.SourceInstances.Length;

                foreach (var signal in bus.Signals)
                {
                    var instancename = ToStringHelper.ToStringWithCulture( bus.InstanceName );
                    var signalname = ToStringHelper.ToStringWithCulture( signal.Name );
                    var signaltypename = ToStringHelper.ToStringWithCulture( signaltype );
                    var vhdltype = ToStringHelper.ToStringWithCulture( RS.VHDLExportTypeName(RS.VHDLType(signal), multiplier) );
                    if (signal.MSCAType.IsArrayType())
                    {
                        // https://forums.xilinx.com/t5/Design-Entry/Error-quot-port-is-not-recognized-quot/td-p/956645
                        // "As per UG 1118, - To ensure that the custom IP simulates properly when using VHDL, set the top-level ports to be std_logic or std_logic_vector."
                        var arraylength = RS.GetArrayLength(signal);
                        var arrayvhdltype = RS.VHDLType(signal);
                        var elementtype = RS.TypeScope.GetByName(arrayvhdltype.ElementName);
                        var elementtypename = RS.VHDLExportTypeName(elementtype);
                        for (int i = 0; i < arraylength; i++)
                            Write($"        {instancename}_{signalname}{i}: {signaltypename} {elementtypename};\n");
                    }
                    else
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

            var converted_outputs = new HashSet<AST.BusSignal>();

            // Add all of the top level output busses
            // as they need to be converted trough a tmp signal
            foreach (var bus in Network.Busses.Where(x => x.IsTopLevelOutput))
            {
                foreach(var signal in bus.Signals)
                {
                    var vt = RS.VHDLType(signal);
                    if (vt.IsSigned || vt.IsUnsigned)
                        converted_outputs.Add(signal);
                }
            }

            // The same goes for arrays in busses
            foreach (var signal in Network.Busses
                .Where(x => x.IsTopLevelInput || x.IsTopLevelOutput)
                .SelectMany(x => x.Signals
                    .Where(y => y.MSCAType.IsArrayType())
                ))
            {
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

            // Emit conversion functions for arrays of busses
            // Input busses
            foreach (var bus in Network.Busses.Where(x => x.IsTopLevelInput && x.SourceInstances.Length > 1))
            {
                var arraylength = bus.SourceInstances.Length;
                foreach (var signal in bus.Signals)
                {
                    var busname = ToStringHelper.ToStringWithCulture( bus.InstanceName );
                    var signalname = ToStringHelper.ToStringWithCulture( signal.Name );
                    var signaltypename = ToStringHelper.ToStringWithCulture( RS.VHDLWrappedTypeName(signal) );
                    var signaltype = RS.VHDLType(signal);
                    var typecast = RS.VHDLExportTypeCast(signaltype);
                    var slice = signaltype.Length > 1 ? $"(i*{signaltype.Length})+{signaltype.Length-1} downto (i*{signaltype.Length})" : "i";
                    Write($@"
    pure function {busname}_{signalname}_conversion(slv : in std_logic_vector) return {signaltypename}_ARRAY is
        variable tmp : {signaltypename}_ARRAY(0 to {arraylength-1});
    begin
        for i in 0 to {arraylength-1} loop
            tmp(i) := {typecast}(slv({slice}));
        end loop;
        return tmp;
    end function;
");
                }
            }

            Write("begin\n");

            if (converted_outputs.Count > 0)
            {
                Write("\n    -- Carry converted signals from entity to wrapped outputs\n");
                foreach(var signal in converted_outputs)
                {
                    var bus = (AST.Bus)signal.Parent;
                    var busname = ToStringHelper.ToStringWithCulture( bus.InstanceName );
                    var signalname = ToStringHelper.ToStringWithCulture( signal.Name );
                    if (signal.MSCAType.IsArrayType())
                    {
                        var arraylength = RS.GetArrayLength(signal);
                        var arrayvhdltype = RS.VHDLType(signal);
                        var elementtype = RS.TypeScope.GetByName(arrayvhdltype.ElementName);
                        var typecast = RS.VHDLExportTypeCast(elementtype);

                        if (bus.IsTopLevelInput)
                            for (int i = 0; i < arraylength; i++)
                                Write($"    tmp_{busname}_{signalname}({i}) <= {typecast}({busname}_{signalname}{i});\n");

                        if (bus.IsTopLevelOutput)
                            for (int i = 0; i < arraylength; i++)
                                Write($"    {busname}_{signalname}{i} <= std_logic_vector(tmp_{busname}_{signalname}({i}));\n");
                    }
                    else
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

                var multiplier = bus.SourceInstances.Length;

                foreach(var signal in bus.Signals)
                {
                    var vt = RS.VHDLType(signal);
                    var instancename = ToStringHelper.ToStringWithCulture( bus.InstanceName );
                    var signalname = ToStringHelper.ToStringWithCulture( signal.Name );

                    var external = $"{instancename}_{signalname}";
                    if (multiplier > 1)
                        external = $"{external}_conversion({external})";
                    else if (converted_outputs.Contains(signal))
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
