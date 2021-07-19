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
    using System;


    public partial class SimulationHeader : SimulationHeaderBase {

        public virtual string TransformText() {
            this.GenerationEnvironment = null;

            #line 1 ""
            this.Write("﻿");

            #line default
            #line hidden

            #line 6 ""
            this.Write("#include <iostream>\n#include <fstream>\n#include <sstream>\n#include \"SystemTypes.h" +
                    "pp\"\n#include \"");

            #line default
            #line hidden

            #line 10 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( Naming.BusDefinitionsFileName(Network) ));

            #line default
            #line hidden

            #line 10 ""
            this.Write("\"\n#include \"");

            #line default
            #line hidden

            #line 11 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( Naming.SharedDefinitionsFileName(Network) ));

            #line default
            #line hidden

            #line 11 ""
            this.Write("\"\n\n");

            #line default
            #line hidden

            #line 13 ""
 foreach(var process in Network.Processes) {

            #line default
            #line hidden

            #line 14 ""
            this.Write("#include \"");

            #line default
            #line hidden

            #line 14 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( process.Name ));

            #line default
            #line hidden

            #line 14 ""
            this.Write(".hpp\"\n");

            #line default
            #line hidden

            #line 15 ""
 }

            #line default
            #line hidden

            #line 16 ""
            this.Write("\n// Insert additional includes and classes here\n// #### USER-DATA-INCLUDE-START\n/" +
                    "/ #### USER-DATA-INCLUDE-END\n\nclass ");

            #line default
            #line hidden

            #line 21 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( Naming.AssemblyNameToFileName(Network) ));

            #line default
            #line hidden

            #line 21 ""
            this.Write(" {\n\n// Insert additional variables and methods here\n// #### USER-VARIABLE-INCLUDE" +
                    "-START\n// #### USER-VARIABLE-INCLUDE-END\n\nprivate:\n\n    // Internal Busses\n");

            #line default
            #line hidden

            #line 30 ""
 foreach(var bus in Network.Busses.Where(x => !(x.IsTopLevelInput || x.IsTopLevelOutput))) {

            #line default
            #line hidden

            #line 31 ""
            this.Write("    ");

            #line default
            #line hidden

            #line 31 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( bus.Name ));

            #line default
            #line hidden

            #line 31 ""
            this.Write(" bus_");

            #line default
            #line hidden

            #line 31 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( Naming.BusNameToValidName(bus) ));

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
            this.Write("\n    // Processes\n");

            #line default
            #line hidden

            #line 35 ""
 foreach(var process in Network.Processes) {

            #line default
            #line hidden

            #line 36 ""
            this.Write("    ");

            #line default
            #line hidden

            #line 36 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( process.Name ));

            #line default
            #line hidden

            #line 36 ""
            this.Write(" proc_");

            #line default
            #line hidden

            #line 36 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( Naming.ProcessNameToValidName(process) ));

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
            this.Write("\n    // The trace input file, if any\n    std::ifstream* trace_input;\n\n    // The " +
                    "current trace input line\n    std::string input_line;\n\n    // The currently simul" +
                    "ated cycle\n    size_t cycle;\n\npublic:\n    // Top level input/output busses\n");

            #line default
            #line hidden

            #line 50 ""
 foreach(var bus in Network.Busses.Where(x => (x.IsTopLevelInput || x.IsTopLevelOutput))) {

            #line default
            #line hidden

            #line 51 ""
            this.Write("    ");

            #line default
            #line hidden

            #line 51 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( bus.Name ));

            #line default
            #line hidden

            #line 51 ""
            this.Write(" bus_");

            #line default
            #line hidden

            #line 51 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( Naming.BusNameToValidName(bus) ));

            #line default
            #line hidden

            #line 51 ""
            this.Write(";\n");

            #line default
            #line hidden

            #line 52 ""
 }

            #line default
            #line hidden

            #line 53 ""
            this.Write("\n    // Default constructor\n    ");

            #line default
            #line hidden

            #line 55 ""
            this.Write(this.ToStringHelper.ToStringWithCulture( Naming.AssemblyNameToFileName(Network) ));

            #line default
            #line hidden

            #line 55 ""
            this.Write(@"();

    // Helper method for running a complete simulation from a
    // trace file
    size_t RunSimulation(const char* inputfile);

    // Opens the file and prepares the input for driving signals
    // and post simulation verification
    void LoadTraceInput(const char* inputfile);

    // Drives the input signals with the values found in the
    // tracefile passed to the constructor
    bool DriveFromTraceInput();

    // Prepares the simulation for the next tick
    void FinishCycle();

    // Performs a single iteration of the program
    void OnTick();

    // Performs post-tick verification of all signals
    void VerifyTrace();

    // Shuts down the simulation, closing all open files
    void Stop();

    // Gets the current cycle
    size_t Cycle() { return cycle; }
};");

            #line default
            #line hidden
            return this.GenerationEnvironment.ToString();
        }

        public virtual void Initialize() {
        }
    }

    public class SimulationHeaderBase {

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
