using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SME
{
    /// <summary>
    /// Class for statically analysis to build an acyclic dependecy graph.
    /// </summary>
    public class DependencyGraph
    {
        /// <summary>
        /// A representation of a graph node.
        /// </summary>
        public interface INode
        {
            /// <summary>
            /// The parents of the node.
            /// </summary>
            INode[] Parents { get; }
            /// <summary>
            /// The children of the node.
            /// </summary>
            INode[] Children { get; }
            /// <summary>
            /// The <see cref="T:SME.Process"/> instance inside the node.
            /// </summary>
            IProcess Item { get; }
            /// <summary>
            /// The instances of <see cref="T:SME.Bus"/>, which should propagate after this node has finished.
            /// </summary>
            IRuntimeBus[] PropagateAfter { get; }
            /// <summary>
            /// Flag indicating if the node has finished.
            /// </summary>
            bool Fired { get; set; }
            /// <summary>
            /// Counter indicating how many of the nodes inputs are ready.
            /// </summary>
            int InputsReady { get; set; }
            /// <summary>
            /// Method, which resets the node.
            /// </summary>
            void Reset();
        }

        /// <summary>
        /// the graph element.
        /// </summary>
        private class Node : INode
        {
            /// <summary>
            /// The parents of the node.
            /// </summary>
            public List<Node> Parents = new List<Node>();
            /// <summary>
            /// The children of the node.
            /// </summary>
            public List<Node> Children = new List<Node>();
            /// <summary>
            /// The <see cref="T:SME.Process"/> instance inside the node.
            /// </summary>
            public readonly IProcess Item;
            /// <summary>
            /// The instances of <see cref="T:SME.Bus"/>, which should propagate after this node has finished.
            /// </summary>
            public List<IRuntimeBus> PropagateAfter = new List<IRuntimeBus>();
            /// <summary>
            /// Flag indicating if the node has finished.
            /// </summary>
            public bool Fired { get; set; } = false;
            /// <summary>
            /// Counter indicating how many of the nodes inputs are ready.
            /// </summary>
            public int InputsReady { get; set; } = 0;

            /// <summary>
            /// Creates a new instance of the <see cref="SME.Node"> class with the given process inside.
            /// </summary>
            /// <param name="component">The process inside the node.</param>
            public Node(IProcess component)
            {
                Item = component;
            }

            /// <summary>
            /// Adds the given node into the list of parents.
            /// </summary>
            /// <param name="parent">The parent node to insert.</param>
            public void AddParent(Node parent)
            {
                if (parent == this)
                    return;
                Parents.Add(parent);
            }

            /// <summary>
            /// Adds the given node into the list of children.
            /// </summary>
            /// <param name="child">The child node to insert.</param>
            public void AddChild(Node child)
            {
                if (child == this)
                    return;
                Children.Add(child);
            }

            /// <summary>
            /// Adds the given collection of buses into the list of buses.
            /// </summary>
            /// <param name="buses">The collection of buses to insert.</param>
            public void AddBus(IEnumerable<IRuntimeBus> buses)
            {
                foreach (var bus in buses)
                    AddBus(bus);
            }

            /// <summary>
            /// Adds the given buses into the list of buses.
            /// </summary>
            /// <param name="bus">The buses to insert.</param>
            public void AddBus(IRuntimeBus bus)
            {
                PropagateAfter.Add(bus);
            }

            /// <summary>
            /// Returns true if the given node is a child of the current node.
            /// <summary>
            /// <param name="n">The node to check for.</param>
            public bool IsInChildren(Node n)
            {
                var work = new Queue<Node>();
                work.Enqueue(this);
                while (work.Count > 0)
                {
                    var w = work.Dequeue();
                    foreach (var v in w.Children)
                        if (v == n)
                            return true;
                        else if (v.Children.Count > 0)
                            work.Enqueue(v);
                }

                return false;
            }

            /// <summary>
            /// Resets the internal state of the node.
            /// </summary>
            public void Reset()
            {
                Fired = false;
                InputsReady = 0;
            }

            #region INode implementation

            INode[] INode.Parents { get { return Parents.Cast<INode>().ToArray(); } }
            INode[] INode.Children { get { return Children.Cast<INode>().ToArray(); } }
            IProcess INode.Item { get { return Item; } }
            IRuntimeBus[] INode.PropagateAfter { get { return PropagateAfter.ToArray(); } }

            #endregion
        }

        /// <summary>
        /// The order in which the processes are told to continue.
        /// </summary>
        private readonly Node[] m_executionPlan;

        /// <summary>
        /// A callback method to invoke before each tick.
        /// </summary>
        private readonly Action<DependencyGraph> m_pretickcallback;

        /// <summary>
        /// A callback method to invoke before each tick.
        /// </summary>
        private readonly Action<DependencyGraph> m_posttickcallback;

        /// <summary>
        /// A callback method to invoke after propagating clocked processes.
        /// </summary>
        private readonly Action<DependencyGraph> m_clocktickcallback;

        /// <summary>
        /// List of all clocked busses.
        /// </summary>
        private readonly IRuntimeBus[] m_clockedBusses;

        /// <summary>
        /// List of all busses.
        /// </summary>
        public IRuntimeBus[] AllBusses { get; private set; }

        /// <summary>
        /// Readonly access to the execution plan, i.e. the root nodes.
        /// </summary>
        /// <value>The execution plan.</value>
        public INode[] ExecutionPlan { get { return m_executionPlan.Cast<INode>().ToArray(); } }

        /// <summary>
        /// Returns a collection of Node, indicating a dependency cycle.
        /// Assumes non of the Nodes in the given graph are clocked, as they
        /// should have been removed prior to calling this function.
        /// </summary>
        /// <param name="start">The start Node of the cycle</param>
        /// <param name="current">The currently visited child</param>
        /// <returns>A collection of Node, indicating a dependency cycle.</returns>
        private IEnumerable<Node> ContainsCycle(Node start, Node current)
        {
            if (start == current)
                yield return current;
            else
            {
                var cycle = current.Children.SelectMany(x => ContainsCycle(start, x));
                if (cycle.Any())
                {
                    yield return current;
                    foreach (var cycle_entry in cycle)
                        yield return cycle_entry;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SME.DependencyGraph"/> class.
        /// </summary>
        /// <param name="components">The components in the graph.</param>
        public DependencyGraph(IEnumerable<IProcess> components, Action<DependencyGraph> pretickcallback = null, Action<DependencyGraph> posttickcallback = null, Action<DependencyGraph> clocktickcallback = null)
        {
            m_pretickcallback = pretickcallback;
            m_posttickcallback = posttickcallback;
            m_clocktickcallback = clocktickcallback;

            // Wrap all processes in nodes
            var nodeLookup = components
                .Select(x => new Node(x))
                .ToDictionary(k => k.Item, k => k);

            // Build a map, indicating which processes write to a given bus
            var neededForOutput = nodeLookup
                .SelectMany(x => x.Key.OutputBusses
                    .SelectMany(y => y)
                    .Where(y => y != null)
                    .Select(y => new
                        {
                            Bus = y,
                            Node = x.Value
                        }
                    )
                )
                .GroupBy(x => x.Bus)
                .ToDictionary(
                    x => x.Key,
                    y => y.Select(n => n.Node).ToList());

            AllBusses = components
                .SelectMany(x => x.InputBusses
                    .Concat(x.OutputBusses)
                    .Concat(x.InternalBusses)
                    .Concat(x.ClockedInputBusses)
                )
                .SelectMany(x => x)
                .Where(x => x != null)
                .Distinct()
                .Cast<IRuntimeBus>()
                .ToArray();

            m_clockedBusses = AllBusses
                .Where(x => x.IsClocked)
                .ToArray();

            // Build the tree by assigning children and parents to the nodes
            foreach (var n in nodeLookup.Values)
            {
                foreach (var b in n.Item.InputBusses.SelectMany(x => x))
                {
                    if (b == null)
                        throw new Exception(string.Format("Found an unassigned input bus for {0}", n.Item.GetType().FullName));

                    List<Node> waitsFor;
                    if (!neededForOutput.TryGetValue(b, out waitsFor))
                    {
                        var target = nodeLookup.Where(x => x.Key.OutputBusses.Any(y => y == null)).FirstOrDefault();
                        if (target.Key == null)
                            throw new Exception(string.Format("A process from the type {0} depends on the bus of type {1}, but no process writes this bus", n.Item.GetType().FullName, b.BusType.FullName));
                        else
                            throw new Exception(string.Format("A process from the type {0} depends on the bus of type {1}, but no process writes this bus. Also, the process {2} has an unassigned output bus.", n.Item.GetType().FullName, b.BusType.FullName, target.Key.GetType().FullName));
                    }

                    foreach (var p in waitsFor)
                    {
                        p.AddChild(n);
                        n.AddParent(p);
                    }
                }
            }

            // All clocked processes are pre-processed
            var unfinishedProcesses = nodeLookup.Values
                .Where(x => !x.Item.IsClockedProcess)
                .ToList();
            var finished = nodeLookup.Values
                .Where(x => x.Item.IsClockedProcess)
                .ToList();
            var completed = finished
                .ToDictionary(k => k, v => (string)null);

            // The last clocked process drives the bus signals for clocked
            // processes
            var lastClockedProcess = finished.Last();
            lastClockedProcess.AddBus(
                finished
                    .SelectMany(x =>
                        x.Item.OutputBusses
                            .SelectMany(y => y))
                    .Distinct());

            var count = 0;
            while(count != unfinishedProcesses.Count)
            {
                count = unfinishedProcesses.Count;

                for (var i = unfinishedProcesses.Count - 1; i >= 0; i--)
                {
                    var p = unfinishedProcesses[i];

                    // An unclocked process with no parents will never be triggered.
                    var ready = p.Parents.Any();

                    // Check if this process has all dependencies met
                    foreach (var b in p.Parents)
                        if (!completed.ContainsKey(b))
                        {
                            ready = false;
                            break;
                        }

                    // If all dependencies are ready, schedule this process and its busses
                    if (ready)
                    {
                        unfinishedProcesses.RemoveAt(i);
                        completed[p] = null;
                        finished.Add(p);

                        foreach (var b in p.Item.OutputBusses.SelectMany(x => x))
                            if (!neededForOutput[b].Where(x => !(completed.ContainsKey(x) || p.IsInChildren(x))).Any())
                            {
                                // Clocked busses are handled by the execution system
                                if (!b.IsClocked)
                                    p.AddBus(b);
                            }
                    }
                }

            }

            if (unfinishedProcesses.Count != 0)
            {
                for (var i = unfinishedProcesses.Count - 1; i >= 0; i--)
                {
                    var proc = unfinishedProcesses[i];
                    var cycle = proc.Children.SelectMany(child => ContainsCycle(proc, child));
                    if (cycle.Any())
                    {
                        var nl = Environment.NewLine;
                        var cycle_start = proc.Item.GetType().FullName;
                        var names = cycle.Select(node => node.Item.GetType().FullName);
                        var inner_cycle = string.Join($" ->{nl}", names);
                        var full_cycle = $"{cycle_start} ->{nl}{inner_cycle} ->{nl}{cycle_start}";
                        throw new UnclockedCycleException($"Found a dependency cycle in the graph:{nl}{full_cycle}");
                    }
                }

                throw new NoWritingParentsException(string.Format("Attempted to build dependency list, but the following processes depend on a bus that is never written: {0}", string.Join(Environment.NewLine, unfinishedProcesses.Select(x => x.Item.GetType().FullName))));
            }

            m_executionPlan = finished.ToArray();
        }

        /// <summary>
        /// Exception indicating that there exist a collection of unclocked processes that depend on each other.
        /// </summary>
        public class UnclockedCycleException : Exception
        {
            public UnclockedCycleException() { }
            public UnclockedCycleException(string message) : base(message) { }
        }

        /// <summary>
        /// Exception indicating that there exists an unclocked process, which depend on a bus, which is never written.
        /// </summary>
        public class NoWritingParentsException : Exception
        {
            public NoWritingParentsException() { }
            public NoWritingParentsException(string message) : base(message) { }
        }

        /// <summary>
        /// A collection of buses, which will latch some of their signals. These should raise a warning.
        /// </summary>
        private Dictionary<string, string> m_warnedLatches = new Dictionary<string, string>();

        /// <summary>
        /// Advances all processes a tick according to the execution plan.
        /// </summary>
        public void Execute()
        {
            var clock = Scope.Current.Clock;
            clock.Tick();

            m_pretickcallback?.Invoke(this);

            // Start by triggering clocked processes
            var next = m_executionPlan
                .Where(x => x.Item.IsClockedProcess)
                .ToArray();
            var first = true;
            do {
                // Reset and get a new processready task
                var outputready_or_done =
                    next
                        .Select(x => Task.WhenAny(
                            x.Item.ResetProcessReady(),
                            x.Item.Finished()
                        ))
                        .ToArray();

                // Trigger the processes
                Task.WhenAll(
                    next.Select(x => x.Item.SignalInputReady())
                ).Wait();

                // Wait for the processes to be ready again, or for them to finish
                Task.WhenAll(outputready_or_done).Wait();

                // Reset the inputready tasks
                Task.WhenAll(
                    next.Select(x => x.Item.ResetInputReady())
                ).Wait();

                // After triggering the clocked processes, we need to trigger
                // all of the clocked buses
                if (first)
                {
                    foreach (var b in m_clockedBusses.Where(x => x.Clock == clock))
                        b.Propagate();
                    m_clocktickcallback?.Invoke(this);
                    first = false;
                }

                foreach (var node in next)
                {
                    // Propegate the buses
                    foreach (var b in node.Item.OutputBusses.SelectMany(x => x))
                        b.Forward();
                    foreach (var b in node.PropagateAfter)
                        b.Propagate();
                    foreach (var b in node.Item.InternalBusses.SelectMany(x => x))
                        b.Propagate();

                    // Update the graph
                    node.Fired = true;
                    foreach (var child in node.Children)
                        child.InputsReady++;
                }

                // Find the next candidates
                next = m_executionPlan
                    .Where(x => !x.Fired && x.InputsReady == x.Parents.Count)
                    .ToArray();
            } while (next.Any());

            // Verify that all the processes fired
            if (!m_executionPlan.All(x => x.Fired))
                throw new Exception(
                    string.Format("Every process did not trigger: {0}",
                        string.Join(", ",
                            m_executionPlan
                                .Where(x => !x.Fired)
                                .Select(x => x.Item.GetType().FullName)
                        )
                    )
                );

            // Reset the graph
            foreach (var node in m_executionPlan)
                node.Reset();

            m_posttickcallback?.Invoke(this);
        }
    }
}
