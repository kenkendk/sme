using System;

namespace SME.Render.VHDL
{
	public interface IVHDLComponent
	{
		string SignalRegion(string componentname, int indentation);
		string ProcessRegion(string componentname, int indentation);
	}
}

