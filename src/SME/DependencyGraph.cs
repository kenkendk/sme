using System;
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
		}

		/// <summary>
		/// the graph element
		/// </summary>
		private class Node : INode
		{
			public Node[] Parents = new Node[0];
			public Node[] Children = new Node[0];
			public readonly IProcess Item;
			public IBus[] PropagateAfter = new IBus[0];

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

			public void AddBus(IEnumerable<IBus> bus)
			{
				foreach (var b in bus)
					AddBus(b);
			}

			public void AddBus(IBus bus)
			{
				var b = new IBus[PropagateAfter.Length + 1];
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

			#endregion
		}

		/// <summary>
		/// The order in which the processes are told to continue
		/// </summary>
		private readonly Node[] m_executionPlan;

		/// <summary>
		/// A callback method to invoke before each tick
		/// </summary>
		private Action m_tickcallback;

		/// <summary>
		/// Readonly access to the execution plan, i.e. the root nodes
		/// </summary>
		/// <value>The execution plan.</value>
		public INode[] ExecutionPlan { get { return m_executionPlan.Cast<INode>().ToArray(); } }

		/// <summary>
		/// Initializes a new instance of the <see cref="SME.DependencyGraph"/> class.
		/// </summary>
		/// <param name="components">The components in the graph.</param>
		public DependencyGraph(IEnumerable<IProcess> components, Action tickcallback = null)
		{
			m_tickcallback = tickcallback;

			// Wrap all processes in nodes
			var nodeLookup = components.Select(x => new Node(x)).ToDictionary(k => k.Item, k => k);

			// Build a map, indicating which processes write to a given bus
			var neededForOutput = 
				(from g in
					from n in nodeLookup
					from b in n.Key.OutputBusses
					select new {Bus = b, Node = n.Value}
					group g by g.Bus)
					.ToDictionary(
						k => k.Key, 
						y => y.Select(n => n.Node).ToList()
					);
								

			// Build the tree by assigning children and parents to the nodes
			foreach (var n in nodeLookup.Values)
			{
				foreach (var b in n.Item.InputBusses)
				{
					List<Node> waitsFor;
					if (!neededForOutput.TryGetValue(b, out waitsFor))
						throw new Exception(string.Format("A process from the type {0} depends on the bus of type {1}, but no process writes this bus", n.Item.GetType().FullName, b.BusType.FullName));

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
								if (!BusManager.IsBusClocked(b))
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
		/// <param name="clock">The clock to use.</param>
		public bool Execute(Clock clock = null)
		{
			clock = clock ?? Clock.DefaultClock;
			clock.Tick();

			if (m_tickcallback != null)
				m_tickcallback();

			foreach (var b in BusManager.ClockedBusses.Where(x => x.Value == clock))
				b.Key.Propagate();

			foreach (var n in m_executionPlan)
			{
				n.Item.SignalInputReady().Wait();

				foreach (var b in n.Item.InputBusses)
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

			Loader.CheckForCrashes();
			return Loader.CheckForCompletion();
		}
	}
}

