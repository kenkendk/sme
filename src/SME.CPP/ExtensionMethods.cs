using System;
using System.Collections.Generic;
using System.IO;

namespace SME
{
    /// <summary>
    /// Extension methods
    /// </summary>
    public static class ExtensionMethodsCpp
    {
        /// <summary>
        /// Extension method for building VHDL output after a simulation
        /// </summary>
        /// <returns>The runner.</returns>
        /// <param name="self">The runner.</param>
        /// <param name="targetfolder">The target folder name</param>
        /// <param name="backupfolder">The backup folder name.</param>
        /// <param name="csvfile">The CSV file with simulation results</param>
        /// <param name="customfiles">A list of VHDL files to include in the Makefile, without the VHDL extension</param>
        public static Simulation BuildCPP(this Simulation self, string targetfolder = "cpp", string backupfolder = null, string csvfile = "../trace.csv", IEnumerable<string> customfiles = null)
        {
            self.AddPostloader(sim =>
            {
                new SME.CPP.RenderState(sim, Path.Combine(sim.TargetFolder, targetfolder ?? string.Empty), backupfolder, csvfile, customfiles).Render();
            });
            return self;
        }
    }
}
