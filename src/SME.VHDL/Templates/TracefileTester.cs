using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SME.AST;

namespace SME.VHDL.Templates
{
    /// <summary>
    /// Template for generating the VHDL testbench.
    /// </summary>
    public class TracefileTester : BaseTemplate
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
        /// Constructs a new instance of the tracefile tester template.
        /// </summary>
        /// <param name="renderer">The render state to render in.</param>
        public TracefileTester(RenderState renderer)
        {
            RS = renderer;
            Network = renderer.Network;
        }

        /// <summary>
        /// Writes the template to the VHDL file.
        /// </summary>
        public override string TransformText() {
            GenerationEnvironment = null;

            Write(
@"library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;
use STD.TEXTIO.all;
use IEEE.STD_LOGIC_TEXTIO.all;

--library SYSTEM_TYPES;
use work.SYSTEM_TYPES.ALL;

--library CUSTOM_TYPES;
use work.CUSTOM_TYPES.ALL;

use work.csv_util.all;

-- User defined packages here
-- #### USER-DATA-IMPORTS-START
-- #### USER-DATA-IMPORTS-END

");
            var networkname = ToStringHelper.ToStringWithCulture( Network.Name );

            Write($"entity {networkname}_tb is\n");
            Write("end;\n");
            Write("\n");
            Write($"architecture TestBench of {networkname}_tb is\n");
            Write(@"
    signal CLOCK : Std_logic;
    signal StopClock : BOOLEAN;
    signal RESET : Std_logic;
    signal ENABLE : Std_logic;

");

            foreach (var bus in Network.Busses.OrderBy(x => x.InstanceName))
            {
                var multiplier = bus.SourceInstances.Length;
                foreach (var signal in bus.Signals.OrderBy(x => x.Name))
                {
                    var busname = ToStringHelper.ToStringWithCulture( bus.InstanceName);
                    var signalname = ToStringHelper.ToStringWithCulture( signal.Name);
                    var vhdltype = ToStringHelper.ToStringWithCulture( RS.VHDLWrappedTypeName(signal) );
                    var quantifier = multiplier > 1 ? $"_ARRAY({multiplier - 1} downto 0)" : "";
                    Write($"    signal {busname}_{signalname} : {vhdltype}{quantifier};\n");
                }
            }

            Write(@"
begin

");
            Write($"    uut: entity work.{networkname}\n");
            Write($"    port map (\n");

            foreach (var signal in RS.AllSignals)
            {
                var instancename = ToStringHelper.ToStringWithCulture( ((AST.Bus)signal.Parent).InstanceName);
                var signalname = ToStringHelper.ToStringWithCulture(signal.Name);
                Write($"        {instancename}_{signalname} => {instancename}_{signalname},\n");
            }

            Write(@"
        ENB => ENABLE,
        RST => RESET,
        CLK => CLOCK
    );

    Clk: process
    begin
        while not StopClock loop
            CLOCK <= '1';
");
            var pulse = ToStringHelper.ToStringWithCulture( RS.ClockPulseLength );
            Write($"            wait for {pulse} NS;\n");
            Write($"            CLOCK <= '0';\n");
            Write($"            wait for {pulse} NS;\n");
            Write(
@"        end loop;
        wait;
    end process;

    TraceFileTester: process

        file F: TEXT;
        variable L: LINE;
        variable Status: FILE_OPEN_STATUS;
        constant filename : string := ""../trace.csv"";
        variable clockcycle : integer := 0;
        variable tmp : CSV_LINE_T;
        variable readOK : boolean;
        variable fieldno : integer := 0;
        variable failures : integer := 0;
        variable newfailures: integer := 0;
        variable first_failure_tick : integer := -1;
        variable first_round : boolean := true;

    begin

        -- #### USER-DATA-CONDITONING-START
        -- #### USER-DATA-CONDITONING-END

        FILE_OPEN(Status, F, filename, READ_MODE);
        if Status /= OPEN_OK then
            report ""Failed to open CSV trace file"" severity Failure;
        else
            -- Verify the headers
            READLINE(F, L);

            fieldno := 0;
");

            var signalnames = ToStringHelper.ToEnumeratedString(RS, RS.DriverBusses.Concat(RS.VerifyBusses));

            foreach (var signalname in signalnames)
            {
                Write($"            read_csv_field(L, tmp);\n");
                Write($"            assert are_strings_equal(tmp, \"{signalname}\") report \"Field #\" & integer\'image(fieldno) & \" is not correctly named: \" & truncate(tmp) & \", expected {signalname}\" severity Failure;\n");
                Write($"            fieldno := fieldno + 1;\n");
            }

            Write(@"
            RESET <= '1';
            ENABLE <= '0';
");
            Write($"            wait for {pulse} NS;\n");

            Write(
@"            RESET <= '0';
            ENABLE <= '1';

            -- Read a line each clock
            while not ENDFILE(F) loop
                READLINE(F, L);
                fieldno := 0;
                newfailures := 0;

                -- Write all driver signals out on the clock edge,
                -- except on the first round, where we make sure the reset
                -- values are propagated _before_ the initial clock edge
                if not first_round then
                    wait until rising_edge(CLOCK);
                end if;

");

            foreach (var bus in RS.DriverBusses.OrderBy(x => x.InstanceName))
            {
                var idxs = ToStringHelper.ToEnumeratedIndices(RS, bus);
                foreach (var idx in idxs)
                {
                    foreach (var signal in bus.Signals.OrderBy(x => ToStringHelper.ToStringWithCulture(x.Name)).SelectMany(x => RS.SplitArray(x)))
                    {
                        // TODO will this work for buses with a single instance? Probably not.
                        var indexstr = idxs.Count() > 1 ? $"({idx})" : "";
                        var vhdltype = RS.VHDLType(signal);
                        var busname = ToStringHelper.ToStringWithCulture( ((AST.Bus)signal.Parent).InstanceName);
                        var signalname = ToStringHelper.ToStringWithCulture(signal.Name);
                        signalname += indexstr;
                        var vhdltypename = ToStringHelper.ToStringWithCulture( vhdltype.ToSafeVHDLName());
                        Write("                read_csv_field(L, tmp);\n");
                        if (vhdltype.IsStdLogic || vhdltype == VHDLTypes.SYSTEM_BOOL)
                        {
                            Write($"                if are_strings_equal(tmp, \"U\") then\n");
                            Write($"                    {busname}_{signalname} <= 'U';\n");
                            Write($"                else\n");
                            Write($"                    {busname}_{signalname} <= to_std_logic(truncate(tmp));\n");
                            Write($"                end if;\n");
                        }
                        else if (vhdltype.IsStdLogicVector || vhdltype.IsSystemType || vhdltype.IsVHDLSigned || vhdltype.IsVHDLUnsigned)
                        {
                            Write($"                if are_strings_equal(tmp, \"U\") then\n");
                            Write($"                    {busname}_{signalname} <= (others => 'U');\n");
                            Write($"                else\n");
                            if ((vhdltype.IsSystemType || vhdltype.IsVHDLSigned) && vhdltype.IsSigned)
                                Write($"                    {busname}_{signalname} <= signed(to_std_logic_vector(truncate(tmp)));\n");
                            else if ((vhdltype.IsSystemType || vhdltype.IsVHDLUnsigned) && vhdltype.IsUnsigned)
                                Write($"                    {busname}_{signalname} <= unsigned(to_std_logic_vector(truncate(tmp)));\n");
                            else
                                Write($"                    {busname}_{signalname} <= to_std_logic_vector(truncate(tmp));\n");
                            Write($"                end if;\n");
                        }
                        else
                            Write($"            {busname}_{signalname} <= {vhdltypename}'value(to_safe_name(truncate(tmp)));\n");
                        Write($"                fieldno := fieldno + 1;\n");
                    }
                }
            }

            Write(@"
                if first_round then
                    first_round := false;
                else
                    -- Wait until the signals are settled before veriying the results
                    wait until falling_edge(CLOCK);
                end if;

                -- Compare each signal with the value in the CSV file
");

            foreach (var bus in RS.VerifyBusses)
            {
                var idxs = ToStringHelper.ToEnumeratedIndices(RS, bus);
                foreach (var idx in idxs)
                {
                    foreach (var signal in bus.Signals.OrderBy(x => ToStringHelper.ToStringWithCulture(x.Name)).SelectMany(x => RS.SplitArray(x)))
                    {
                        var indexstr = idxs.Count() > 1 ? $"({idx})" : "";
                        var vhdltype = RS.VHDLType(signal);
                        var busname = ToStringHelper.ToStringWithCulture( ((AST.Bus)signal.Parent).InstanceName);
                        var signalname = ToStringHelper.ToStringWithCulture(signal.Name);
                        signalname += indexstr;
                        var vhdltypename = ToStringHelper.ToStringWithCulture( vhdltype.ToSafeVHDLName());
                        Write($"                read_csv_field(L, tmp);\n");
                        Write($"                if not are_strings_equal(tmp, \"U\") then\n");
                        if (vhdltype.IsStdLogicVector || vhdltype.IsSystemType || vhdltype.IsVHDLSigned || vhdltype.IsVHDLUnsigned)
                        {
                            Write($"                    if not are_strings_equal(str({busname}_{signalname}), tmp) then\n");
                            Write($"                        newfailures := newfailures + 1;\n");
                            Write($"                        report \"Value for {busname}_{signalname} in cycle \" & integer'image(clockcycle) & \" was: \" & str({busname}_{signalname}) & \" but should have been: \" & truncate(tmp) severity Error;\n");
                            Write($"                    end if;\n");
                        }
                        else
                        {
                            Write($"                    if not are_strings_equal({vhdltypename}'image({busname}_{signalname}), to_safe_name(tmp)) then\n");
                            Write($"                        newfailures := newfailures + 1;\n");
                            Write($"                        report \"Value for {busname}_{signalname} in cycle \" & integer'image(clockcycle) & \" was: \" & {vhdltypename}'image({busname}_{signalname}) & \" but should have been: \" & to_safe_name(truncate(tmp)) severity Error;\n");
                            Write($"                    end if;\n");
                        }
                        Write("                end if;\n");
                        Write("                fieldno := fieldno + 1;\n");
                    }
                }
            }

            Write(@"
                failures := failures + newfailures;
                if newfailures = 0 then
                    first_failure_tick := -1;
                elsif first_failure_tick = -1 then
                    first_failure_tick := clockcycle;
                else
                    if clockcycle - first_failure_tick >= 5 then
                        report ""Stopping simulation due to five consecutive failed cycles"" severity error;
                        StopClock <= true;
                    elsif failures > 20 then
                        report ""Stopping simulation after 20 failures"" severity error;
                        StopClock <= true;
                    end if;
                end if;

                clockcycle := clockcycle + 1;
            end loop;

            FILE_CLOSE(F);
        end if;

        if failures = 0 then
            report ""completed successfully after "" & integer'image(clockcycle) & "" clockcycles"";
        else
            report ""completed with "" & integer'image(failures) & "" error(s) after "" & integer'image(clockcycle) & "" clockcycle(s)"";
        end if;
        StopClock <= true;

        wait;
    end process;
end architecture TestBench;");

            return GenerationEnvironment.ToString();
        }
    }
}
