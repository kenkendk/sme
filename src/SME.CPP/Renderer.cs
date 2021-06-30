using System;
using System.Collections.Generic;

namespace SME.CPP
{
    /// <summary>
    /// Static helper to simplify rendering without using a simulation
    /// </summary>
    public static class Renderer
    {
        /// <summary>
        /// Performs the CPP rendering.
        /// </summary>
        /// <param name="simulation">The simulation to parse.</param>
        /// <param name="targetfolder">The folder where the output is stored.</param>
        /// <param name="backupfolder">The folder where backups are stored.</param>
        /// <param name="csvtracename">The name of the CSV trace file.</param>
        /// <param name="customfiles">A list of CPP files to include in the Makefile, without the CPP extension</param>
        public static void Render(Simulation simulation, string targetfolder, string backupfolder = null, string csvtracename = null, IEnumerable<string> customfiles = null)
        {
            new RenderState(simulation, targetfolder, backupfolder, csvtracename, customfiles).Render();
        }
    }
}
