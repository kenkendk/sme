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
    public class Simulation : IDisposable
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
        /// The list of processes in the simulation
        /// </summary>
        private HashSet<IProcess> m_processes = new HashSet<IProcess>();

        /// <summary>
        /// Create a unique scope for the simulation
        /// </summary>
        private Scope m_scope = new Scope(isolated: true);

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
        public IList<IProcess> Processes => m_processes.ToList();

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
            if (Current != null)
                throw new InvalidOperationException("Cannot start a new simulation before the current one is disposed");
            ScopeKey = Guid.NewGuid().ToString();
            Current = this;
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
        /// Run the specified processes.
        /// </summary>
        /// <returns>The awaitable task.</returns>
        public void Run(params IProcess[] processes)
        {
            if (processes != null)
                foreach (var p in processes)
                    RegisterProcess(p);
            
            if (m_processes.Count == 0)
                throw new InvalidOperationException("No processes to run?");

            try
            {
                Tick = 0uL;
                Graph = new DependencyGraph(m_processes);

                foreach (var cfg in m_preloaders)
                    cfg(this);

                var tick = 0UL;

                // Fire up all the processes
                var running_tasks = m_processes
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

        /// <summary>
        /// Releases all resource used by the <see cref="T:SME.Simulation"/> object.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="T:SME.Simulation"/>. The
        /// <see cref="Dispose"/> method leaves the <see cref="T:SME.Simulation"/> in an unusable state. After calling
        /// <see cref="Dispose"/>, you must release all references to the <see cref="T:SME.Simulation"/> so the garbage
        /// collector can reclaim the memory that the <see cref="T:SME.Simulation"/> was occupying.</remarks>
        public void Dispose()
        {
            if (Current == this)
            {
                Current = null;
                m_scope.Dispose();
            }
        }

        /// <summary>
        /// Registers a process for running in this simulation
        /// </summary>
        /// <param name="p">P.</param>
        public void RegisterProcess(IProcess p)
        {
            if (p == null)
                throw new ArgumentNullException(nameof(p));
            m_processes.Add(p);
        }

		/// <summary>
		/// Gets the current scope.
		/// </summary>
		/// <value>The current scope.</value>
        public static Simulation Current
		{
			get
			{
				var key = ScopeKey;
				if (key == null)
					return null;

				_scopes.TryGetValue(key, out var res);
				return res;
			}

			private set
			{
				var key = ScopeKey;
				if (key == null)
					throw new InvalidOperationException("Cannot set the simulation without a key");
                if (value == null)
                    _scopes.Remove(key);
                else if (_scopes.ContainsKey(key) && _scopes[key] != null)
                    throw new InvalidOperationException("Cannot use nested simulations");
                else                    
					_scopes[key] = value;
			}
		}
		/// <summary>
		/// The simulation scopes matching the keys.
		/// </summary>
        private static readonly Dictionary<string, Simulation> _scopes = new Dictionary<string, Simulation>();

		/// <summary>
		/// The key used to store data in the CallContext
		/// </summary>
		private const string CONTEXT_SCOPE_KEY = "SME_SIMULATION_SCOPE_KEY";

		/// <summary>
		/// Gets or sets the scope key from the call context.
		/// </summary>
		/// <value>The scope key.</value>
		private static string ScopeKey
		{
			get { return System.Runtime.Remoting.Messaging.CallContext.GetData(CONTEXT_SCOPE_KEY) as string; }
			set { System.Runtime.Remoting.Messaging.CallContext.SetData(CONTEXT_SCOPE_KEY, value); }
		}
    }
}
