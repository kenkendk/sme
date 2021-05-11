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

		public static string ProcessNameToFileName(IProcess process)
		{
			return ProcessNameToValidName(process) + ".vhdl";
		}

        public static string ProcessNameToFileName(Type type)
        {
            return ProcessNameToValidName(type) + ".vhdl";
        }

		public static string ProcessNameToValidName(IProcess process)
		{
            return ProcessNameToValidName(process.GetType());
		}

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

		private static Regex RX_ALPHANUMERIC = new Regex(@"[^\u0030-\u0039|\u0041-\u005A|\u0061-\u007A]");

		public static string ToValidName(string name, bool is_bus_signal = false)
		{
			var r = RX_ALPHANUMERIC.Replace(name, "_");
			if (!is_bus_signal && new string[] { "register", "record", "variable", "process", "if", "then", "else", "begin", "end", "architecture", "of", "is", "wait", "block" }.Contains(r.ToLowerInvariant()))
				r = "vhdl_" + r;

            while (r.IndexOf("__", StringComparison.Ordinal) >= 0)
                r = r.Replace("__", "_");

            return r.TrimEnd('_');
		}
	}
}
