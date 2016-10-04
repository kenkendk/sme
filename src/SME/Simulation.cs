using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

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
		private List<Action<IEnumerable<IProcess>, string>> m_preloaders = new List<Action<IEnumerable<IProcess>, string>>();
		/// <summary>
		/// The methods to call during the simulation
		/// </summary>
		private List<Action<ulong>> m_tickers = new List<Action<ulong>>();
		/// <summary>
		/// The methods to call after the simulation
		/// </summary>
		private List<Action<IEnumerable<IProcess>, string>> m_postloaders = new List<Action<IEnumerable<IProcess>, string>>();

		/// <summary>
		/// The output folder
		/// </summary>
		public string TargetFolder { get; private set; }

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
		public void AddPreloader(Action<IEnumerable<IProcess>, string> loader)
		{
			if (loader == null)
				throw new ArgumentNullException($"{loader}");

			m_preloaders.Add(loader);
		}

		/// <summary>
		/// Adds a postloader.
		/// </summary>
		/// <param name="loader">The postloader.</param>
		public void AddPostloader(Action<IEnumerable<IProcess>, string> loader)
		{
			if (loader == null)
				throw new ArgumentNullException($"{loader}");

			m_postloaders.Add(loader);
		}

		/// <summary>
		/// Adds a ticker.
		/// </summary>
		/// <param name="ticker">The ticker.</param>
		public void AddTicker(Action<ulong> ticker)
		{
			if (ticker == null)
				throw new ArgumentNullException($"{ticker}");

			m_tickers.Add(ticker);
		}


		/// <summary>
		/// Runs the simulation on the specified assembly
		/// </summary>
		/// <param name="asm">The assembly to load and simulate.</param>
		public IEnumerable<IProcess> Run(Assembly asm)
		{
			var processes = SME.Loader.LoadAssemblies(asm);

			foreach(var cfg in m_preloaders)
				cfg(processes, TargetFolder);

			var tick = 0UL;

			SME.Loader.RunUntilCompletion(processes, () =>
			{
				foreach (var clk in m_tickers)
					clk(tick);
				tick++;
			});

			foreach (var cfg in m_postloaders)
				cfg(processes, TargetFolder);

			return processes;			
		}
	}
}
