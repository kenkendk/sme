﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SME.VHDL.Templates {
    using System.Linq;
    using System.Text;
    using System.Collections.Generic;
    using SME.VHDL;
    using SME.AST;
    using System;
    
    
    public partial class TracefileTester : TracefileTesterBase {
        
        public virtual string TransformText() {
            this.GenerationEnvironment = null;
            
            #line 8 ""
            this.Write(@"
library IEEE;
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

entity ");
            
            #line default
            #line hidden
            
            #line 27 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( Network.Name ));
            
            #line default
            #line hidden
            
            #line 27 ""
            this.Write("_tb is\nend;\n\narchitecture TestBench of ");
            
            #line default
            #line hidden
            
            #line 30 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( Network.Name ));
            
            #line default
            #line hidden
            
            #line 30 ""
            this.Write("_tb is\n\n  signal CLOCK : Std_logic;\n  signal StopClock : BOOLEAN;\n  signal RESET " +
                    ": Std_logic;\n  signal ENABLE : Std_logic;\n\n");
            
            #line default
            #line hidden
            
            #line 37 ""
 foreach (var signal in RS.AllSignals) { 
            
            #line default
            #line hidden
            
            #line 38 ""
            this.Write("  signal ");
            
            #line default
            #line hidden
            
            #line 38 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( ((AST.Bus)signal.Parent).InstanceName));
            
            #line default
            #line hidden
            
            #line 38 ""
            this.Write("_");
            
            #line default
            #line hidden
            
            #line 38 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( signal.Name));
            
            #line default
            #line hidden
            
            #line 38 ""
            this.Write(" : ");
            
            #line default
            #line hidden
            
            #line 38 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( RS.VHDLWrappedTypeName(signal) ));
            
            #line default
            #line hidden
            
            #line 38 ""
            this.Write(";\n");
            
            #line default
            #line hidden
            
            #line 39 ""
 } 
            
            #line default
            #line hidden
            
            #line 40 ""
            this.Write("\nbegin\n\n  uut: entity work.");
            
            #line default
            #line hidden
            
            #line 43 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( Network.Name ));
            
            #line default
            #line hidden
            
            #line 43 ""
            this.Write("\n  port map (\n\n");
            
            #line default
            #line hidden
            
            #line 46 ""
foreach (var signal in RS.AllSignals) { 
            
            #line default
            #line hidden
            
            #line 47 ""
            this.Write("    ");
            
            #line default
            #line hidden
            
            #line 47 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( ((AST.Bus)signal.Parent).InstanceName));
            
            #line default
            #line hidden
            
            #line 47 ""
            this.Write("_");
            
            #line default
            #line hidden
            
            #line 47 ""
            this.Write(this.ToStringHelper.ToStringWithCulture(signal.Name));
            
            #line default
            #line hidden
            
            #line 47 ""
            this.Write(" => ");
            
            #line default
            #line hidden
            
            #line 47 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( ((AST.Bus)signal.Parent).InstanceName));
            
            #line default
            #line hidden
            
            #line 47 ""
            this.Write("_");
            
            #line default
            #line hidden
            
            #line 47 ""
            this.Write(this.ToStringHelper.ToStringWithCulture(signal.Name));
            
            #line default
            #line hidden
            
            #line 47 ""
            this.Write(",\n");
            
            #line default
            #line hidden
            
            #line 48 ""
 } 
            
            #line default
            #line hidden
            
            #line 49 ""
            this.Write("\n    ENB => ENABLE,\n    RST => RESET,\n    CLK => CLOCK\n  );\n\n  Clk: process\n  beg" +
                    "in\n    while not StopClock loop\n      CLOCK <= \'1\';\n      wait for ");
            
            #line default
            #line hidden
            
            #line 59 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( RS.ClockPulseLength ));
            
            #line default
            #line hidden
            
            #line 59 ""
            this.Write(" NS;\n      CLOCK <= \'0\';\n      wait for ");
            
            #line default
            #line hidden
            
            #line 61 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( RS.ClockPulseLength ));
            
            #line default
            #line hidden
            
            #line 61 ""
            this.Write(@" NS;
    end loop;
    wait;
  end process;


TraceFileTester: process
    file F: TEXT;
    variable L: LINE;
    variable Status: FILE_OPEN_STATUS;
    constant filename : string := ""./trace.csv"";
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
            
            #line default
            #line hidden
            
            #line 94 ""
 foreach (var signal in RS.DriverSignals.Concat(RS.VerifySignals)) { 
            
            #line default
            #line hidden
            
            #line 95 ""
            this.Write("        read_csv_field(L, tmp);\n        assert are_strings_equal(tmp, \"");
            
            #line default
            #line hidden
            
            #line 96 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( RS.TestBenchSignalName(signal) ));
            
            #line default
            #line hidden
            
            #line 96 ""
            this.Write("\") report \"Field #\" & integer\'image(fieldno) & \" is not correctly named: \" & trun" +
                    "cate(tmp) & \", expected ");
            
            #line default
            #line hidden
            
            #line 96 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( RS.TestBenchSignalName(signal) ));
            
            #line default
            #line hidden
            
            #line 96 ""
            this.Write("\" severity Failure;\n        fieldno := fieldno + 1;\n");
            
            #line default
            #line hidden
            
            #line 98 ""
 } 
            
            #line default
            #line hidden
            
            #line 99 ""
            this.Write("\n        RESET <= \'1\';\n        ENABLE <= \'0\';\n        wait for ");
            
            #line default
            #line hidden
            
            #line 102 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( RS.ClockPulseLength ));
            
            #line default
            #line hidden
            
            #line 102 ""
            this.Write(" NS;\n        RESET <= \'0\';\n        ENABLE <= \'1\';\n\n        -- Read a line each cl" +
                    "ock\n        while not ENDFILE(F) loop\n            READLINE(F, L);\n\n            f" +
                    "ieldno := 0;\n            newfailures := 0;\n\n");
            
            #line default
            #line hidden
            
            #line 113 ""
 if (RS.DriverSignals.Count() > 0) { 
            
            #line default
            #line hidden
            
            #line 114 ""
            this.Write(@"            -- Write all driver signals out on the clock edge,
            -- except on the first round, where we make sure the reset
            -- values are propagated _before_ the initial clock edge
            if not first_round then
                wait until rising_edge(CLOCK);
            end if;

");
            
            #line default
            #line hidden
            
            #line 121 ""
     foreach (var signal in RS.DriverSignals) { 
           var vhdltype = RS.VHDLType(signal);

            
            #line default
            #line hidden
            
            #line 124 ""
            this.Write("            read_csv_field(L, tmp);\n");
            
            #line default
            #line hidden
            
            #line 125 ""
        if (vhdltype.IsStdLogic || vhdltype == VHDLTypes.SYSTEM_BOOL) { 
            
            #line default
            #line hidden
            
            #line 126 ""
            this.Write("            if are_strings_equal(tmp, \"U\") then\n                ");
            
            #line default
            #line hidden
            
            #line 127 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( ((AST.Bus)signal.Parent).InstanceName));
            
            #line default
            #line hidden
            
            #line 127 ""
            this.Write("_");
            
            #line default
            #line hidden
            
            #line 127 ""
            this.Write(this.ToStringHelper.ToStringWithCulture(signal.Name));
            
            #line default
            #line hidden
            
            #line 127 ""
            this.Write(" <= \'U\';\n            else\n                ");
            
            #line default
            #line hidden
            
            #line 129 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( ((AST.Bus)signal.Parent).InstanceName));
            
            #line default
            #line hidden
            
            #line 129 ""
            this.Write("_");
            
            #line default
            #line hidden
            
            #line 129 ""
            this.Write(this.ToStringHelper.ToStringWithCulture(signal.Name));
            
            #line default
            #line hidden
            
            #line 129 ""
            this.Write(" <= to_std_logic(truncate(tmp));\n            end if;\n");
            
            #line default
            #line hidden
            
            #line 131 ""
        } else if (vhdltype.IsStdLogicVector || vhdltype.IsSystemType || vhdltype.IsVHDLSigned || vhdltype.IsVHDLUnsigned) { 
            
            #line default
            #line hidden
            
            #line 132 ""
            this.Write("            if are_strings_equal(tmp, \"U\") then\n                ");
            
            #line default
            #line hidden
            
            #line 133 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( ((AST.Bus)signal.Parent).InstanceName));
            
            #line default
            #line hidden
            
            #line 133 ""
            this.Write("_");
            
            #line default
            #line hidden
            
            #line 133 ""
            this.Write(this.ToStringHelper.ToStringWithCulture(signal.Name));
            
            #line default
            #line hidden
            
            #line 133 ""
            this.Write(" <= (others => \'U\');\n            else\n");
            
            #line default
            #line hidden
            
            #line 135 ""
            if ((vhdltype.IsSystemType || vhdltype.IsVHDLSigned) && vhdltype.IsSigned) { 
            
            #line default
            #line hidden
            
            #line 136 ""
            this.Write("                ");
            
            #line default
            #line hidden
            
            #line 136 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( ((AST.Bus)signal.Parent).InstanceName));
            
            #line default
            #line hidden
            
            #line 136 ""
            this.Write("_");
            
            #line default
            #line hidden
            
            #line 136 ""
            this.Write(this.ToStringHelper.ToStringWithCulture(signal.Name));
            
            #line default
            #line hidden
            
            #line 136 ""
            this.Write(" <= signed(to_std_logic_vector(truncate(tmp)));\n");
            
            #line default
            #line hidden
            
            #line 137 ""
            } else if ((vhdltype.IsSystemType || vhdltype.IsVHDLUnsigned) && vhdltype.IsUnsigned) { 
            
            #line default
            #line hidden
            
            #line 138 ""
            this.Write("                ");
            
            #line default
            #line hidden
            
            #line 138 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( ((AST.Bus)signal.Parent).InstanceName));
            
            #line default
            #line hidden
            
            #line 138 ""
            this.Write("_");
            
            #line default
            #line hidden
            
            #line 138 ""
            this.Write(this.ToStringHelper.ToStringWithCulture(signal.Name));
            
            #line default
            #line hidden
            
            #line 138 ""
            this.Write(" <= unsigned(to_std_logic_vector(truncate(tmp)));\n");
            
            #line default
            #line hidden
            
            #line 139 ""
            } else { 
            
            #line default
            #line hidden
            
            #line 140 ""
            this.Write("                ");
            
            #line default
            #line hidden
            
            #line 140 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( ((AST.Bus)signal.Parent).InstanceName));
            
            #line default
            #line hidden
            
            #line 140 ""
            this.Write("_");
            
            #line default
            #line hidden
            
            #line 140 ""
            this.Write(this.ToStringHelper.ToStringWithCulture(signal.Name));
            
            #line default
            #line hidden
            
            #line 140 ""
            this.Write(" <= to_std_logic_vector(truncate(tmp));\n");
            
            #line default
            #line hidden
            
            #line 141 ""
            } 
            
            #line default
            #line hidden
            
            #line 142 ""
            this.Write("            end if;\n");
            
            #line default
            #line hidden
            
            #line 143 ""
        } else { 
            
            #line default
            #line hidden
            
            #line 144 ""
            this.Write("            ");
            
            #line default
            #line hidden
            
            #line 144 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( ((AST.Bus)signal.Parent).InstanceName));
            
            #line default
            #line hidden
            
            #line 144 ""
            this.Write("_");
            
            #line default
            #line hidden
            
            #line 144 ""
            this.Write(this.ToStringHelper.ToStringWithCulture(signal.Name));
            
            #line default
            #line hidden
            
            #line 144 ""
            this.Write(" <= ");
            
            #line default
            #line hidden
            
            #line 144 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( vhdltype.ToSafeVHDLName()));
            
            #line default
            #line hidden
            
            #line 144 ""
            this.Write("\'value(to_safe_name(truncate(tmp)));\n");
            
            #line default
            #line hidden
            
            #line 145 ""
        } 
            
            #line default
            #line hidden
            
            #line 146 ""
            this.Write("            fieldno := fieldno + 1;\n");
            
            #line default
            #line hidden
            
            #line 147 ""
     } 
            
            #line default
            #line hidden
            
            #line 148 ""
 } 
            
            #line default
            #line hidden
            
            #line 149 ""
            this.Write(@"
            if first_round then
                wait until rising_edge(CLOCK);
                first_round := false;
            end if;

            -- Wait until the signals are settled before veriying the results
            wait until falling_edge(CLOCK);

            -- Compare each signal with the value in the CSV file
");
            
            #line default
            #line hidden
            
            #line 159 ""
 foreach (var signal in RS.VerifySignals) { 
       var vhdltype = RS.VHDLType(signal);

            
            #line default
            #line hidden
            
            #line 162 ""
            this.Write("\t        read_csv_field(L, tmp);\n\t        if not are_strings_equal(tmp, \"U\") then" +
                    "\n");
            
            #line default
            #line hidden
            
            #line 164 ""
    if (vhdltype.IsStdLogicVector || vhdltype.IsSystemType || vhdltype.IsVHDLSigned || vhdltype.IsVHDLUnsigned) { 
            
            #line default
            #line hidden
            
            #line 165 ""
            this.Write("            \tif not are_strings_equal(str(");
            
            #line default
            #line hidden
            
            #line 165 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( ((AST.Bus)signal.Parent).InstanceName));
            
            #line default
            #line hidden
            
            #line 165 ""
            this.Write("_");
            
            #line default
            #line hidden
            
            #line 165 ""
            this.Write(this.ToStringHelper.ToStringWithCulture(signal.Name));
            
            #line default
            #line hidden
            
            #line 165 ""
            this.Write("), tmp) then\n                    newfailures := newfailures + 1;\n                " +
                    "    report \"Value for ");
            
            #line default
            #line hidden
            
            #line 167 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( ((AST.Bus)signal.Parent).InstanceName));
            
            #line default
            #line hidden
            
            #line 167 ""
            this.Write("_");
            
            #line default
            #line hidden
            
            #line 167 ""
            this.Write(this.ToStringHelper.ToStringWithCulture(signal.Name));
            
            #line default
            #line hidden
            
            #line 167 ""
            this.Write(" in cycle \" & integer\'image(clockcycle) & \" was: \" & str(");
            
            #line default
            #line hidden
            
            #line 167 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( ((AST.Bus)signal.Parent).InstanceName));
            
            #line default
            #line hidden
            
            #line 167 ""
            this.Write("_");
            
            #line default
            #line hidden
            
            #line 167 ""
            this.Write(this.ToStringHelper.ToStringWithCulture(signal.Name));
            
            #line default
            #line hidden
            
            #line 167 ""
            this.Write(") & \" but should have been: \" & truncate(tmp) severity Error;\n                end" +
                    " if;\n");
            
            #line default
            #line hidden
            
            #line 169 ""
    } else { 
            
            #line default
            #line hidden
            
            #line 170 ""
            this.Write("            \tif not are_strings_equal(");
            
            #line default
            #line hidden
            
            #line 170 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( vhdltype.ToSafeVHDLName()));
            
            #line default
            #line hidden
            
            #line 170 ""
            this.Write("\'image(");
            
            #line default
            #line hidden
            
            #line 170 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( ((AST.Bus)signal.Parent).InstanceName));
            
            #line default
            #line hidden
            
            #line 170 ""
            this.Write("_");
            
            #line default
            #line hidden
            
            #line 170 ""
            this.Write(this.ToStringHelper.ToStringWithCulture(signal.Name));
            
            #line default
            #line hidden
            
            #line 170 ""
            this.Write("), to_safe_name(tmp)) then\n                    newfailures := newfailures + 1;\n  " +
                    "                  report \"Value for ");
            
            #line default
            #line hidden
            
            #line 172 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( ((AST.Bus)signal.Parent).InstanceName));
            
            #line default
            #line hidden
            
            #line 172 ""
            this.Write("_");
            
            #line default
            #line hidden
            
            #line 172 ""
            this.Write(this.ToStringHelper.ToStringWithCulture(signal.Name));
            
            #line default
            #line hidden
            
            #line 172 ""
            this.Write(" in cycle \" & integer\'image(clockcycle) & \" was: \" & ");
            
            #line default
            #line hidden
            
            #line 172 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( vhdltype.ToSafeVHDLName()));
            
            #line default
            #line hidden
            
            #line 172 ""
            this.Write("\'image(");
            
            #line default
            #line hidden
            
            #line 172 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( ((AST.Bus)signal.Parent).InstanceName));
            
            #line default
            #line hidden
            
            #line 172 ""
            this.Write("_");
            
            #line default
            #line hidden
            
            #line 172 ""
            this.Write(this.ToStringHelper.ToStringWithCulture(signal.Name));
            
            #line default
            #line hidden
            
            #line 172 ""
            this.Write(") & \" but should have been: \" & to_safe_name(truncate(tmp)) severity Error;\n     " +
                    "            end if;\n");
            
            #line default
            #line hidden
            
            #line 174 ""
    } 
            
            #line default
            #line hidden
            
            #line 175 ""
            this.Write("            end if;\n            fieldno := fieldno + 1;\n\n");
            
            #line default
            #line hidden
            
            #line 178 ""
 } 
            
            #line default
            #line hidden
            
            #line 179 ""
            this.Write(@"            failures := failures + newfailures;
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
            
            #line default
            #line hidden
            return this.GenerationEnvironment.ToString();
        }
        
        public virtual void Initialize() {
        }
    }
    
    public class TracefileTesterBase {
        
        private global::System.Text.StringBuilder builder;
        
        private global::System.Collections.Generic.IDictionary<string, object> session;
        
        private global::System.CodeDom.Compiler.CompilerErrorCollection errors;
        
        private string currentIndent = string.Empty;
        
        private global::System.Collections.Generic.Stack<int> indents;
        
        private ToStringInstanceHelper _toStringHelper = new ToStringInstanceHelper();
        
        public virtual global::System.Collections.Generic.IDictionary<string, object> Session {
            get {
                return this.session;
            }
            set {
                this.session = value;
            }
        }
        
        public global::System.Text.StringBuilder GenerationEnvironment {
            get {
                if ((this.builder == null)) {
                    this.builder = new global::System.Text.StringBuilder();
                }
                return this.builder;
            }
            set {
                this.builder = value;
            }
        }
        
        protected global::System.CodeDom.Compiler.CompilerErrorCollection Errors {
            get {
                if ((this.errors == null)) {
                    this.errors = new global::System.CodeDom.Compiler.CompilerErrorCollection();
                }
                return this.errors;
            }
        }
        
        public string CurrentIndent {
            get {
                return this.currentIndent;
            }
        }
        
        private global::System.Collections.Generic.Stack<int> Indents {
            get {
                if ((this.indents == null)) {
                    this.indents = new global::System.Collections.Generic.Stack<int>();
                }
                return this.indents;
            }
        }
        
        public ToStringInstanceHelper ToStringHelper {
            get {
                return this._toStringHelper;
            }
        }
        
        public void Error(string message) {
            this.Errors.Add(new global::System.CodeDom.Compiler.CompilerError(null, -1, -1, null, message));
        }
        
        public void Warning(string message) {
            global::System.CodeDom.Compiler.CompilerError val = new global::System.CodeDom.Compiler.CompilerError(null, -1, -1, null, message);
            val.IsWarning = true;
            this.Errors.Add(val);
        }
        
        public string PopIndent() {
            if ((this.Indents.Count == 0)) {
                return string.Empty;
            }
            int lastPos = (this.currentIndent.Length - this.Indents.Pop());
            string last = this.currentIndent.Substring(lastPos);
            this.currentIndent = this.currentIndent.Substring(0, lastPos);
            return last;
        }
        
        public void PushIndent(string indent) {
            this.Indents.Push(indent.Length);
            this.currentIndent = (this.currentIndent + indent);
        }
        
        public void ClearIndent() {
            this.currentIndent = string.Empty;
            this.Indents.Clear();
        }
        
        public void Write(string textToAppend) {
            this.GenerationEnvironment.Append(textToAppend);
        }
        
        public void Write(string format, params object[] args) {
            this.GenerationEnvironment.AppendFormat(format, args);
        }
        
        public void WriteLine(string textToAppend) {
            this.GenerationEnvironment.Append(this.currentIndent);
            this.GenerationEnvironment.AppendLine(textToAppend);
        }
        
        public void WriteLine(string format, params object[] args) {
            this.GenerationEnvironment.Append(this.currentIndent);
            this.GenerationEnvironment.AppendFormat(format, args);
            this.GenerationEnvironment.AppendLine();
        }
        
        public class ToStringInstanceHelper {
            
            private global::System.IFormatProvider formatProvider = global::System.Globalization.CultureInfo.InvariantCulture;
            
            public global::System.IFormatProvider FormatProvider {
                get {
                    return this.formatProvider;
                }
                set {
                    if ((value != null)) {
                        this.formatProvider = value;
                    }
                }
            }
            
            public string ToStringWithCulture(object objectToConvert) {
                if ((objectToConvert == null)) {
                    throw new global::System.ArgumentNullException("objectToConvert");
                }
                global::System.Type type = objectToConvert.GetType();
                global::System.Type iConvertibleType = typeof(global::System.IConvertible);
                if (iConvertibleType.IsAssignableFrom(type)) {
                    return ((global::System.IConvertible)(objectToConvert)).ToString(this.formatProvider);
                }
                global::System.Reflection.MethodInfo methInfo = type.GetMethod("ToString", new global::System.Type[] {
                            iConvertibleType});
                if ((methInfo != null)) {
                    return ((string)(methInfo.Invoke(objectToConvert, new object[] {
                                this.formatProvider})));
                }
                return objectToConvert.ToString();
            }
        }
    }
}
