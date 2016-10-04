using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace SME
{
	/// <summary>
	/// Extension methods
	/// </summary>
	public static class GraphVizExtensionMethods
	{
		/// <summary>
		/// Extension method for adding graph output to a simulation
		/// </summary>
		/// <returns>The runner.</returns>
		/// <param name="self">The runner.</param>
		/// <param name="filename">The output filename.</param>
		public static Simulation BuildGraph(this Simulation self, string filename = "network.dot")
		{
			self.AddPostloader((processes, target) =>
			{
				SME.Render.GraphViz.Renderer.Render(processes, Path.Combine(target, filename));
			});
			return self;
		}
	}
}

namespace SME.Render.GraphViz
{
	/// <summary>
	/// Class for generating a GraphViz display of the current network
	/// </summary>
	public static class Renderer
	{
		/// <summary>
		/// Render the specified processes into a dot file.
		/// </summary>
		/// <param name="components">The proccesses to render.</param>
		/// <param name="file">The filename to write into.</param>
		public static void Render(IEnumerable<IProcess> components, string file)
		{
			var sb = new StringBuilder();

			sb.AppendFormat("digraph {0} {{", components.First().GetType().Assembly.GetName().Name);
			sb.AppendLine();

			var r = new NetworkMapper(components).ReduceToTypes();

			foreach (var b in r.BusDependsOn.Keys.Union(r.DependsOnBus.Keys).Union(r.DependsOnClockedBus.Keys).Distinct())
				sb.AppendFormat("\"{0}\" [shape=oval];{1}", b.FullName, Environment.NewLine);

			foreach (var p in r.BusDependsOn.Values.Union(r.DependsOnBus.Values).Union(r.DependsOnClockedBus.Values).SelectMany(x => x).Select(n => n).Distinct())
				sb.AppendFormat("\"{0}\" [shape=box];{1}", p.FullName, Environment.NewLine);

			foreach (var b in r.BusDependsOn)
				foreach (var p in b.Value)
					sb.AppendFormat("\"{0}\" -> \"{1}\";{2}", b.Key.FullName, p.FullName, Environment.NewLine);

			foreach (var b in r.DependsOnBus)
				foreach (var p in b.Value)
					sb.AppendFormat("\"{1}\" -> \"{0}\";{2}", b.Key.FullName, p.FullName, Environment.NewLine);

			foreach (var b in r.DependsOnClockedBus)
				foreach (var p in b.Value)
					sb.AppendFormat("\"{0}\" -> \"{1}\" [style=dotted];{2}", b.Key.FullName, p.FullName, Environment.NewLine);
			

			sb.AppendLine("}");

			File.WriteAllText(file, sb.ToString());
		}

		private class NetworkMapperTypes
		{
			public Dictionary<Type, List<Type>> BusDependsOn { get; private set; }
			public Dictionary<Type, List<Type>> DependsOnBus { get; private set; }
			public Dictionary<Type, List<Type>> DependsOnClockedBus { get; private set; }

			public NetworkMapperTypes(NetworkMapper mapper)
			{
				BusDependsOn = ReduceToTypes(mapper.BusDependsOn);
				DependsOnBus = ReduceToTypes(mapper.DependsOnBus);
				DependsOnClockedBus = ReduceToTypes(mapper.DependsOnClockedBus);
			}

			private static Dictionary<Type, List<Type>> ReduceToTypes(Dictionary<IBus, List<IProcess>> input)
			{
				var res = new Dictionary<Type, List<Type>>();
				foreach (var k in input)
					res[k.Key.BusType] = k.Value.Select(x => x.GetType()).ToList();
				return res;
			}
		}

		private class NetworkMapper
		{
			public Dictionary<IBus, List<IProcess>> BusDependsOn { get; private set; }
			public Dictionary<IBus, List<IProcess>> DependsOnBus { get; private set; }
			public Dictionary<IBus, List<IProcess>> DependsOnClockedBus { get; private set; }

			public NetworkMapper(IEnumerable<IProcess> components)
			{
				DependsOnBus = 
					(from g in
					from c in components
					from b in c.OutputBusses
					select new {Bus = b, Component = c}
					group g by g.Bus)
						.ToDictionary(
							k => k.Key, 
							y => y.Select(n => n.Component).ToList()
						);

				BusDependsOn = 
					(from g in
						from c in components
						from b in c.InputBusses
						select new {Bus = b, Component = c}
						group g by g.Bus)
						.ToDictionary(
							k => k.Key, 
							y => y.Select(n => n.Component).ToList()
						);

				DependsOnClockedBus = 
					(from g in
						from c in components
						from b in c.ClockedInputBusses
						select new {Bus = b, Component = c}
						group g by g.Bus)
						.ToDictionary(
							k => k.Key, 
							y => y.Select(n => n.Component).ToList()
						);
			}

			public NetworkMapperTypes ReduceToTypes()
			{
				return new NetworkMapperTypes(this);
			}
		}
	}

}

