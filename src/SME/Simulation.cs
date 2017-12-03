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
        private Dictionary<IProcess, ProcessMetadata> m_processes = new Dictionary<IProcess, ProcessMetadata>();

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
        public IList<ProcessMetadata> Processes => m_processes.Values.ToList();

        /// <summary>
        /// Bus name lookup table
        /// </summary>
        /// <value>The bus names.</value>
        public Dictionary<IBus, string> BusNames { get; } = new Dictionary<IBus, string>();

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
        /// Creates a single process for each class that implements <see cref="IProcess"/> and runs the simulation on that
        /// </summary>
        /// <param name="asm">The assembly to load.</param>
        [System.Obsolete("This method is for supporting older versions that did static exploration only. Please change your code to use Run(Loader.StartProcesses(asm, true)).")]
        public void Run(Assembly asm)
        {
            Run(Loader.StartProcesses(asm, true));
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

            foreach (var p in m_processes.Values)
                p.RegisterInitializationData();

            // Assign unique names to processes if there are multiple instances
            var processmap = new Dictionary<Type, List<ProcessMetadata>>();
            foreach (var p in m_processes.Values)
            {
                List<ProcessMetadata> lp;
                if (!processmap.TryGetValue(p.Instance.GetType(), out lp))
                    processmap[p.Instance.GetType()] = lp = new List<ProcessMetadata>();
                lp.Add(p);
            }

            foreach (var lp in processmap.Values)
            {
                var t = TypeNameToName(lp[0].Instance.GetType());
                if (lp.Count == 1)
                {
                    lp[0].InstanceName = t;
                }
                else
                {
                    for (var i = 0; i < lp.Count; i++)
                        lp[i].InstanceName = t + "#" + i.ToString();
                }
            }


            // Assign unique names to processes if there are multiple instances
            var busmap = new Dictionary<Type, List<IBus>>();
            foreach (var b in m_processes.Values.SelectMany(x => x.Instance.InputBusses.Concat(x.Instance.OutputBusses).Concat(x.Instance.InternalBusses)).Distinct())
            {
                if (b == null)
                    continue;
                
                List<IBus> lp;
                if (!busmap.TryGetValue(b.BusType, out lp))
                    busmap[b.BusType] = lp = new List<IBus>();
                lp.Add(b);
            }

            foreach (var lp in busmap.Values)
            {
                var t = TypeNameToName(lp[0].BusType);

                if (lp.Count == 1)
                {
                    BusNames[lp[0]] = t;
                }
                else
                {
                    for (var i = 0; i < lp.Count; i++)
                        BusNames[lp[i]] = t + "#" + i.ToString();
                }
            }

            try
            {
                Tick = 0uL;
                Graph = new DependencyGraph(m_processes.Keys);

                foreach (var cfg in m_preloaders)
                    cfg(this);

                var tick = 0UL;

                // Fire up all the processes
                var running_tasks = m_processes.Keys
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
        /// Converts a type to a friendly name
        /// </summary>
        /// <returns>The name of the type.</returns>
        /// <param name="type">The type to get the name for.</param>
        public string TypeNameToName(Type type)
        {
            var fullname = type.FullName;
            var extras = string.Empty;
            if (type.IsGenericType)
            {
                fullname = type.GetGenericTypeDefinition().FullName;
                extras = "<" + string.Join(", ", type.GenericTypeArguments.Select(x => x.Name)) + ">";
            }

            var asmname = type.Assembly.GetName().Name + '.';
            if (fullname.StartsWith(asmname, StringComparison.Ordinal))
                fullname = fullname.Substring(asmname.Length);

            return fullname + extras;
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
            if (!m_processes.ContainsKey(p))
                m_processes.Add(p, new ProcessMetadata(p));
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

                Simulation res;
				_scopes.TryGetValue(key, out res);
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
