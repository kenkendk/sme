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
            self.AddPostloader(sim =>
            {
                SME.GraphViz.Renderer.Render(sim, Path.Combine(sim.TargetFolder, filename));
            });
            return self;
        }
    }
}

namespace SME.GraphViz
{
    /// <summary>
    /// Class for generating a GraphViz display of the current network
    /// </summary>
    public static class Renderer
    {
        /// <summary>
        /// Render the specified processes into a dot file.
        /// </summary>
        /// <param name="simulation">The simulation setup to render.</param>
        /// <param name="file">The filename to write into.</param>
        public static void Render(Simulation simulation, string file)
        {
            var sb = new StringBuilder();

            sb.AppendFormat("digraph {0} {{", simulation.Processes.First().Instance.GetType().Assembly.GetName().Name);
            sb.AppendLine();

            var r = new NetworkMapper(simulation).ReduceToNames();

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


            sb.AppendLine("}");

            File.WriteAllText(file, sb.ToString());
        }

        private class NetworkMapperNames
        {
            public Dictionary<string, List<string>> BusDependsOn { get; private set; }
            public Dictionary<string, List<string>> DependsOnBus { get; private set; }
            public Dictionary<string, List<string>> DependsOnClockedBus { get; private set; }

            public NetworkMapperNames(NetworkMapper mapper)
            {
                BusDependsOn = ReduceToNames(mapper.Simulation, mapper.BusDependsOn);
                DependsOnBus = ReduceToNames(mapper.Simulation, mapper.DependsOnBus);
                DependsOnClockedBus = ReduceToNames(mapper.Simulation, mapper.DependsOnClockedBus);
            }

            private static Dictionary<string, List<string>> ReduceToNames(Simulation simulation, Dictionary<IRuntimeBus, List<ProcessMetadata>> input)
            {
                var res = new Dictionary<string, List<string>>();
                foreach (var k in input)
                    res[simulation.BusNames[k.Key]] = k.Value.Select(x => x.InstanceName).ToList();
                return res;
            }
        }

        private class NetworkMapper
        {
            public readonly Simulation Simulation;
            public Dictionary<IRuntimeBus, List<ProcessMetadata>> BusDependsOn { get; private set; }
            public Dictionary<IRuntimeBus, List<ProcessMetadata>> DependsOnBus { get; private set; }
            public Dictionary<IRuntimeBus, List<ProcessMetadata>> DependsOnClockedBus { get; private set; }

            public NetworkMapper(Simulation simulation)
            {
                Simulation = simulation;

                var components = simulation.Processes;

                DependsOnBus =
                    (from g in
                    from c in components
                    from b in c.Instance.OutputBusses
                    select new {Bus = b, Component = c}
                    group g by g.Bus)
                        .ToDictionary(
                            k => k.Key,
                            y => y.Select(n => n.Component).ToList()
                        );

                BusDependsOn =
                    (from g in
                        from c in components
                        from b in c.Instance.InputBusses
                        select new {Bus = b, Component = c}
                        group g by g.Bus)
                        .ToDictionary(
                            k => k.Key,
                            y => y.Select(n => n.Component).ToList()
                        );

                DependsOnClockedBus =
                    (from g in
                        from c in components
                        from b in c.Instance.ClockedInputBusses
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

