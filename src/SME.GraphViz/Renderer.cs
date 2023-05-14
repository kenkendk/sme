using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace SME
{
    /// <summary>
    /// Extension methods for graph visualization.
    /// </summary>
    public static class GraphVizExtensionMethods
    {
        /// <summary>
        /// Extension method for adding graph output to a simulation.
        /// </summary>
        /// <returns>The runner.</returns>
        /// <param name="self">The runner.</param>
        /// <param name="filename">The output filename.</param>
        public static Simulation BuildGraph(this Simulation self, string filename = "network.dot", bool render_buses = true)
        {
            self.AddPostloader(sim =>
            {
                SME.GraphViz.Renderer.Render(sim, Path.Combine(sim.TargetFolder, filename), render_buses);
            });
            return self;
        }
    }
}

namespace SME.GraphViz
{
    /// <summary>
    /// Class for generating a GraphViz display of the current network.
    /// </summary>
    public static class Renderer
    {
        /// <summary>
        /// Render the specified processes into a dot file.
        /// </summary>
        /// <param name="simulation">The simulation setup to render.</param>
        /// <param name="file">The filename to write into.</param>
        public static void Render(Simulation simulation, string file, bool render_buses = true)
        {
            var sb = new StringBuilder();

            sb.AppendFormat("digraph {0} {{", simulation.Processes.First().Instance.GetType().Assembly.GetName().Name);
            sb.AppendLine();

            var map = new NetworkMapper(simulation);

            if (render_buses)
            {
                var r = map.ReduceToNames();
                foreach (var b in r.BusDependsOn.Keys.Concat(r.DependsOnBus.Keys).Concat(r.DependsOnClockedBus.Keys).Distinct())
                    sb.AppendFormat("\"{0}\" [shape=oval];{1}", b, Environment.NewLine);

                foreach (var p in r.BusDependsOn.Values.Concat(r.DependsOnBus.Values).Concat(r.DependsOnClockedBus.Values).SelectMany(x => x).Select(n => n).Distinct())
                    sb.AppendFormat("\"{0}\" [shape=box];{1}", p, Environment.NewLine);

                foreach (var b in r.BusDependsOn)
                    foreach (var p in b.Value)
                        sb.AppendFormat("\"{0}\" -> \"{1}\";{2}", b.Key, p, Environment.NewLine);

                foreach (var b in r.DependsOnBus)
                    foreach (var p in b.Value)
                        sb.AppendFormat("\"{1}\" -> \"{0}\";{2}", b.Key, p, Environment.NewLine);

                foreach (var b in r.DependsOnClockedBus)
                    foreach (var p in b.Value)
                        sb.AppendFormat("\"{0}\" -> \"{1}\" [style=dotted];{2}", b.Key, p, Environment.NewLine);

            }
            else
            {
                foreach(var p in map.ClockedProcesses)
                    sb.AppendFormat("\"{0}\" [shape=box];{1}",
                        p.InstanceName, Environment.NewLine);

                foreach(var p in map.UnclockedProcesses)
                    sb.AppendFormat("\"{0}\" [shape=box, style=dashed];{1}",
                        p.InstanceName, Environment.NewLine);

                foreach(var p in map.ClockedProcesses.Union(map.UnclockedProcesses))
                    foreach (var b in p.Instance.OutputBusses.SelectMany(x => x))
                        foreach (var s in map.BusDependsOn[b])
                            sb.AppendFormat(
                                "\"{0}\" -> \"{1}\";{2}",
                                p.InstanceName, s.InstanceName, Environment.NewLine
                            );
            }

            sb.AppendLine("}");

            File.WriteAllText(file, sb.ToString());
        }

        /// <summary>
        /// Class for converting a network mapper to a string based one.
        /// </summary>
        private class NetworkMapperNames
        {
            /// <summary>
            /// A dictionary, where given a name of a bus, returns a list of names that the bus depends on.
            /// </summary>
            public Dictionary<string, List<string>> BusDependsOn { get; private set; }
            /// <summary>
            /// A dictionary, where given a name of a bus, returns a list of names that depends on the bus.
            /// </summary>
            public Dictionary<string, List<string>> DependsOnBus { get; private set; }
            /// <summary>
            /// A dictionary, where given a name of a clocked bus, returns a list of names that depends on the bus.
            /// </summary>
            public Dictionary<string, List<string>> DependsOnClockedBus { get; private set; }

            /// <summary>
            /// Constructs a new instance of the network mapper class.
            /// </summary>
            /// <param name="mapper">The network mapper to convert.</param>
            public NetworkMapperNames(NetworkMapper mapper)
            {
                BusDependsOn = ReduceToNames(mapper.Simulation, mapper.BusDependsOn);
                DependsOnBus = ReduceToNames(mapper.Simulation, mapper.DependsOnBus);
                DependsOnClockedBus = ReduceToNames(mapper.Simulation, mapper.DependsOnClockedBus);
            }

            /// <summary>
            /// Reduces the given map of the given simulation to a structure of strings.
            /// </summary>
            /// <param name="simulation">The given simulation.</param>
            /// <param name="input">The given map.</param>
            private static Dictionary<string, List<string>> ReduceToNames(Simulation simulation, Dictionary<IRuntimeBus, List<ProcessMetadata>> input)
            {
                var res = new Dictionary<string, List<string>>();
                foreach (var k in input)
                    res[simulation.BusNames[k.Key]] = k.Value.Select(x => x.InstanceName).ToList();
                return res;
            }
        }

        /// <summary>
        /// A mapping of the network.
        /// </summary>
        private class NetworkMapper
        {
            /// <summary>
            /// The simulation to construct the mapping from.
            /// </summary>
            public readonly Simulation Simulation;
            public IEnumerable<ProcessMetadata> ClockedProcesses;
            public IEnumerable<ProcessMetadata> UnclockedProcesses;
            public IEnumerable<ProcessMetadata> SimulationProcesses;
            /// <summary>
            /// A dictionary, where given a bus, returns a list of processes that the bus depends on.
            /// </summary>
            public Dictionary<IRuntimeBus, List<ProcessMetadata>> BusDependsOn { get; private set; }
            /// <summary>
            /// A dictionary, where given a bus, returns a list of processes that depends on the bus.
            /// </summary>
            public Dictionary<IRuntimeBus, List<ProcessMetadata>> DependsOnBus { get; private set; }
            /// <summary>
            /// A dictionary, where given a clocked bus, returns a list of processes that depends on the bus.
            /// </summary>
            public Dictionary<IRuntimeBus, List<ProcessMetadata>> DependsOnClockedBus { get; private set; }

            /// <summary>
            /// Constructs a new instance of the mapper, based on the given simulation.
            /// </summary>
            /// <param name="simulation">The given simulation.</param>
            public NetworkMapper(Simulation simulation)
            {
                Simulation = simulation;

                var components = simulation.Processes;

                ClockedProcesses = components
                    .Where(x =>
                        !(x.Instance is SimulationProcess) &&
                        x.Instance.IsClockedProcess);

                UnclockedProcesses = components
                    .Where(x =>
                        !(x.Instance is SimulationProcess) &&
                        !x.Instance.IsClockedProcess);

                SimulationProcesses = components
                    .Where(x => x.Instance is SimulationProcess);

                DependsOnBus =
                    (from g in
                    from c in components
                    from b in c.Instance.OutputBusses.SelectMany(x => x)
                    select new {Bus = b, Component = c}
                    group g by g.Bus)
                        .ToDictionary(
                            k => k.Key,
                            y => y.Select(n => n.Component).ToList()
                        );

                BusDependsOn =
                    (from g in
                        from c in components
                        from b in c.Instance.InputBusses.SelectMany(x => x).Union(c.Instance.ClockedInputBusses.SelectMany(x => x)).Distinct()
                        select new {Bus = b, Component = c}
                        group g by g.Bus)
                        .ToDictionary(
                            k => k.Key,
                            y => y.Select(n => n.Component).ToList()
                        );

                DependsOnClockedBus =
                    (from g in
                        from c in components
                        from b in c.Instance.ClockedInputBusses.SelectMany(x => x)
                        select new {Bus = b, Component = c}
                        group g by g.Bus)
                        .ToDictionary(
                            k => k.Key,
                            y => y.Select(n => n.Component).ToList()
                        );
            }

            public NetworkMapperNames ReduceToNames()
            {
                return new NetworkMapperNames(this);
            }
        }
    }

}
