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
    using SME;
    using SME.VHDL;
    using System.Text;
    using System.Collections.Generic;
    using SME.AST;
    using System;
    
    
    public partial class ExportTopLevel : ExportTopLevelBase {
        
        public virtual string TransformText() {
            this.GenerationEnvironment = null;
            
            #line 9 ""
            this.Write(@"library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

-- library SYSTEM_TYPES;
use work.SYSTEM_TYPES.ALL;

-- library CUSTOM_TYPES;
use work.CUSTOM_TYPES.ALL;

-- User defined packages here
-- #### USER-DATA-IMPORTS-START
-- #### USER-DATA-IMPORTS-END

entity ");
            
            #line default
            #line hidden
            
            #line 23 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( Network.Name ));
            
            #line default
            #line hidden
            
            #line 23 ""
            this.Write("_export is\n  port(\n\n");
            
            #line default
            #line hidden
            
            #line 26 ""
 foreach (var bus in Network.Busses.Where(x => x.IsTopLevelInput || x.IsTopLevelOutput)) {
	var signaltype = "inout"; 

	if (bus.IsTopLevelInput && !bus.IsTopLevelOutput)
		signaltype = "in";
	else if (bus.IsTopLevelOutput && !bus.IsTopLevelInput)
		signaltype = "out";
	
            
            #line default
            #line hidden
            
            #line 34 ""
            this.Write("    -- Top-level bus ");
            
            #line default
            #line hidden
            
            #line 34 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( bus.Name ));
            
            #line default
            #line hidden
            
            #line 34 ""
            this.Write(" signals\n");
            
            #line default
            #line hidden
            
            #line 35 ""
     foreach (var signal in bus.Signals) { 
            
            #line default
            #line hidden
            
            #line 36 ""
            this.Write("    ");
            
            #line default
            #line hidden
            
            #line 36 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( bus.InstanceName ));
            
            #line default
            #line hidden
            
            #line 36 ""
            this.Write("_");
            
            #line default
            #line hidden
            
            #line 36 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( signal.Name ));
            
            #line default
            #line hidden
            
            #line 36 ""
            this.Write(": ");
            
            #line default
            #line hidden
            
            #line 36 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( signaltype ));
            
            #line default
            #line hidden
            
            #line 36 ""
            this.Write(" ");
            
            #line default
            #line hidden
            
            #line 36 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( RS.VHDLExportTypeName(signal) ));
            
            #line default
            #line hidden
            
            #line 36 ""
            this.Write(";\n");
            
            #line default
            #line hidden
            
            #line 37 ""
     } 
            
            #line default
            #line hidden
            
            #line 38 ""
            this.Write("\n");
            
            #line default
            #line hidden
            
            #line 39 ""
 } 
            
            #line default
            #line hidden
            
            #line 40 ""
            this.Write(@"
    -- User defined signals here
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
end ");
            
            #line default
            #line hidden
            
            #line 58 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( Network.Name ));
            
            #line default
            #line hidden
            
            #line 58 ""
            this.Write("_export;\n\n\n");
            
            #line default
            #line hidden
            
            #line 61 ""

var converted_outputs = new HashSet<AST.Signal>();

foreach (var bus in Network.Busses.Where(x => x.IsTopLevelOutput && !x.IsTopLevelInput)) 
{ 
    foreach(var signal in bus.Signals) 
    {
        var vt = RS.VHDLType(signal);
        if (vt.IsSigned || vt.IsUnsigned) 
        {
            converted_outputs.Add(signal);
        }
    }
}

            
            #line default
            #line hidden
            
            #line 76 ""
            this.Write("\narchitecture RTL of ");
            
            #line default
            #line hidden
            
            #line 77 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( Network.Name ));
            
            #line default
            #line hidden
            
            #line 77 ""
            this.Write("_export is  \n  -- User defined signals here\n  -- #### USER-DATA-SIGNALS-START\n  -" +
                    "- #### USER-DATA-SIGNALS-END\n\n");
            
            #line default
            #line hidden
            
            #line 82 ""
 if (converted_outputs.Count > 0) { 
            
            #line default
            #line hidden
            
            #line 83 ""
            this.Write("  -- Intermediate conversion signal to convert internal types to external ones\n");
            
            #line default
            #line hidden
            
            #line 84 ""
     foreach(var signal in converted_outputs) { 
            
            #line default
            #line hidden
            
            #line 85 ""
            this.Write("  signal tmp_");
            
            #line default
            #line hidden
            
            #line 85 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( ((AST.Bus)signal.Parent).InstanceName ));
            
            #line default
            #line hidden
            
            #line 85 ""
            this.Write("_");
            
            #line default
            #line hidden
            
            #line 85 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( signal.Name ));
            
            #line default
            #line hidden
            
            #line 85 ""
            this.Write(" : ");
            
            #line default
            #line hidden
            
            #line 85 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( RS.VHDLWrappedTypeName(signal) ));
            
            #line default
            #line hidden
            
            #line 85 ""
            this.Write(";\n");
            
            #line default
            #line hidden
            
            #line 86 ""
     } 
            
            #line default
            #line hidden
            
            #line 87 ""
 } 
            
            #line default
            #line hidden
            
            #line 88 ""
            this.Write("\nbegin\n");
            
            #line default
            #line hidden
            
            #line 90 ""
 if (converted_outputs.Count > 0) { 
            
            #line default
            #line hidden
            
            #line 91 ""
            this.Write("\n    -- Carry converted signals from entity to wrapped outputs\n");
            
            #line default
            #line hidden
            
            #line 93 ""
     foreach(var signal in converted_outputs) { 
            
            #line default
            #line hidden
            
            #line 94 ""
            this.Write("  ");
            
            #line default
            #line hidden
            
            #line 94 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( ((AST.Bus)signal.Parent).InstanceName ));
            
            #line default
            #line hidden
            
            #line 94 ""
            this.Write("_");
            
            #line default
            #line hidden
            
            #line 94 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( signal.Name ));
            
            #line default
            #line hidden
            
            #line 94 ""
            this.Write(" <= std_logic_vector(tmp_");
            
            #line default
            #line hidden
            
            #line 94 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( ((AST.Bus)signal.Parent).InstanceName ));
            
            #line default
            #line hidden
            
            #line 94 ""
            this.Write("_");
            
            #line default
            #line hidden
            
            #line 94 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( signal.Name ));
            
            #line default
            #line hidden
            
            #line 94 ""
            this.Write(");\n");
            
            #line default
            #line hidden
            
            #line 95 ""
     } 
            
            #line default
            #line hidden
            
            #line 96 ""
 } 
            
            #line default
            #line hidden
            
            #line 97 ""
            this.Write("\n    -- Entity ");
            
            #line default
            #line hidden
            
            #line 98 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( Network.Name ));
            
            #line default
            #line hidden
            
            #line 98 ""
            this.Write(" signals\n    ");
            
            #line default
            #line hidden
            
            #line 99 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( Network.Name ));
            
            #line default
            #line hidden
            
            #line 99 ""
            this.Write(": entity work.");
            
            #line default
            #line hidden
            
            #line 99 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( Network.Name ));
            
            #line default
            #line hidden
            
            #line 99 ""
            this.Write("\n    port map (\n");
            
            #line default
            #line hidden
            
            #line 101 ""
    foreach (var bus in Network.Busses.Where(x => x.IsTopLevelInput || x.IsTopLevelOutput)) { 
	      var type = "Input/Output"; 

	      if (bus.IsTopLevelInput && !bus.IsTopLevelOutput)
		      type = "Input";
	      else if (bus.IsTopLevelOutput && !bus.IsTopLevelInput)
		      type = "Output";
	
            
            #line default
            #line hidden
            
            #line 109 ""
            this.Write("        -- ");
            
            #line default
            #line hidden
            
            #line 109 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( type ));
            
            #line default
            #line hidden
            
            #line 109 ""
            this.Write(" bus ");
            
            #line default
            #line hidden
            
            #line 109 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( bus.Name ));
            
            #line default
            #line hidden
            
            #line 109 ""
            this.Write("\n");
            
            #line default
            #line hidden
            
            #line 110 ""
		  foreach(var signal in bus.Signals) {
              var vt = RS.VHDLType(signal);

            
            #line default
            #line hidden
            
            #line 113 ""
            if (converted_outputs.Contains(signal)) { 
            
            #line default
            #line hidden
            
            #line 114 ""
            this.Write("        ");
            
            #line default
            #line hidden
            
            #line 114 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( bus.InstanceName ));
            
            #line default
            #line hidden
            
            #line 114 ""
            this.Write("_");
            
            #line default
            #line hidden
            
            #line 114 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( signal.Name ));
            
            #line default
            #line hidden
            
            #line 114 ""
            this.Write(" => tmp_");
            
            #line default
            #line hidden
            
            #line 114 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( bus.InstanceName ));
            
            #line default
            #line hidden
            
            #line 114 ""
            this.Write("_");
            
            #line default
            #line hidden
            
            #line 114 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( signal.Name ));
            
            #line default
            #line hidden
            
            #line 114 ""
            this.Write(",\n");
            
            #line default
            #line hidden
            
            #line 115 ""
            } else if (vt.IsUnsigned) { 
            
            #line default
            #line hidden
            
            #line 116 ""
            this.Write("        ");
            
            #line default
            #line hidden
            
            #line 116 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( bus.InstanceName ));
            
            #line default
            #line hidden
            
            #line 116 ""
            this.Write("_");
            
            #line default
            #line hidden
            
            #line 116 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( signal.Name ));
            
            #line default
            #line hidden
            
            #line 116 ""
            this.Write(" => unsigned(");
            
            #line default
            #line hidden
            
            #line 116 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( bus.InstanceName ));
            
            #line default
            #line hidden
            
            #line 116 ""
            this.Write("_");
            
            #line default
            #line hidden
            
            #line 116 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( signal.Name ));
            
            #line default
            #line hidden
            
            #line 116 ""
            this.Write("),\n");
            
            #line default
            #line hidden
            
            #line 117 ""
            } else if (vt.IsSigned) { 
            
            #line default
            #line hidden
            
            #line 118 ""
            this.Write("        ");
            
            #line default
            #line hidden
            
            #line 118 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( bus.InstanceName ));
            
            #line default
            #line hidden
            
            #line 118 ""
            this.Write("_");
            
            #line default
            #line hidden
            
            #line 118 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( signal.Name ));
            
            #line default
            #line hidden
            
            #line 118 ""
            this.Write(" => signed(");
            
            #line default
            #line hidden
            
            #line 118 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( bus.InstanceName ));
            
            #line default
            #line hidden
            
            #line 118 ""
            this.Write("_");
            
            #line default
            #line hidden
            
            #line 118 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( signal.Name ));
            
            #line default
            #line hidden
            
            #line 118 ""
            this.Write("),\n");
            
            #line default
            #line hidden
            
            #line 119 ""
            } else { 
            
            #line default
            #line hidden
            
            #line 120 ""
            this.Write("        ");
            
            #line default
            #line hidden
            
            #line 120 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( bus.InstanceName ));
            
            #line default
            #line hidden
            
            #line 120 ""
            this.Write("_");
            
            #line default
            #line hidden
            
            #line 120 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( signal.Name ));
            
            #line default
            #line hidden
            
            #line 120 ""
            this.Write(" => ");
            
            #line default
            #line hidden
            
            #line 120 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( bus.InstanceName ));
            
            #line default
            #line hidden
            
            #line 120 ""
            this.Write("_");
            
            #line default
            #line hidden
            
            #line 120 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( signal.Name ));
            
            #line default
            #line hidden
            
            #line 120 ""
            this.Write(",\n");
            
            #line default
            #line hidden
            
            #line 121 ""
            } 
            
            #line default
            #line hidden
            
            #line 122 ""
        } 
            
            #line default
            #line hidden
            
            #line 123 ""
            this.Write("\n");
            
            #line default
            #line hidden
            
            #line 124 ""
    } 
            
            #line default
            #line hidden
            
            #line 125 ""
            this.Write("        ENB => ENB,\n        RST => RST,\n        FIN => FIN,\n        CLK => CLK\n  " +
                    "  );\n\n-- User defined processes here\n-- #### USER-DATA-CODE-START\n-- #### USER-D" +
                    "ATA-CODE-END\n\nend RTL;");
            
            #line default
            #line hidden
            return this.GenerationEnvironment.ToString();
        }
        
        public virtual void Initialize() {
        }
    }
    
    public class ExportTopLevelBase {
        
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
