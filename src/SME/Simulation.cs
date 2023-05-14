using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace SME
{
    /// <summary>
    /// Helper class to run a simulation.
    /// </summary>
    public class Simulation : IDisposable
    {
        /// <summary>
        /// The methods to call prior to running the simulation.
        /// </summary>
        private readonly List<Action<Simulation>> m_preloaders = new List<Action<Simulation>>();
        /// <summary>
        /// The methods to call each cycle before running the network during the simulation.
        /// </summary>
        private readonly List<Action<Simulation>> m_prerunners = new List<Action<Simulation>>();
        /// <summary>
        /// The methods to call each cycle before running the network during the simulation.
        /// </summary>
        private readonly List<Action<Simulation>> m_postrunners = new List<Action<Simulation>>();
        /// <summary>
        /// The methods to call each cycle after clocked process propagation during the simulation.
        /// </summary>
        private readonly List<Action<Simulation>> m_clockrunners = new List<Action<Simulation>>();

        /// <summary>
        /// The methods to call after the simulation.
        /// </summary>
        private readonly List<Action<Simulation>> m_postloaders = new List<Action<Simulation>>();
        /// <summary>
        /// The list of processes in the simulation.
        /// </summary>
        private readonly Dictionary<IProcess, ProcessMetadata> m_processes = new Dictionary<IProcess, ProcessMetadata>();

        /// <summary>
        /// Create a unique scope for the simulation.
        /// </summary>
        private Scope m_scope = new Scope(isolated: true);

        /// <summary>
        /// The output folder.
        /// </summary>
        public string TargetFolder { get; private set; }

        /// <summary>
        /// Gets the current tick value.
        /// </summary>
        public ulong Tick { get; private set; }

        /// <summary>
        /// Specifies whether or not the current simulation is running.
        /// </summary>
        private bool Running;

        /// <summary>
        /// Gets the currently running processes.
        /// </summary>
        public IList<ProcessMetadata> Processes => m_processes.Values.ToList();

        /// <summary>
        /// Bus name lookup table.
        /// </summary>
        /// <value>The bus names.</value>
        public Dictionary<IBus, string> BusNames { get; } = new Dictionary<IBus, string>();

        /// <summary>
        /// The list of bus instances that are registered as top-level inputs.
        /// </summary>
        public readonly HashSet<IBus> TopLevelInputBusses = new HashSet<IBus>();

        /// <summary>
        /// The list of bus instances that are registered as top-level outputs.
        /// </summary>
        public readonly HashSet<IBus> TopLevelOutputBusses = new HashSet<IBus>();

        /// <summary>
        /// Gets the current running dependency graph.
        /// </summary>
        public DependencyGraph Graph { get; private set; }

        public static string ProjectPath { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SME.Simulation"/> class.
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
        /// Adds pre-run handler.
        /// </summary>
        /// <returns>The simulation instance for chaining syntax use.</returns>
        /// <param name="prerunner">The prerunner method.</param>
        public Simulation AddPreRunner(Action<Simulation> prerunner)
        {
            if (prerunner == null)
                throw new ArgumentNullException($"{prerunner}");

            m_prerunners.Add(prerunner);
            return this;
        }

        /// <summary>
        /// Adds post-run handler.
        /// </summary>
        /// <param name="postrunner">The postrunner method.</param>
        public Simulation AddPostRunner(Action<Simulation> postrunner)
        {
            if (postrunner == null)
                throw new ArgumentNullException($"{postrunner}");

            m_postrunners.Add(postrunner);
            return this;
        }

        /// <summary>
        /// Adds post-clock-run handler.
        /// </summary>
        /// <param name="postrunner">The post-clock-runner method.</param>
        public Simulation AddPostClockRunner(Action<Simulation> postrunner)
        {
            if (postrunner == null)
                throw new ArgumentNullException($"{postrunner}");

            m_clockrunners.Add(postrunner);
            return this;
        }

        /// <summary>
        /// Adds a callback method that is invoked after each cycle.
        /// </summary>
        /// <returns>The simulation instance for chaining syntax use.</returns>
        /// <param name="ticker">The method to invoke.</param>
        public Simulation AddTicker(Action<Simulation> ticker)
        {
            return AddPostRunner(ticker);
        }

        /// <summary>
        /// Registers one or more busses as TopLevel input.
        /// </summary>
        /// <returns>The simulation instance for chaining syntax use.</returns>
        /// <param name="inputs">The busses to register.</param>
        public Simulation AddTopLevelInputs(params IBus[] inputs)
        {
            foreach (var b in inputs)
                TopLevelInputBusses.Add(b);
            return this;
        }

        /// <summary>
        /// Registers one or more busses as TopLevel input.
        /// </summary>
        /// <returns>The simulation instance for chaining syntax use.</returns>
        /// <param name="outputs">The busses to register.</param>
        public Simulation AddTopLevelOutputs(params IBus[] outputs)
        {
            foreach (var b in outputs)
                TopLevelOutputBusses.Add(b);
            return this;
        }

        /// <summary>
        /// Requests that the current simulation should stop.
        /// </summary>
        public void RequestStop()
        {
            Running = false;
        }

        /// <summary>
        /// Creates a single process for each class that implements <see cref="IProcess"/> and runs the simulation on that.
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
        /// <param name="processes">The processes to run.</param>
        /// <returns>The awaitable task.</returns>
        public void Run(params IProcess[] processes)
        {
            Run(processes, null);
        }

        /// <summary>
        /// Run the specified processes.
        /// </summary>
        /// <param name="processes">The processes to run.</param>
        /// <param name="exitMethod">The exit method, return true to keep running.</param>
        /// <returns>The awaitable task.</returns>
        public void Run(IProcess[] processes = null, Func<bool> exitMethod = null)
        {
            if (processes != null)
                foreach (var p in processes)
                    RegisterProcess(p);

            if (m_processes.Count == 0)
                throw new InvalidOperationException("No processes to run?");

            foreach (var p in m_processes.Values)
                p.RegisterInitializationData();

            // Assign unique names to processes if there are multiple instances
            var processmap = new Dictionary<string, List<ProcessMetadata>>();
            foreach (var p in m_processes.Values)
            {
                List<ProcessMetadata> lp;
                var pn = string.IsNullOrWhiteSpace(p.Instance.Name) ? TypeNameToName(p.Instance.GetType()) : p.InstanceName;

                if (!processmap.TryGetValue(pn, out lp))
                    processmap[pn] = lp = new List<ProcessMetadata>();
                lp.Add(p);
            }

            foreach (var lp in processmap)
            {
                if (lp.Value.Count == 1)
                {
                    lp.Value[0].InstanceName = lp.Key;
                }
                else
                {
                    for (var i = 0; i < lp.Value.Count; i++)
                        lp.Value[i].InstanceName = lp.Key + "#" + i.ToString();
                }
            }

            foreach (var p in m_processes.Values.Select(x => x.Instance).OfType<Process>())
                p.LoadBusMapsIfRequired();

            // Assign unique names to busses if there are multiple instances
            var busmap = new Dictionary<Type, List<IRuntimeBus>>();
            var allBusses = m_processes.Values
                .Select(x => x.Instance)
                .SelectMany(x => x.InputBusses.SelectMany(y => y)
                    .Concat(x.OutputBusses.SelectMany(y => y))
                    .Concat(x.InternalBusses.SelectMany(y => y))
                    .Concat(x.ClockedInputBusses.SelectMany(y => y)))
                .Distinct();

            foreach (var b in allBusses)
            {
                List<IRuntimeBus> lp;
                if (!busmap.TryGetValue(b.BusType, out lp))
                    busmap[b.BusType] = lp = new List<IRuntimeBus>();
                lp.Add(b);

                if (b.BusType.GetCustomAttributes(typeof(TopLevelInputBusAttribute), true).Any())
                    TopLevelInputBusses.Add(b);
                if (b.BusType.GetCustomAttributes(typeof(TopLevelOutputBusAttribute), true).Any())
                    TopLevelOutputBusses.Add(b);
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
                Running = true;
                Graph = new DependencyGraph(
                    m_processes.Keys,
                    g => m_prerunners.ForEach(x => x?.Invoke(this)),
                    g => m_postrunners.ForEach(x => x?.Invoke(this)),
                    g => m_clockrunners.ForEach(x => x?.Invoke(this))
                );

                foreach (var cfg in m_preloaders)
                    cfg(this);

                // Fire up all the processes
                var running_tasks = m_processes.Keys
                    .Select(x =>
                    {
                        SME.Loader.AutoloadBusses(x);
                        return new
                        {
                            Task = x.Run().ContinueWith(y => {
                                x.SignalFinished();
                                if (y.Exception != null)
                                    throw new AggregateException(y.Exception.InnerExceptions);
                            }),
                            Proc = x,
                            HasOutputs = x.OutputBusses.Any()
                        };
                    })
                    .ToArray();

                // Determine when to quit
                if (exitMethod == null)
                {
                    // Wait until all simulation processes completes
                    exitMethod = () => running_tasks
                        .Where(x => x.Proc is SimulationProcess)
                        .All(x => x.Task.IsCompleted);

                    // Wait until all simulation processes that write completes
                    //exitMethod = () => running_tasks
                    //    .Where(x => x.Proc is SimulationProcess && x.HasOutputs)
                    //    .All(x => x.Task.IsCompleted);

                    // Wait until one of the simulation processes completes
                    //exitMethod = () => running_tasks
                    //    .Any(x => x.Proc is SimulationProcess && x.Task.IsCompleted);
                }

                // Keep running until all simulation (stimulation) processes have finished
                while (!exitMethod() && Running)
                {
                    Graph.Execute();
                    foreach (var t in running_tasks)
                        if (t.Task.Status == System.Threading.Tasks.TaskStatus.Running)
                            t.Task.Wait();
                    var crashes = running_tasks
                        .Where(x => x.Task.Exception != null)
                        .SelectMany(x => x.Task.Exception.InnerExceptions);
                    if (crashes.Any())
                        throw new AggregateException(crashes);

                    Tick++;
                }

                foreach (var cfg in m_postloaders)
                    cfg(this);
            }
            finally
            {
                Scope.Current.Clock.Clear();
                this.Dispose();
            }
        }

        /// <summary>
        /// Converts a type to a friendly name.
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
        /// Registers a process for running in this simulation.
        /// </summary>
        /// <param name="p">The process to register.</param>
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
        /// The shared scope key3
        /// </summary>
        private static AsyncLocal<string> _scopekey = new AsyncLocal<string>();

        /// <summary>
        /// Gets or sets the scope key from the call context.
        /// </summary>
        /// <value>The scope key.</value>
        private static string ScopeKey
        {
            get => _scopekey.Value;
            set => _scopekey.Value = value;
        }
    }
}
