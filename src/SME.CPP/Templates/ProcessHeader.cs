﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SME.CPP.Templates {
    using System.Linq;
    using System.Text;
    using System.Collections.Generic;
    using SME.AST;
    using System;


    public partial class ProcessHeader : ProcessHeaderBase {

        public virtual string TransformText() {
            this.GenerationEnvironment = null;

            #line 1 ""
            this.Write("﻿");

            #line default
            #line hidden

            #line 7 ""
            this.Write("#ifndef SME_");

            #line default
            #line hidden

            #line 7 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( RSP.Process.Name.ToUpper() ));

            #line default
            #line hidden

            #line 7 ""
            this.Write("_HPP\n#define SME_");

            #line default
            #line hidden

            #line 8 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( RSP.Process.Name.ToUpper() ));

            #line default
            #line hidden

            #line 8 ""
            this.Write("_HPP\n\n#include \"SystemTypes.hpp\"\n#include \"");

            #line default
            #line hidden

            #line 11 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( Naming.SharedDefinitionsFileName(Network) ));

            #line default
            #line hidden

            #line 11 ""
            this.Write("\"\n#include \"");

            #line default
            #line hidden

            #line 12 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( Naming.BusDefinitionsFileName(Network) ));

            #line default
            #line hidden

            #line 12 ""
            this.Write("\"\n\n");

            #line default
            #line hidden

            #line 14 ""


var busses = RSP.Process.InputBusses.Concat(RSP.Process.OutputBusses).Concat(RSP.Process.InternalBusses).Distinct().OrderBy(x => x.Name).ToArray();
var members = RSP.Process.SharedVariables.Cast<DataElement>().Union(RSP.Process.SharedSignals).ToArray();


            #line default
            #line hidden

            #line 19 ""
            this.Write("\nclass ");

            #line default
            #line hidden

            #line 20 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( RSP.Process.Name ));

            #line default
            #line hidden

            #line 20 ""
            this.Write(" : public IProcess {\n    // Insert additional private variables and methods here\n" +
                    "    // #### USER-DATA-PRIVATE-START\n    // #### USER-DATA-PRIVATE-END\n\nprivate:\n" +
                    "");

            #line default
            #line hidden

            #line 26 ""
 if (RSP.Process.SharedVariables.Any()) {

            #line default
            #line hidden

            #line 27 ""
            this.Write("    // Shared variables\n");

            #line default
            #line hidden

            #line 28 ""
     foreach(var v in RSP.Process.SharedVariables) {

            #line default
            #line hidden

            #line 29 ""
            this.Write("    ");

            #line default
            #line hidden

            #line 29 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( Type(v) ));

            #line default
            #line hidden

            #line 29 ""
            this.Write(" ");

            #line default
            #line hidden

            #line 29 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( v.Name ));

            #line default
            #line hidden

            #line 29 ""
            this.Write(";\n");

            #line default
            #line hidden

            #line 30 ""
         if (v.MSCAType.IsArrayType()) {

            #line default
            #line hidden

            #line 31 ""
            this.Write("    size_t size_");

            #line default
            #line hidden

            #line 31 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( v.Name ));

            #line default
            #line hidden

            #line 31 ""
            this.Write(";\n");

            #line default
            #line hidden

            #line 32 ""
         }

            #line default
            #line hidden

            #line 33 ""
     }

            #line default
            #line hidden

            #line 34 ""
            this.Write("\n");

            #line default
            #line hidden

            #line 35 ""
 }

            #line default
            #line hidden

            #line 36 ""
 if (RSP.Process.SharedSignals.Any()) {

            #line default
            #line hidden

            #line 37 ""
            this.Write("    // Shared signals\n");

            #line default
            #line hidden

            #line 38 ""
     foreach(var v in RSP.Process.SharedSignals) {

            #line default
            #line hidden

            #line 39 ""
            this.Write("    ");

            #line default
            #line hidden

            #line 39 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( Type(v) ));

            #line default
            #line hidden

            #line 39 ""
            this.Write(" ");

            #line default
            #line hidden

            #line 39 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( v.Name ));

            #line default
            #line hidden

            #line 39 ""
            this.Write(";\n");

            #line default
            #line hidden

            #line 40 ""
         if (v.MSCAType.IsArrayType()) {

            #line default
            #line hidden

            #line 41 ""
            this.Write("    size_t size_");

            #line default
            #line hidden

            #line 41 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( v.Name ));

            #line default
            #line hidden

            #line 41 ""
            this.Write(";\n");

            #line default
            #line hidden

            #line 42 ""
         }

            #line default
            #line hidden

            #line 43 ""
     }

            #line default
            #line hidden

            #line 44 ""
            this.Write("\n");

            #line default
            #line hidden

            #line 45 ""
 }

            #line default
            #line hidden

            #line 46 ""
            this.Write("    // Bus pointers\n");

            #line default
            #line hidden

            #line 47 ""
 foreach(var bus in busses) {

            #line default
            #line hidden

            #line 48 ""
            this.Write("    ");

            #line default
            #line hidden

            #line 48 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( bus.Name ));

            #line default
            #line hidden

            #line 48 ""
            this.Write("* bus_");

            #line default
            #line hidden

            #line 48 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( Naming.BusNameToValidName(bus, RSP.Process) ));

            #line default
            #line hidden

            #line 48 ""
            this.Write(";\n");

            #line default
            #line hidden

            #line 49 ""
 }

            #line default
            #line hidden

            #line 50 ""
            this.Write("\n");

            #line default
            #line hidden

            #line 51 ""
 if (RSP.Process.Methods != null && RSP.Process.Methods.Any(x => !x.Ignore)) {

            #line default
            #line hidden

            #line 52 ""
            this.Write("    // Internal methods\n");

            #line default
            #line hidden

            #line 53 ""
     foreach (var s in RSP.Process.Methods.Where(x => !x.Ignore)) {

            #line default
            #line hidden

            #line 54 ""

           var rettype = (s.ReturnVariable == null || s.ReturnVariable.MSCAType.IsSameTypeReference(typeof(void))) ? "void" : Type(s.ReturnVariable);


            #line default
            #line hidden

            #line 57 ""
            this.Write("    ");

            #line default
            #line hidden

            #line 57 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( rettype ));

            #line default
            #line hidden

            #line 57 ""
            this.Write(" ");

            #line default
            #line hidden

            #line 57 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( s.Name ));

            #line default
            #line hidden

            #line 57 ""
            this.Write("(");

            #line default
            #line hidden

            #line 57 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( string.Join(", ", s.Parameters.Select(x => $"{Type(x)} {x.Name}")) ));

            #line default
            #line hidden

            #line 57 ""
            this.Write(");\n");

            #line default
            #line hidden

            #line 58 ""
     }

            #line default
            #line hidden

            #line 59 ""
 }

            #line default
            #line hidden

            #line 60 ""
            this.Write("\npublic:\n    ");

            #line default
            #line hidden

            #line 62 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( RSP.Process.Name ));

            #line default
            #line hidden

            #line 62 ""
            this.Write("(\n");

            #line default
            #line hidden

            #line 63 ""
     foreach(var bus in busses) {

            #line default
            #line hidden

            #line 64 ""
            this.Write("        ");

            #line default
            #line hidden

            #line 64 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( bus.Name ));

            #line default
            #line hidden

            #line 64 ""
            this.Write("* p");

            #line default
            #line hidden

            #line 64 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( Naming.BusNameToValidName(bus, RSP.Process) ));

            #line default
            #line hidden

            #line 64 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( (bus == busses.Last() && members.Length == 0) ? "" : "," ));

            #line default
            #line hidden

            #line 64 ""
            this.Write("\n");

            #line default
            #line hidden

            #line 65 ""
      }

            #line default
            #line hidden

            #line 66 ""
 foreach(var v in members) {
       var rt = RS.TypeScope.GetType(v);
       if (rt.IsArray) {

            #line default
            #line hidden

            #line 69 ""
            this.Write("        size_t init_size_");

            #line default
            #line hidden

            #line 69 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( v.Name ));

            #line default
            #line hidden

            #line 69 ""
            this.Write(",\n");

            #line default
            #line hidden

            #line 70 ""
     }

            #line default
            #line hidden

            #line 71 ""
            this.Write("        const ");

            #line default
            #line hidden

            #line 71 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( Type(v) ));

            #line default
            #line hidden

            #line 71 ""
            this.Write(" init_");

            #line default
            #line hidden

            #line 71 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( v.Name ));

            #line default
            #line hidden

            #line 71 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( v == members.Last() ? "" : "," ));

            #line default
            #line hidden

            #line 71 ""
            this.Write("\n");

            #line default
            #line hidden

            #line 72 ""
  }

            #line default
            #line hidden

            #line 73 ""
            this.Write("    );\n\n    void onTick();\n};\n\n#endif /* SME_");

            #line default
            #line hidden

            #line 78 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( RSP.Process.Name.ToUpper() ));

            #line default
            #line hidden

            #line 78 ""
            this.Write("_HPP */\n");

            #line default
            #line hidden
            return this.GenerationEnvironment.ToString();
        }

        public virtual void Initialize() {
        }
    }

    public class ProcessHeaderBase {

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
