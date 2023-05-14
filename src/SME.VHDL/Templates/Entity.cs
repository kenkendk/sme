using System;
using System.Linq;
using SME.AST;

namespace SME.VHDL.Templates
{
    /// <summary>
    /// Template for entities, e.g. processes.
    /// </summary>
    public class Entity : BaseTemplate
    {
        /// <summary>
        /// The current render state.
        /// </summary>
        public readonly RenderState RS;
        /// <summary>
        /// The current render state of the process to render.
        /// </summary>
        public readonly RenderStateProcess RSP;
        /// <summary>
        /// The network the process belongs to.
        /// </summary>
        public readonly Network Network;
        /// <summary>
        /// The process to render.
        /// </summary>
        public readonly AST.Process Process;

        /// <summary>
        /// Constructs a new instance of the entity template.
        /// </summary>
        /// <param name="renderer">The render state to render in.</param>
        /// <param name="renderproc">The process to render.</param>
        public Entity(RenderState renderer, RenderStateProcess renderproc)
        {
            RS = renderer;
            RSP = renderproc;
            Network = renderer.Network;
            Process = renderproc.Process;
        }

        /// <summary>
        /// Writes the template to the VHDL file.
        /// </summary>
        public override string TransformText()
        {
            GenerationEnvironment = null;
            Write(
@"library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

-- library SYSTEM_TYPES;
use work.SYSTEM_TYPES.ALL;
-- library CUSTOM_TYPES;
use work.CUSTOM_TYPES.ALL;

");

            if (RSP.HasCustomRenderer)
            {
                Write("  -- Component declaration and signals\n");
                Write($"{ToStringHelper.ToStringWithCulture(RSP.CustomRendererInclude)}\n");
            }

            Write(
@"-- User defined packages here
-- #### USER-DATA-IMPORTS-START
-- #### USER-DATA-IMPORTS-END

");
            var procname = ToStringHelper.ToStringWithCulture( Naming.ProcessNameToValidName(Process.SourceInstance.Instance) );
            Write($"entity {procname} is\n");

            var shared = Process.SharedVariables.Cast<object>().Concat(Process.SharedSignals).Concat(Process.SharedConstants);
            var lastel = shared.LastOrDefault();
            if (lastel != null)
            {
                Write("    generic(\n");
                foreach (var variable in Process.SharedVariables)
                {
                    var name = ToStringHelper.ToStringWithCulture( Naming.ToValidName($"reset_{variable.Name}") );
                    var type = ToStringHelper.ToStringWithCulture( RS.VHDLWrappedTypeName(variable) );
                    var end = variable == lastel ? "" : ";";
                    Write($"        {name}: in {type}{end}\n");
                }

                foreach (var constant in Process.SharedConstants)
                {
                    var name = ToStringHelper.ToStringWithCulture( Naming.ToValidName($"reset_{constant.Name}") );
                    var type = ToStringHelper.ToStringWithCulture( RS.VHDLWrappedTypeName(constant) );
                    var end = constant == lastel ? "" : ";";
                    Write($"        {name}: in {type}{end}\n");
                }

                foreach (var variable in Process.SharedSignals)
                {
                    var name = ToStringHelper.ToStringWithCulture( Naming.ToValidName($"reset_{variable.Name}") );
                    var type = ToStringHelper.ToStringWithCulture( RS.VHDLWrappedTypeName(variable) );
                    var end = variable == lastel ? "" : ";";
                    Write($"        {name}: in {type}{end}\n");
                }
                Write("    );\n");
            }

            Write("    port(\n");
            var inputbusses = Process.InputBusses.Where(x => !Process.OutputBusses.Contains(x));
            if (inputbusses.Any())
            {
                foreach (var bus in inputbusses)
                {
                    var busname = RS.GetLocalBusName(bus, Process);
                    Write($"        -- Input bus {ToStringHelper.ToStringWithCulture( busname )} signals\n");
                    foreach (var signal in bus.Signals)
                    {
                        var signalname = ToStringHelper.ToStringWithCulture( Naming.ToValidName($"{busname}_{signal.Name}") );
                        var signaltype = ToStringHelper.ToStringWithCulture( RS.VHDLWrappedTypeName(signal) );
                        var signalquantifier = bus.SourceInstances.Length > 1 ? $"_ARRAY({bus.SourceInstances.Length - 1} downto 0)" : "";
                        Write($"        {signalname}: in {signaltype}{signalquantifier};\n");
                    }
                }
                Write("\n");
            }

            var outputbusses = Process.OutputBusses.Where(x => !Process.InputBusses.Contains(x));
            if (outputbusses.Any())
            {
                foreach (var bus in outputbusses)
                {
                    var busname = RS.GetLocalBusName(bus, Process);
                    Write($"        -- Output bus {ToStringHelper.ToStringWithCulture( busname )} signals\n");
                    foreach (var signal in RSP.WrittenSignals(bus))
                    {
                        var signalname = ToStringHelper.ToStringWithCulture( Naming.ToValidName($"{busname}_{signal.Name}") );
                        var signaltype = ToStringHelper.ToStringWithCulture( RS.VHDLWrappedTypeName(signal) );
                        Write($"        {signalname}: out {signaltype};\n");
                    }
                }
                Write("\n");
            }

            var inoutbusses = Process.InputBusses.Where(x => Process.OutputBusses.Contains(x));
            if (inoutbusses.Any())
            {
                foreach (var bus in inoutbusses)
                {
                    var busname = RS.GetLocalBusName(bus, Process);
                    Write($"        -- Input/output bus {ToStringHelper.ToStringWithCulture( busname )} signals\n");
                    foreach (var signal in bus.Signals)
                    {
                        var signalname = ToStringHelper.ToStringWithCulture( Naming.ToValidName($"{busname}_{signal.Name}") );
                        var signaltype = ToStringHelper.ToStringWithCulture( RS.VHDLWrappedTypeName(signal) );
                        Write($"        {signalname}: in {signaltype};\n");
                    }

                    Write("\n");

                    foreach (var signal in bus.Signals)
                    {
                        var signalname = ToStringHelper.ToStringWithCulture( Naming.ToValidName($"out_{busname}_{signal.Name}") );
                        var signaltype = ToStringHelper.ToStringWithCulture( RS.VHDLWrappedTypeName(signal) );
                        Write($"        {signalname}: out {signaltype};\n");
                    }
                }
                Write("\n");
            }

            Write(
@"        -- Clock signal
        CLK : in Std_logic;

        -- Ready signal
        RDY : in Std_logic;

        -- Finished signal
        FIN : out Std_logic;

        -- Enable signal
        ENB : in Std_logic;

        -- Reset signal
        RST : in Std_logic
    );
end ");

            Write($"{procname};\n\n");
            Write($"architecture RTL of {procname} is \n\n");

            if (RSP.HasCustomRenderer)
                Write($"{ToStringHelper.ToStringWithCulture( RSP.CustomRendererBody )}\n");
            else
            {
                foreach (var bus in Process.InternalBusses)
                {
                    var busname = RS.GetLocalBusName(bus, Process);
                    Write($"    -- Internal bus {ToStringHelper.ToStringWithCulture( busname )} signals\n");
                    foreach (var signal in bus.Signals)
                    {
                        var signalname = ToStringHelper.ToStringWithCulture( Naming.ToValidName(busname + "_" + signal.Name));
                        var signaltype = ToStringHelper.ToStringWithCulture( RS.VHDLWrappedTypeName(signal) );
                        Write($"    signal {signalname}: {signaltype};\n");
                    }
                    Write("\n");
                }

                if (Process.SharedSignals.Any() || Process.InternalDataElements.Any())
                {
                    Write("    -- Internal signals\n");
                    foreach (var s in Process.SharedSignals)
                    {
                        var signalname = ToStringHelper.ToStringWithCulture( s.Name );
                        var signaltype = ToStringHelper.ToStringWithCulture( RS.VHDLWrappedTypeName(s) );
                        Write($"    signal {signalname} : {signaltype};\n");
                    }

                    foreach (var s in Process.InternalDataElements)
                    {
                        var construct = ToStringHelper.ToStringWithCulture( s is AST.Signal ? "signal" : "shared variable" );
                        var signalname = ToStringHelper.ToStringWithCulture( s.Name );
                        var signaltype = ToStringHelper.ToStringWithCulture( RS.VHDLWrappedTypeName(s) );
                        var reset = ToStringHelper.ToStringWithCulture( RS.GetResetExpression(s) );
                        Write($"    {construct} {signalname} : {signaltype} := {reset};\n");
                    }

                    if (RSP.FiniteStateMethod != null)
                        Write("    signal FSM_Trigger : std_logic := \'0\';\n");
                    Write("\n");
                }

                if (Process.SharedConstants.Any())
                {
                    Write("    -- Internal constants\n");
                    foreach (var c in Process.SharedConstants)
                    {
                        var constname = ToStringHelper.ToStringWithCulture( c.Name );
                        constname = Naming.ToValidName(constname);
                        var consttype = ToStringHelper.ToStringWithCulture( RS.VHDLWrappedTypeName(c) );
                        var defaultvalue = $"reset_{constname}";
                        defaultvalue = Naming.ToValidName(defaultvalue);

                        var constant = ToStringHelper.ToStringWithCulture( $"constant {constname} : {consttype} := {defaultvalue}" );
                        Write($"    {constant};\n");
                    }
                    Write("\n");
                }

                if (Process.IsClocked && RSP.FiniteStateMethod != null)
                {
                    Write("    -- Clock-edge capture signals\n");
                    foreach (var bus in Process.InputBusses)
                    {
                        var busname = RS.GetLocalBusName(bus, Process);
                        Write($"    -- Input bus {ToStringHelper.ToStringWithCulture( busname )} signals\n");
                        foreach (var signal in bus.Signals)
                        {
                            var signalname = ToStringHelper.ToStringWithCulture( Naming.ToValidName("capture_" + busname + "_" + signal.Name) );
                            var signaltype = ToStringHelper.ToStringWithCulture( RS.VHDLWrappedTypeName(signal) );
                            var reset = ToStringHelper.ToStringWithCulture( RS.GetResetExpression(signal) );
                            Write($"    signal {signalname}: {signaltype} := {reset};\n");
                        }
                    }
                    Write("\n");
                }

                Write(
@"    -- User defined signals, procedures and components here
    -- #### USER-DATA-SIGNALS-START
    -- #### USER-DATA-SIGNALS-END

begin

    -- Custom processes go here
    -- #### USER-DATA-PROCESSES-START
    -- #### USER-DATA-PROCESSES-END

");

                if (Process.Methods != null && Process.Methods.Any(x => !x.Ignore && x.IsStateMachine))
                {
                    Write("    -- State machine process\n");
                    foreach (var s in Process.Methods.Where(x => !x.Ignore && x.IsStateMachine))
                        foreach(var line in RSP.Helper.RenderStateMachine(s, RSP))
                            Write($"    {ToStringHelper.ToStringWithCulture( line )}\n");
                    Write("\n");
                }

                var sensitivity_signal = RSP.Process.IsClocked ? "CLK" : "RDY";
                var variables = RSP.Variables.Concat(RSP.FiniteStateMethod == null ? RSP.SharedVariables : new Variable[0]);

                Write(
@"    process(
        -- Custom sensitivity signals here
        -- #### USER-DATA-SENSITIVITY-START
        -- #### USER-DATA-SENSITIVITY-END
");

                Write($"        {ToStringHelper.ToStringWithCulture( sensitivity_signal )},\n");
                Write("        RST\n");
                Write("    )\n");

                if (variables.Count() > 0) {
                    Write("        -- Internal variables\n");
                    foreach(var s in variables.Where(x => !x.isLoopIndex)) {
                        var varname = ToStringHelper.ToStringWithCulture( s.Name );
                        var vartype = ToStringHelper.ToStringWithCulture( RS.VHDLWrappedTypeName(s) );
                        if (s.MSCAType.IsArrayType())
                            vartype += $" (0 to {((Array)s.DefaultValue).Length-1})";
                        var reset = ToStringHelper.ToStringWithCulture( Process.SharedVariables.Contains(s) ? " := " + Naming.ToValidName("reset_" + s.Name) : "" );
                        Write($"        variable {varname} : {vartype}{reset};\n");
                    }
                    Write("\n");
                }

                if (!RSP.Process.IsClocked)
                    Write("        variable reentry_guard: std_logic;\n");

                if (Process.Methods != null && Process.Methods.Any(x => !(x.Ignore || x.IsStateMachine)))
                {
                    Write("        -- Internal methods\n");
                    foreach (var s in Process.Methods.Where(x => !(x.Ignore || x.IsStateMachine)))
                        foreach(var line in RSP.Helper.RenderMethod(s))
                            Write($"        {ToStringHelper.ToStringWithCulture( line )}\n");
                    Write("\n");
                }

                Write(@"
        -- #### USER-DATA-NONCLOCKEDVARIABLES-START
        -- #### USER-DATA-NONCLOCKEDVARIABLES-END
    begin
        -- Initialize code here
        -- #### USER-DATA-NONCLOCKEDSHAREDINITIALIZECODE-START
        -- #### USER-DATA-NONCLOCKEDSHAREDINITIALIZECODE-END

        if RST = '1' then
");

                foreach(var s in RSP.ProcessResetStaments)
                    Write($"            {ToStringHelper.ToStringWithCulture( s )}\n");

                var resetvars = RSP.FiniteStateMethod == null ? RSP.SharedVariables : RSP.Variables;
                foreach(var variable in resetvars.Where(x => !x.isLoopIndex))
                {
                    var varname = ToStringHelper.ToStringWithCulture( variable.Name );
                    var vartype = ToStringHelper.ToStringWithCulture( Naming.ToValidName("reset_" + variable.Name) );
                    Write($"            {varname} := {vartype};\n");
                }

                foreach(var variable in Process.SharedSignals)
                {
                    var varname = ToStringHelper.ToStringWithCulture( variable.Name );
                    var vartype = ToStringHelper.ToStringWithCulture( Naming.ToValidName("reset_" + variable.Name) );
                    Write($"            {varname} <= {vartype};\n");
                }

                if (Process.IsClocked && RSP.FiniteStateMethod != null)
                {
                    foreach (var bus in Process.InputBusses)
                    {
                        var busname = RS.GetLocalBusName(bus, Process);
                        foreach (var signal in bus.Signals)
                        {
                            var signalname = ToStringHelper.ToStringWithCulture( Naming.ToValidName("capture_" + busname + "_" + signal.Name) );
                            var reset = ToStringHelper.ToStringWithCulture( RS.GetResetExpression(signal) );
                            Write($"            {signalname} <= {reset};\n");
                        }
                    }
                }

                if (!RSP.Process.IsClocked)
                    Write("            reentry_guard := \'0\';\n");

                if (RSP.FiniteStateMethod == null) {
                    Write("            FIN <= \'0\';\n");
                } else {
                    Write("            FSM_Trigger <= \'0\';\n");
                }

                Write(@"
            -- Initialize code here
            -- #### USER-DATA-NONCLOCKEDRESETCODE-START
            -- #### USER-DATA-NONCLOCKEDRESETCODE-END

");

                if (RSP.Process.IsClocked)
                {
                    Write($"        elsif rising_edge({ToStringHelper.ToStringWithCulture( sensitivity_signal )}) then\n");
                }
                else
                {
                    Write("        elsif reentry_guard /= RDY then\n");
                    Write("            reentry_guard := RDY;\n");
                }

                Write(@"
            -- Initialize code here
            -- #### USER-DATA-NONCLOCKEDINITIALIZECODE-START
            -- #### USER-DATA-NONCLOCKEDINITIALIZECODE-END

");


                foreach(var line in RSP.Helper.RenderMethod(Process.MainMethod))
                    Write($"            {ToStringHelper.ToStringWithCulture( line )}\n");
                Write("\n");

                if (Process.IsClocked && RSP.FiniteStateMethod != null) {
                    Write("            -- Clock-edge capture signals\n");
                    foreach (var bus in Process.InputBusses) {
                        var busname = RS.GetLocalBusName(bus, Process);
                        foreach (var signal in bus.Signals) {
                            var capture = ToStringHelper.ToStringWithCulture( Naming.ToValidName("capture_" + busname + "_" + signal.Name) );
                            var signalname = ToStringHelper.ToStringWithCulture( Naming.ToValidName(busname + "_" + signal.Name) );
                            Write($"            {capture} <= {signalname};\n");
                        }
                    }
                    Write("\n");
                }

                if (RSP.FiniteStateMethod == null)
                    if (RSP.Process.IsClocked)
                        Write("            FIN <= not RDY;\n");
                    else
                        Write("            FIN <= reentry_guard;\n");
                else if (RSP.Process.IsClocked)
                    Write("            FSM_Trigger <= not FSM_Trigger;\n");
                else
                    Write("            FSM_Trigger <= reentry_guard;\n");

                Write("\n");
                Write("        end if;\n");

                Write(@"
        -- Non-clocked process actions here
        -- #### USER-DATA-CODE-START
        -- #### USER-DATA-CODE-END

");

                Write("    end process;\n");
            }

            Write(@"
end RTL;

-- User defined architectures here
-- #### USER-DATA-ARCH-START
-- #### USER-DATA-ARCH-END
");

            return GenerationEnvironment.ToString();
        }
    }
}
