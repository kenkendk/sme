using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace SME.Render.VHDL
{
	public static class VHDLName
	{
		public static string BusSignalNameToVHDLName(IProcess process, System.Reflection.PropertyInfo pi)
		{
			if (process != null && pi.DeclaringType.DeclaringType == process.GetType())
				return ConvertToValidVHDLName(pi.DeclaringType.Name + '_' + pi.Name);

			var busname = pi.DeclaringType.FullName + '_' + pi.Name;
			var asmname = (process == null ? pi.DeclaringType : process.GetType()).Assembly.GetName().Name + '.';
			if (busname.StartsWith(asmname))
				busname = busname.Substring(asmname.Length);

			return ConvertToValidVHDLName(busname);
		}

		public static string AssemblyNameToVHDLName(IEnumerable<IProcess> processes)
		{
			return ConvertToValidVHDLName(processes.First().GetType().Assembly.GetName().Name);
		}

		private static Regex RX_ALPHANUMERIC = new Regex(@"[^\u0030-\u0039|\u0041-\u005A|\u0061-\u007A]");

		public static string ConvertToValidVHDLName(string name)
		{
			var  r = RX_ALPHANUMERIC.Replace(name, "_");
			if (new string[] {"register", "record", "variable", "process", "if", "then", "else", "begin", "end", "architecture", "of", "is"}.Contains(r.ToLowerInvariant()))
				r = "vhdl_" + r;
			return r;
		}	}
}

