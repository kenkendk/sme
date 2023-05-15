using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SME.VHDL
{

    public static class Naming
    {
        public static string AssemblyNameToFileName()
        {
            return $"{Assembly.GetEntryAssembly().GetName().Name}.vhdl";
        }

        /// <summary>
        /// Gets the filename corresponding to the name of the given process.
        /// </summary>
        /// <param name="process">The given process.</param>
        public static string ProcessNameToFileName(IProcess process)
        {
            return ProcessNameToValidName(process) + ".vhdl";
        }

        /// <summary>
        /// Gets the filename corresponding to the name of the given type.
        /// </summary>
        /// <param name="type">The given type.</param>
        public static string ProcessNameToFileName(Type type)
        {
            return ProcessNameToValidName(type) + ".vhdl";
        }

        /// <summary>
        /// Gets a valid name corresponding to the name of the given process.
        /// </summary>
        /// <param name="process">The given process.</param>
        public static string ProcessNameToValidName(IProcess process)
        {
            return ProcessNameToValidName(process.GetType());
        }

        /// <summary>
        /// Gets a valid name of the process corresponding to the name of the given type.
        /// </summary>
        /// <param name="type">The given type.</param>
        public static string ProcessNameToValidName(Type type)
        {
            var processname = type.FullName;
            var extras = string.Empty;
            var prefix = string.Empty;
            if (type.IsGenericType)
            {
                processname = type.GetGenericTypeDefinition().FullName;
                extras = "<" + string.Join(", ", type.GenericTypeArguments.Select(x => x.Name)) + ">";
            }

            var asmname = Assembly.GetEntryAssembly().GetName().Name + '.';
            if (processname.StartsWith(asmname, StringComparison.Ordinal)) // Remove prefixed namespace
                processname = processname.Substring(asmname.Length);

            if (string.Equals(Assembly.GetEntryAssembly().GetName().Name.ToLowerInvariant(), processname.ToLowerInvariant())) // Ensure no nameclash
                prefix = "cls_";

            return ToValidName(prefix + processname + extras);
        }

        /// <summary>
        /// Gets a valid name for a bus corresponding to the given property in the given process.
        /// </summary>
        /// <param name="process">The given process.</param>
        /// <param name="pi">The given property.</param>
        public static string BusSignalToValidName(IProcess process, System.Reflection.PropertyInfo pi)
        {
            if (process != null && pi.DeclaringType.DeclaringType == process.GetType())
                return ToValidName(pi.DeclaringType.Name + '_' + pi.Name);

            var busname = pi.DeclaringType.FullName + '_' + pi.Name;
            var asmname = (process == null ? pi.DeclaringType : process.GetType()).Assembly.GetName().Name + '.';
            if (busname.StartsWith(asmname, StringComparison.Ordinal))
                busname = busname.Substring(asmname.Length);

            return ToValidName(busname);
        }

        public static string AssemblyToValidName()
        {
            return ToValidName(Assembly.GetEntryAssembly().GetName().Name);
        }

        /// <summary>
        /// Gets a valid name corresponding to the name of the assembly of the given simulation.
        /// </summary>
        /// <param name="simulation">The given simulation.</param>
        public static string AssemblyToValidName(Simulation simulation)
        {
            return ToValidName(simulation.Processes.First().Instance.GetType().Assembly.GetName().Name);
        }

        /// <summary>
        /// Gets a valid name corresponding to the name of the assembly of the first of the given processes.
        /// </summary>
        /// <param name="processes">The given processes.</param>
        public static string AssemblyToValidName(IEnumerable<IProcess> processes)
        {
            return ToValidName(processes.First().GetType().Assembly.GetName().Name);
        }

        /// <summary>
        /// Regular expression for alpha numeric strings.
        /// </summary>
        private static Regex RX_ALPHANUMERIC = new Regex(@"[^\u0030-\u0039|\u0041-\u005A|\u0061-\u007A]");

        /// <summary>
        /// Gets a valid name corresponding to the given name. Ensures there's
        /// no clash with VHDL keywords, and that the name can be parsed by
        /// VHDL.
        /// </summary>
        /// <param name="name">The given name.</param>
        /// <param name="is_bus_signal">If the name is a bus signal.</param>
        public static string ToValidName(string name, bool is_bus_signal = false)
        {
            var r = RX_ALPHANUMERIC.Replace(name, "_");
            var keywords = new string[] {
                "register", "record", "variable", "process", "if", "then",
                "else", "begin", "end", "architecture", "of", "is", "wait",
                "block", "generate", "next", "constant", "buffer"
            };
            if (!is_bus_signal && keywords.Contains(r.ToLowerInvariant()))
                r = "vhdl_" + r;

            while (r.IndexOf("__", StringComparison.Ordinal) >= 0)
                r = r.Replace("__", "_");

            return r.TrimEnd('_');
        }
    }
}
