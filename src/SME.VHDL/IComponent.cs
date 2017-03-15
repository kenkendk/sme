using System;

namespace SME.VHDL
{
	/// <summary>
	/// Interface for a VHDL component
	/// </summary>
	public interface IVHDLComponent
	{
		/// <summary>
		/// Method to output the signal region of the component
		/// </summary>
		/// <returns>The signal region.</returns>
		/// <param name="componentname">The name of this component.</param>
		/// <param name="indentation">The indentation level to use.</param>
		string SignalRegion(string componentname, int indentation);
		/// <summary>
		/// Method to output the process region of the component
		/// </summary>
		/// <returns>The process region.</returns>
		/// <param name="componentname">The name of this component.</param>
		/// <param name="indentation">The indentation level to use.</param>
		string ProcessRegion(string componentname, int indentation);
	}
}

