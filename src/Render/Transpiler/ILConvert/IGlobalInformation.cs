using System;
using System.Collections.Generic;

namespace SME.Render.Transpiler.ILConvert
{
	public interface IGlobalInformation
	{
		string ToValidName(string name);
		string ToComment(string comment);

		string AssemblyNameToFileName(IEnumerable<IProcess> processes);
		string ProcessNameToFileName(IProcess process);
		string ProcessNameToValidName(IProcess process);
		string BusSignalToValidName(IProcess process, System.Reflection.PropertyInfo pi);
		string AssemblyToValidName(IEnumerable<IProcess> processes);		
	}
}
