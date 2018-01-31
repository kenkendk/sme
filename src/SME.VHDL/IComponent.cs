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
        /// <param name="indentation">The indentation level to use.</param>
        /// <param name="renderer">The renderer building the output</param>
        string IncludeRegion(RenderStateProcess renderer, int indentation);

		/// <summary>
		/// Method to output the signal region of the component
		/// </summary>
		/// <returns>The signal region.</returns>
		/// <param name="indentation">The indentation level to use.</param>
        /// <param name="renderer">The renderer building the output</param>
        string SignalRegion(RenderStateProcess renderer, int indentation);
		/// <summary>
		/// Method to output the process region of the component
		/// </summary>
		/// <returns>The process region.</returns>
		/// <param name="indentation">The indentation level to use.</param>
        /// <param name="renderer">The renderer building the output</param>
        string ProcessRegion(RenderStateProcess renderer, int indentation);
	}
}

