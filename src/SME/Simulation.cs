using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SME
{
	/// <summary>
	/// Helper class to run a simulation
	/// </summary>
	public class Simulation
	{
		/// <summary>
		/// The methods to call prior to running the simulation
		/// </summary>
        private List<Action<Simulation>> m_preloaders = new List<Action<Simulation>>();
		/// <summary>
		/// The methods to call during the simulation
		/// </summary>
        private List<Action<Simulation>> m_tickers = new List<Action<Simulation>>();
		/// <summary>
		/// The methods to call after the simulation
		/// </summary>
		private List<Action<Simulation>> m_postloaders = new List<Action<Simulation>>();

		/// <summary>
		/// The output folder
		/// </summary>
		public string TargetFolder { get; private set; }

        /// <summary>
        /// Gets the current tick value.
        /// </summary>
        public ulong Tick { get; private set; }

        /// <summary>
        /// Gets the currently running processes
        /// </summary>
        public IProcess[] Processes { get; private set; }

        /// <summary>
        /// Gets the current running dependency graph
        /// </summary>
        public DependencyGraph Graph { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SME.Runner"/> class.
		/// </summary>
		/// <param name="outputfolder">The folder where output files are stored.</param>
		public Simulation(string outputfolder = "output")
		{
			TargetFolder = Path.GetFullPath(outputfolder);
			if (!Directory.Exists(TargetFolder))
				Directory.CreateDirectory(TargetFolder);			
		}

		/// <summary>
		/// Adds a preloader.
		/// </summary>
		/// <param name="loader">The preloader.</param>
        public Simulation AddPreloader(Action<Simulation> loader)
		{
			if (loader == null)
				throw new ArgumentNullException($"{loader}");

			m_preloaders.Add(loader);
			return this;
		}

		/// <summary>
		/// Adds a postloader.
		/// </summary>
		/// <param name="loader">The postloader.</param>
        public Simulation AddPostloader(Action<Simulation> loader)
		{
			if (loader == null)
				throw new ArgumentNullException($"{loader}");

			m_postloaders.Add(loader);
			return this;
		}

		/// <summary>
		/// Adds a ticker.
		/// </summary>
		/// <param name="ticker">The ticker.</param>
        public Simulation AddTicker(Action<Simulation> ticker)
		{
			if (ticker == null)
				throw new ArgumentNullException($"{ticker}");

			m_tickers.Add(ticker);
            return this;
		}

        /// <summary>
        /// Run the specified processes until one exits or crashes.
        /// </summary>
        /// <returns>The awaitable task.</returns>
        /// <param name="processes">The processes to run.</param>
        public void Run(IEnumerable<IProcess> processes)
        {
            if (processes == null)
                throw new ArgumentNullException(nameof(processes));
            Run(processes.ToArray());
        }

        /// <summary>
        /// Run the specified processes.
        /// </summary>
        /// <returns>The awaitable task.</returns>
        /// <param name="processes">The processes to run.</param>
        public void Run(params IProcess[] processes)
        {
			if (processes == null)
				throw new ArgumentNullException(nameof(processes));
            if (processes.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(processes), "No processes to run?");

            try
            {
                Processes = processes;
                Tick = 0uL;
                Graph = new DependencyGraph(processes);

                foreach (var cfg in m_preloaders)
                    cfg(this);

                var tick = 0UL;

                // Fire up all the processes
                var running_tasks = processes
                    .Select(x =>
                    {
                        SME.Loader.AutoloadBusses(x);
                        return x.Run();
                    }).ToArray();

                while (!running_tasks.Any(x => x.IsCompleted))
                {
                    foreach (var clk in m_tickers)
                        clk(this);
                    tick++;

                    Graph.Execute();

                    var crashes = running_tasks.Where(x => x.Exception != null).SelectMany(x => x.Exception.InnerExceptions);
                    if (crashes.Any())
                        throw new AggregateException(crashes);
                }

                foreach (var cfg in m_postloaders)
                    cfg(this);

            }
            finally
            {
				Scope.Current.Clock.Clear();
			}
		}
	}
}
