using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SME.AST;

namespace SME.CPP
{
    public static class Naming
    {
        public static string AssemblyNameToFileName(Network network)
        {
            return network.Name;
        }

        public static string SharedDefinitionsFileName(Network network)
        {
            return network.Name + "_SharedDefinitions.hpp";
        }

        public static string BusDefinitionsFileName(Network network)
        {
            return network.Name + "_BusDefinitions.hpp";
        }

        public static string SimulatorFileName(Network network)
        {
            return network.Name + "_Simulator";
        }

        public static string BusImplementationsFileName(Network network)
        {
            return network.Name + "_BusImplementations";
        }

        public static string ProcessNameToFileName(AST.Process process)
        {
            return ToValidName(process.Name) + ".cpp";
        }

        public static string ProcessNameToValidName(AST.Process process)
        {
            return ToValidName(process.InstanceName);
        }

        public static string BusNameToValidName(AST.Bus bus, AST.Process proc = null)
        {
            if (proc != null && proc.LocalBusNames != null && proc.LocalBusNames.ContainsKey(bus))
                return ToValidName(proc.LocalBusNames[bus]);
            return ToValidName(bus.InstanceName);
        }

        public static string BusSignalToValidName(AST.Process process, AST.Signal signal)
        {
            return ToValidName(signal.Name);
        }

        private static Regex RX_ALPHANUMERIC = new Regex(@"[^\u0030-\u0039|\u0041-\u005A|\u0061-\u007A]");

        public static string ToValidName(string name)
        {
            var r = RX_ALPHANUMERIC.Replace(name, "_");
            if (new string[] { "register", "record", "variable", "process", "if", "then", "else", "begin", "end", "architecture", "of", "is" }.Contains(r.ToLowerInvariant()))
                r = "sme_" + r;
            return r.Trim('_');
        }
    }
}
