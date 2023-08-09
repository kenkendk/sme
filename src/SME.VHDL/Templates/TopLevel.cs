using System;
using System.Collections.Generic;
using System.Linq;
using SME.AST;

namespace SME.VHDL.Templates
{
    /// <summary>
    /// Template for generating the top level VHDL file.
    /// </summary>
    public class TopLevel : BaseTemplate
    {
        /// <summary>
        /// The network to render.
        /// </summary>
        public readonly Network Network;
        /// <summary>
        /// The current render state.
        /// </summary>
        public readonly RenderState RS;

        /// <summary>
        /// Constructs a new instance of the top level template.
        /// </summary>
        /// <param name="renderer">The render state to render in.</param>
        public TopLevel(RenderState renderer)
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

            Write(
@"library IEEE;
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
            var feedbacks = RS.FeedbackBusses.ToArray();
            var processes = Network.Processes.Where(x => !x.IsSimulation).ToArray();
            var tmps = processes.SelectMany(x => x.InputBusses.Keys.Where(y => y.IsTopLevelOutput)).Distinct().ToArray();

            Write($"entity {networkname} is\n");
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

                // TODO An array of buses can only be used by one process at the time? It shouldn't be a problem for the readers, but the writers will break the multiple writer analysis
                var quantifier = bus.SourceInstances.Length > 1 ? $"_ARRAY({bus.SourceInstances.Length-1} downto 0)" : "";

                foreach (var signal in bus.Signals)
                {
                    var signalname = ToStringHelper.ToStringWithCulture( Naming.ToValidName($"{bus.InstanceName}_{signal.Name}") );
                    var signaltypename = ToStringHelper.ToStringWithCulture( signaltype );
                    var vhdltype = ToStringHelper.ToStringWithCulture( RS.VHDLWrappedTypeName(signal) );
                    Write($"        {signalname}: {signaltypename} {vhdltype}{quantifier};\n");
                }

                Write("\n");
            }

            foreach (var bus in Network.Busses.Where(x => !(x.IsTopLevelInput || x.IsTopLevelOutput || x.IsInternal)))
            {
                var busname = ToStringHelper.ToStringWithCulture( bus.Name );
                Write($"        -- Interconnection bus {busname} signals\n");

                var quantifier = bus.SourceInstances.Length > 1 ? $"_ARRAY({bus.SourceInstances.Length-1} downto 0)" : "";

                foreach (var signal in bus.Signals)
                {
                    var signalname = ToStringHelper.ToStringWithCulture( Naming.ToValidName($"{bus.InstanceName}_{signal.Name}") );
                    var vhdltype = ToStringHelper.ToStringWithCulture( RS.VHDLWrappedTypeName(signal) );
                    Write($"        {signalname}: inout {vhdltype}{quantifier};\n");
                }

                Write("\n");
            }


            Write(
@"        -- User defined signals here
        -- #### USER-DATA-ENTITYSIGNALS-START
        -- #### USER-DATA-ENTITYSIGNALS-END

        -- Enable signal
        ENB : in Std_logic;

        -- Finished signal
        FIN : out Std_logic;

        -- Reset signal
        RST : in Std_logic;

        -- Clock signal
        CLK : in Std_logic
    );
");

            Write($"end {networkname};\n");
            Write("\n");

            Write($"architecture RTL of {networkname} is\n");

            Write(@"
    -- User defined signals here
    -- #### USER-DATA-SIGNALS-START
    -- #### USER-DATA-SIGNALS-END

");

            if (feedbacks.Any())
            {
                Write("    -- Feedback signals\n");
                foreach (var signal in feedbacks.SelectMany(x => x.Signals))
                {
                    var signalname = $"{(signal.Parent as AST.Bus).InstanceName}_{signal.Name}";
                    var current = ToStringHelper.ToStringWithCulture( Naming.ToValidName( $"current_{signalname}" ));
                    var next = ToStringHelper.ToStringWithCulture( Naming.ToValidName( $"next_{signalname}" ));
                    var vhdltype = ToStringHelper.ToStringWithCulture( RS.VHDLWrappedTypeName(signal) );
                    Write($"    signal {current}, {next}: {vhdltype};\n");
                }
                Write("\n");
            }

            Write("    -- Process ready triggers\n");
            foreach (var p in processes) {
                var fin = ToStringHelper.ToStringWithCulture( Naming.ToValidName($"FIN_{p.InstanceName}") );
                var rdy = Naming.ToValidName($"RDY_{p.InstanceName}");
                rdy = ToStringHelper.ToStringWithCulture( p.IsClocked ? "" : $", {rdy}" );
                Write($"    signal {fin}{rdy} : std_logic;\n");
            }
            Write("\n");

            foreach (var bus in processes.SelectMany(x => x.OutputBusses.Keys.Intersect(x.InputBusses.Keys).Except(feedbacks)))
            {
                var busname = ToStringHelper.ToStringWithCulture( bus.Name );
                Write($"    -- Bus {busname} intermediate signals\n");
                foreach(var signal in bus.Signals)
                {
                    var signalname = ToStringHelper.ToStringWithCulture( Naming.ToValidName($"next_{bus.InstanceName}_{signal.Name}") );
                    var vhdltype = ToStringHelper.ToStringWithCulture( RS.VHDLWrappedTypeName(signal) );
                    Write($"    signal {signalname}: {vhdltype};\n");
                }
                Write("\n");
            }

            foreach (var bus in tmps)
            {
                foreach (var signal in bus.Signals)
                {
                    var signalname = ToStringHelper.ToStringWithCulture( Naming.ToValidName($"tmp_{bus.InstanceName}_{signal.Name}") );
                    var vhdltype = ToStringHelper.ToStringWithCulture( RS.VHDLWrappedTypeName(signal) );
                    Write($"    signal {signalname}: {vhdltype};\n");
                }
                Write("\n");
            }

Write(@"    -- The primary ready driver signal
    signal RDY : std_logic;

begin

");

            foreach (var p in processes)
            {
                var instance = ToStringHelper.ToStringWithCulture( p.InstanceName );
                var instancename = ToStringHelper.ToStringWithCulture( Naming.ToValidName(p.InstanceName) );
                var processname = ToStringHelper.ToStringWithCulture( Naming.ProcessNameToValidName(p.SourceInstance.Instance) );
                Write($"    -- Entity {instance} signals\n");
                Write($"    {instancename}: entity work.{processname}\n");
                List<string> generic_map = new List<string>();
                foreach (var variable in p.SharedVariables)
                {
                    var name = ToStringHelper.ToStringWithCulture( Naming.ToValidName($"reset_{variable.Name}") );
                    if (variable.MSCAType.IsArrayType())
                        name += $" (0 to {((Array)variable.DefaultValue).Length-1})";
                    var resetvar = ToStringHelper.ToStringWithCulture( RS.GetResetExpression(variable) );
                    generic_map.Add($"        {name} => {resetvar}");
                }

                foreach (var constant in p.SharedConstants)
                {
                    // TODO array of busses length sometimes emitted to processes, that don't need it.
                    if (constant.Source is IBus)
                    {
                        var bus = constant.Source as IBus;
                        var allBusses = p.InputBusses.Concat(p.OutputBusses).Concat(p.InternalBusses).Where(x => x.Value.Length == 1);
                        var busindices = allBusses.FirstOrDefault(x => x.Key.SourceInstances.Contains(bus)).Value;
                        // TODO if the array of buses is length 1 on a global scale, this won't work.
                        if (busindices != null)
                            continue;
                    }
                    var name = ToStringHelper.ToStringWithCulture( Naming.ToValidName($"reset_{constant.Name}") );
                    if (constant.MSCAType.IsArrayType() && ((Array)constant.DefaultValue).Length > 1)
                        name += $" (0 to {((Array)constant.DefaultValue).Length-1})";
                    var resetvar = ToStringHelper.ToStringWithCulture( RS.GetResetExpression(constant) );
                    generic_map.Add($"        {name} => {resetvar}");
                }

                foreach (var variable in p.SharedSignals)
                {
                    var name = ToStringHelper.ToStringWithCulture( Naming.ToValidName($"reset_{variable.Name}") );
                    var resetvar = ToStringHelper.ToStringWithCulture( RS.GetResetExpression(variable) );
                    generic_map.Add($"        {name} => {resetvar}");
                }

                if (generic_map.Any()) {
                    Write("    generic map(\n");
                    Write(string.Join(",\n", generic_map));
                    Write("\n    )\n");
                }

                Write("    port map (\n");
                foreach (var (bus, indices) in p.InputBusses.Union(p.OutputBusses))
                {
                    var isInput = p.InputBusses.Keys.Contains(bus);
                    var isOutput = p.OutputBusses.Keys.Contains(bus);
                    var isBoth = isInput && isOutput;
                    var type = "Input/Output";
                    if (isInput && !isOutput)
                        type = "Input";
                    else if (isOutput && !isInput)
                        type = "Output";

                    var output_prefix = string.Empty;
                    var input_prefix = string.Empty;
                    if (feedbacks.Contains(bus))
                        input_prefix = output_prefix = "current_";
                    else if (tmps.Contains(bus))
                        input_prefix = output_prefix = "tmp_";

                    var busname = RS.GetLocalBusName(bus, p);
                    var signals = bus.Signals.AsEnumerable();

                    if (isOutput && !isBoth)
                        signals = RS.WrittenSignals(p, bus);

                    var direction = ToStringHelper.ToStringWithCulture( type );
                    var name = ToStringHelper.ToStringWithCulture( bus.Name );
                    Write($"        -- {direction} bus {name}\n");

                    // TODO I would like to interchange the loops, but VHDL
                    // doesn't allow it. The ports need to be contiguous. Which
                    // I guess means that we can't do this with scrambled
                    // indices? I.e. Views? In that case, it may just be easier
                    // to have all of the arrays unrolled here in the toplevel,
                    // as the arrays won't be scrambled inside of a process.
                    foreach(var signal in signals)
                    for (int i = 0; i < indices.Length; i++)
                    {
                        var global_idx = bus.SourceInstances.Length > 1 ? $"({indices[i]})" : string.Empty;
                        var local_idx = indices.Length > 1 ? $"({i})" : string.Empty;
                        var prefix = isInput || isBoth ? input_prefix : output_prefix;
                        var signalname = ToStringHelper.ToStringWithCulture( Naming.ToValidName($"{busname}_{signal.Name}") );
                        var signalinstancename = ToStringHelper.ToStringWithCulture( Naming.ToValidName($"{prefix}{bus.InstanceName}_{signal.Name}") );
                        Write($"        {signalname}{local_idx} => {signalinstancename}{global_idx},\n");
                    }

                    if (isBoth)
                    {
                        foreach(var signal in bus.Signals)
                        {
                            var signalout = ToStringHelper.ToStringWithCulture( Naming.ToValidName($"out_{busname}_{signal.Name}") );
                            var signalnext = ToStringHelper.ToStringWithCulture( Naming.ToValidName($"next_{bus.InstanceName}_{signal.Name}") );
                            Write($"        {signalout} => {signalnext},\n");
                        }
                    }
                    Write("\n");
                }

                var rdy = ToStringHelper.ToStringWithCulture( p.IsClocked ? "RDY" : Naming.ToValidName($"RDY_{p.InstanceName}") );
                var fin = ToStringHelper.ToStringWithCulture( Naming.ToValidName($"FIN_{p.InstanceName}") );
                Write($"        CLK => CLK,\n");
                Write($"        RDY => {rdy},\n");
                Write($"        FIN => {fin},\n");
                Write($"        ENB => ENB,\n");
                Write($"        RST => RST\n");
                Write($"    );\n");
                Write($"\n");
            }

            Write("    -- Connect ready signals\n");
            foreach (var p in processes)
            {
                var parents = RS.DependsOn(p).Select(x => x.InstanceName).Distinct().ToArray();
                if (parents.Length == 0)
                {
                    if (!p.IsClocked)
                    {
                        var rdy = ToStringHelper.ToStringWithCulture( Naming.ToValidName($"RDY_{p.InstanceName}") );
                        Write($"    {rdy} <= RDY;\n");
                    }
                }
                else if (parents.Length == 1)
                {
                    var rdy = ToStringHelper.ToStringWithCulture( Naming.ToValidName($"RDY_{p.InstanceName}") );
                    var fin = ToStringHelper.ToStringWithCulture( Naming.ToValidName($"FIN_{parents.First()}") );
                    Write($"    {rdy} <= {fin};\n");
                }
                else
                {
                    var rdy = ToStringHelper.ToStringWithCulture( Naming.ToValidName($"RDY_{p.InstanceName}") );
                    var fin = ToStringHelper.ToStringWithCulture( Naming.ToValidName($"FIN_{parents.First()}") );
                    var zipped = parents.Skip(1)
                        .Zip(
                            parents.SkipLast(1),
                            (a, b) => {
                                var namea = Naming.ToValidName($"FIN_{a}");
                                var nameb = Naming.ToValidName($"FIN_{b}");
                                return $"{namea} = {nameb}";
                            }
                        );
                    var parentsfin = string.Join(" AND ", zipped);
                    Write($"    {rdy} <= {fin} when {parentsfin};\n");
                }
            }
            Write("\n");

            Write("    -- Setup the FIN feedback signals\n");
            var first_fin = ToStringHelper.ToStringWithCulture( Naming.ToValidName($"FIN_{processes.First().InstanceName}") );
            if (processes.Length == 1)
                Write($"    FIN <= {first_fin};\n");
            else
            {
                var zipped = processes.Skip(1)
                        .Zip(
                            processes.SkipLast(1),
                            (a, b) => {
                                var namea = Naming.ToValidName($"FIN_{a.InstanceName}");
                                var nameb = Naming.ToValidName($"FIN_{b.InstanceName}");
                                return $"{namea} = {nameb}";
                            }
                        );
                    var allfin = string.Join(" AND ", zipped);
                Write($"    FIN <= {first_fin} when {allfin};\n");
            }


            Write(@"
    -- Propagate all clocked and feedback signals
    process(
        CLK,
        RST
    )
        variable readyflag: std_logic;
    begin
        if RST = '1' then
            RDY <= '0';
            readyflag := '0';
        elsif rising_edge(CLK) then
            if ENB = '1' then
                readyflag := not readyflag;
                RDY <= readyflag;
");

            if (feedbacks.Any())
            {
                Write("\n");
                Write("                -- Forward feedback signals\n");
                foreach (var signal in feedbacks.SelectMany(x => x.Signals))
                {
                    var signalname = ToStringHelper.ToStringWithCulture( Naming.ToValidName($"{(signal.Parent as AST.Bus).InstanceName}_{signal.Name}"));
                    Write($"                current_{signalname} <= next_{signalname};\n");
                }
            }

            var intermediates = processes
                .SelectMany(x => x.OutputBusses.Keys.Intersect(x.InputBusses.Keys))
                .Except(feedbacks)
                .Where(x => !x.IsTopLevelInput);
            foreach (var bus in intermediates)
            {
                var busname = ToStringHelper.ToStringWithCulture( bus.Name );
                Write("\n");
                Write($"                -- Bus {busname} intermediate signals\n");
                foreach(var signal in bus.Signals)
                {
                    var signalname = ToStringHelper.ToStringWithCulture( Naming.ToValidName($"{bus.InstanceName}_{signal.Name}") );
                    var nextsignalname = ToStringHelper.ToStringWithCulture( Naming.ToValidName($"next_{bus.InstanceName}_{signal.Name}") );
                    Write($"                {signalname} <= {nextsignalname};\n");
                }
            }

            Write(
@"            end if;
        end if;
    end process;

");


            if (feedbacks.Where(x => x.IsTopLevelOutput).Any())
            {
                Write("    -- Send feedback outputs to the actual output\n");
                foreach (var signal in feedbacks.Where(x => x.IsTopLevelOutput).SelectMany(x => x.Signals))
                {
                    var signalname = ToStringHelper.ToStringWithCulture( Naming.ToValidName($"{(signal.Parent as AST.Bus).InstanceName}_{signal.Name}") );
                    var nextsignalname = ToStringHelper.ToStringWithCulture( Naming.ToValidName($"next_{(signal.Parent as AST.Bus).InstanceName}_{signal.Name}") );
                    Write($"    {signalname} <= {nextsignalname};\n");
                }
                Write("\n");
            }

            var tmp_signals = tmps.SelectMany(x => x.Signals);
            if (tmp_signals.Any())
            {
                Write("    -- Propegate tmp signals\n");
                foreach (var signal in tmp_signals)
                {
                    var signalname = ToStringHelper.ToStringWithCulture( Naming.ToValidName($"{(signal.Parent as AST.Bus).InstanceName}_{signal.Name}") );
                    var tmpname = ToStringHelper.ToStringWithCulture( Naming.ToValidName($"tmp_{(signal.Parent as AST.Bus).InstanceName}_{signal.Name}") );
                    Write($"    {signalname} <= {tmpname};\n");
                }
                Write("\n");
            }

            Write(
@"    -- User defined processes here
    -- #### USER-DATA-CODE-START
    -- #### USER-DATA-CODE-END

end RTL;");

            return GenerationEnvironment.ToString();
        }
    }
}
