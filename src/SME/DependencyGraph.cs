﻿using System;
using System.Linq;
using System.Collections.Generic;

namespace SME
{
	/// <summary>
	/// Class for statically analysis to build a dependecy graph
	/// </summary>
	public class DependencyGraph
	{
		/// <summary>
		/// A representation of a graph node
		/// </summary>
		public interface INode
		{
			INode[] Parents { get; }
			INode[] Children { get; }
			IProcess Item { get; }
            IRuntimeBus[] PropagateAfter { get; }
		}

		/// <summary>
		/// the graph element
		/// </summary>
		private class Node : INode
		{
			public Node[] Parents = new Node[0];
			public Node[] Children = new Node[0];
			public readonly IProcess Item;
			public IRuntimeBus[] PropagateAfter = new IRuntimeBus[0];

			public Node(IProcess component)
			{
				Item = component;
			}

			public void AddParent(Node parent)
			{
				if (parent == this)
					return;
				
				var p = new Node[Parents.Length + 1];
				Array.Copy(Parents, p, Parents.Length);
				Parents = p;
				Parents[Parents.Length - 1] = parent;
			}

			public void AddChild(Node child)
			{
				if (child == this)
					return;
				
				var c = new Node[Children.Length + 1];
				Array.Copy(Children, c, Children.Length);
				Children = c;
				Children[Children.Length - 1] = child;
			}

			public void AddBus(IEnumerable<IRuntimeBus> bus)
			{
				foreach (var b in bus)
					AddBus(b);
			}

			public void AddBus(IRuntimeBus bus)
			{
				var b = new IRuntimeBus[PropagateAfter.Length + 1];
				Array.Copy(PropagateAfter, b, PropagateAfter.Length);
				PropagateAfter = b;
				PropagateAfter[PropagateAfter.Length - 1] = bus;
			}

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
						else if (v.Children.Length > 0)
							work.Enqueue(v);
				}

				return false;
			}

			#region INode implementation

			INode[] INode.Parents { get { return Parents.Cast<INode>().ToArray(); } }
			INode[] INode.Children { get { return Children.Cast<INode>().ToArray(); } }
			IProcess INode.Item { get { return Item; } }
            IRuntimeBus[] INode.PropagateAfter { get { return PropagateAfter; } }

			#endregion
		}

		/// <summary>
		/// The order in which the processes are told to continue
		/// </summary>
		private readonly Node[] m_executionPlan;

        /// <summary>
        /// A callback method to invoke before each tick
        /// </summary>
        private readonly Action<DependencyGraph> m_pretickcallback;

        /// <summary>
        /// A callback method to invoke before each tick
        /// </summary>
        private readonly Action<DependencyGraph> m_posttickcallback;

        /// <summary>
        /// A callback method to invoke after propagating clocked processes
        /// </summary>
        private readonly Action<DependencyGraph> m_clocktickcallback;

        /// <summary>
        /// List of all clocked busses
        /// </summary>
        private readonly IRuntimeBus[] m_clockedBusses;

        /// <summary>
		/// List of all busses
		/// </summary>
        public IRuntimeBus[] AllBusses { get; private set; }

		/// <summary>
		/// Readonly access to the execution plan, i.e. the root nodes
		/// </summary>
		/// <value>The execution plan.</value>
		public INode[] ExecutionPlan { get { return m_executionPlan.Cast<INode>().ToArray(); } }

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
			var nodeLookup = components.Select(x => new Node(x)).ToDictionary(k => k.Item, k => k);

			// Build a map, indicating which processes write to a given bus
			var neededForOutput = nodeLookup
                .SelectMany(
                    x => x.Key.OutputBusses.Where(y => y != null).Select(
                        y => new { Bus = y, Node = x.Value }))
                .GroupBy(x => x.Bus)
                .ToDictionary(
                    x => x.Key, 
                    y => y.Select(n => n.Node).ToList());

            AllBusses = components
                .SelectMany(x =>
                            x.InputBusses
                            .Concat(x.OutputBusses)
                            .Concat(x.InternalBusses)
                            .Concat(x.ClockedInputBusses)
                           )
                .Where(x => x != null)
                .Distinct()
				.Cast<IRuntimeBus>()
                .ToArray();

            m_clockedBusses = AllBusses.Where(x => x.IsClocked).ToArray();

			// Build the tree by assigning children and parents to the nodes
			foreach (var n in nodeLookup.Values)
			{
				foreach (var b in n.Item.InputBusses)
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
			var unfinishedProcesses = nodeLookup.Values.Where(x => !x.Item.IsClockedProcess).ToList();
			var finished = nodeLookup.Values.Where(x => x.Item.IsClockedProcess).ToList();
			var completed = finished.ToDictionary(k => k, v => (string)null);

			// The last clocked process drives the bus signals for clocked processes
			var lastClockedProcess = finished.Last();
			lastClockedProcess.AddBus(finished.SelectMany(x => x.Item.OutputBusses).Distinct());

			var count = 0;
			while(count != unfinishedProcesses.Count)
			{
				count = unfinishedProcesses.Count;

				for (var i = unfinishedProcesses.Count - 1; i >= 0; i--)
				{
					var ready = true;
					var p = unfinishedProcesses[i];

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

						foreach (var b in p.Item.OutputBusses)
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
					var p = unfinishedProcesses[i];

					// TODO: Should do real cyclic dependencies, otherwise we can only see one link
					var mutuals = unfinishedProcesses.Where(x => x == p || (x.Parents.Contains(p) && p.Parents.Contains(x))).ToList();
					if (mutuals.Count > 1)
					{
						throw new Exception(string.Format("Found a mutual depency in the processes: {0}", string.Join(Environment.NewLine, mutuals.Select(x => x.Item.GetType().FullName))));
					}
				}

				throw new Exception(string.Format("Attempted to build dependency list, but the following processes depend on a bus that is never written: {0}", string.Join(Environment.NewLine, unfinishedProcesses.Select(x => x.Item.GetType().FullName))));
			}

			m_executionPlan = finished.ToArray();
		}

		private Dictionary<string, string> m_warnedLatches = new Dictionary<string, string>();

		/// <summary>
		/// Advances all processes a tick according to the execution plan
		/// </summary>
		public void Execute()
		{
            var clock = Scope.Current.Clock;			
            clock.Tick();

            m_pretickcallback?.Invoke(this);

            // Start by propagating all tasks
            System.Threading.Tasks.Task.WhenAll(
                m_executionPlan
                .Select(x => x.Item)
                .Where(x => x.IsClockedProcess)
                .Select(x => x.SignalInputReady()))
            .Wait();

            // Then forward all busses
            foreach (var b in m_clockedBusses.Where(x => x.Clock == clock))
                b.Propagate();

            m_clocktickcallback?.Invoke(this);

			foreach (var n in m_executionPlan)
			{
                if (!n.Item.IsClockedProcess)
				    n.Item.SignalInputReady().Wait();

				foreach (var b in n.Item.InputBusses.Where(x => !n.Item.OutputBusses.Contains(x)))
					if (b.AnyStaged())
						throw new Exception(string.Format("Process {0} has written the input bus {1}", n.Item.GetType().FullName, b.BusType.FullName));

				if (n.Item.GetType().GetCustomAttributes(typeof(ClockedProcessAttribute), true).FirstOrDefault() == null)
				{
					foreach (var b in n.Item.OutputBusses)
						if (b.NonStaged().Any())
						{
							var key = n.Item.GetType().FullName + ":" + b.BusType.FullName;
							if (!m_warnedLatches.ContainsKey(key))
							{
								Console.WriteLine("Some signals were not written to bus {1} by {0} during the execution, this will lead to latches in the design: {2}", n.Item.GetType().FullName, b.BusType.FullName, string.Join(", ", b.NonStaged()));
								m_warnedLatches[key] = null;
							}
						}
				}

				foreach (var b in n.Item.OutputBusses)
					b.Forward();
					
				foreach (var b in n.PropagateAfter)
					b.Propagate();
				foreach (var b in n.Item.InternalBusses)
					b.Propagate();

			}

            m_posttickcallback?.Invoke(this);
		}
	}
}

