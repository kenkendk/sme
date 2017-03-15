using System;

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
		public static Simulation BuildVHDL(this Simulation self, string backupfolder = null, string csvfile = "trace.csv")
		{
			self.AddPostloader((processes, target) =>
			{
				new SME.VHDL.RenderState(processes, target, backupfolder, csvfile).Render();
			});
			return self;
		}
	}
}
