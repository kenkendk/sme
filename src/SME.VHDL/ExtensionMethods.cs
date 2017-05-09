using System;
using System.Collections.Generic;
using System.IO;

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
		/// <param name="targetfolder">The target folder where output is written to</param>
		/// <param name="backupfolder">The backup folder name.</param>
		/// <param name="csvfile">The CSV file with simulation results</param>
		/// <param name="customfiles">A list of VHDL files to include in the Makefile, without the VHDL extension</param>
		public static Simulation BuildVHDL(this Simulation self, string targetfolder = "vhdl", string backupfolder = null, string csvfile = "../trace.csv", IEnumerable<string> customfiles = null)
		{
			self.AddPostloader((processes, target) =>
			{
				new SME.VHDL.RenderState(processes, Path.Combine(target, targetfolder ?? string.Empty), backupfolder, csvfile, customfiles).Render();
			});
			return self;
		}
	}
}
