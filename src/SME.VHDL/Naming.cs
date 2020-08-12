using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SME.VHDL
{
    /// <summary>
    /// Helper class with static methods for naming.
    /// </summary>
    public static class Naming
    {
        /// <summary>
        /// Gets the filename corresponding to the assembly name of the given simulation.
        /// </summary>
        /// <param name="simulation">The given simulation</param>
        public static string AssemblyNameToFileName(Simulation simulation)
        {
            return simulation.Processes.First().Instance.GetType().Assembly.GetName().Name + ".vhdl";
        }

        /// <summary>
        /// Gets the filename corresponding to the assembly name of the first of the given processes.
        /// </summary>
        /// <param name="processes">The given processes.</param>
        public static string AssemblyNameToFileName(IEnumerable<IProcess> processes)
        {
            return processes.First().GetType().Assembly.GetName().Name + ".vhdl";
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

            var asmname = type.Assembly.GetName().Name + '.';
            if (processname.StartsWith(asmname, StringComparison.Ordinal))
                processname = processname.Substring(asmname.Length);

            if (string.Equals(type.Assembly.GetName().Name, processname))
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
        /// Gets a valid name from the given string.
        /// </summary>
        /// <param name="name">The given name.</param>
        public static string ToValidName(string name)
        {
            var r = RX_ALPHANUMERIC.Replace(name, "_");
            if (new string[] { "register", "record", "variable", "process", "if", "then", "else", "begin", "end", "architecture", "of", "is", "wait" }.Contains(r.ToLowerInvariant()))
                r = "vhdl_" + r;

            while (r.IndexOf("__", StringComparison.Ordinal) >= 0)
                r = r.Replace("__", "_");

            return r.TrimEnd('_');
        }
    }
}
