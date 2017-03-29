using System;
using System.Collections.Generic;

namespace SME
{
	/// <summary>
	/// Extension methods
	/// </summary>
	public static class ExtensionMethodsVHDL
	{
		/// <summary>
		/// Extension method for building VHDL output after a simulation
		/// </summary>
		/// <returns>The runner.</returns>
		/// <param name="self">The runner.</param>
		/// <param name="backupfolder">The backup folder name.</param>
		/// <param name="csvfile">The CSV file with simulation results</param>
		/// <param name="customfiles">A list of VHDL files to include in the Makefile, without the VHDL extension</param>
		public static Simulation BuildVHDL(this Simulation self, string backupfolder = null, string csvfile = "trace.csv", IEnumerable<string> customfiles = null)
		{
			self.AddPostloader((processes, target) =>
			{
				new SME.VHDL.RenderState(processes, target, backupfolder, csvfile, customfiles).Render();
			});
			return self;
		}
	}
}
