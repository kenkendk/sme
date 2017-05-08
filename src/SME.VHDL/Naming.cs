using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SME.VHDL
{
	public static class Naming
	{
		public static string AssemblyNameToFileName(IEnumerable<IProcess> processes)
		{
			return processes.First().GetType().Assembly.GetName().Name + ".vhdl";
		}

		public static string ProcessNameToFileName(IProcess process)
		{
			return ProcessNameToValidName(process) + ".vhdl";
		}

		public static string ProcessNameToValidName(IProcess process)
		{
			var processname = process.GetType().FullName;
			var asmname = process.GetType().Assembly.GetName().Name + '.';
			if (processname.StartsWith(asmname, StringComparison.Ordinal))
				processname = processname.Substring(asmname.Length);

			return ToValidName(processname);
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

		public static string AssemblyToValidName(IEnumerable<IProcess> processes)
		{
			return ToValidName(processes.First().GetType().Assembly.GetName().Name);
		}

		private static Regex RX_ALPHANUMERIC = new Regex(@"[^\u0030-\u0039|\u0041-\u005A|\u0061-\u007A]");

		public static string ToValidName(string name)
		{
			var r = RX_ALPHANUMERIC.Replace(name, "_");
			if (new string[] { "register", "record", "variable", "process", "if", "then", "else", "begin", "end", "architecture", "of", "is" }.Contains(r.ToLowerInvariant()))
				r = "vhdl_" + r;
			return r;
		}	
	}
}
