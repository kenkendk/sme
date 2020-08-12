using System;
using System.Collections.Generic;

namespace SME.VHDL
{
    /// <summary>
    /// Static helper to simplify rendering without using a simulation.
    /// </summary>
    public static class Renderer
    {
        /// <summary>
        /// Performs the VHDL rendering.
        /// </summary>
        /// <param name="simulation">The simulation instance to parse.</param>
        /// <param name="targetfolder">The folder where the output is stored.</param>
        /// <param name="backupfolder">The folder where backups are stored.</param>
        /// <param name="csvtracename">The name of the CSV trace file.</param>
        /// <param name="customfiles">A list of VHDL files to include in the Makefile, without the VHDL extension.</param>
        /// <param name="config">The device and render configuration.</param>
        public static void Render(Simulation simulation, string targetfolder, string backupfolder = null, string csvtracename = null, IEnumerable<string> customfiles = null, RenderConfig config = null)
        {
            new RenderState(simulation, targetfolder, backupfolder, csvtracename, customfiles, config).Render();
        }
    }
}
