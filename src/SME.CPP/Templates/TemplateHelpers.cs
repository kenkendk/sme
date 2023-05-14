using System;
using System.Collections.Generic;
using System.Linq;
using SME.AST;

namespace SME.CPP.Templates
{
    public partial class TopLevel
    {
        private readonly RenderState RS;
        private readonly AST.Network Network;

        public TopLevel(RenderState rs)
        {
            RS = rs;
            Network = rs.Network;
        }
    }

    public partial class BusDefinitions
    {
        private readonly RenderState RS;
        private readonly AST.Network Network;

        public BusDefinitions(RenderState rs)
        {
            RS = rs;
            Network = rs.Network;
        }

        public string Type(AST.Signal signal)
        {
            return RS.TypeScope.GetType(signal).Name;
        }
    }

    public partial class BusImplementations
    {
        private readonly RenderState RS;
        private readonly AST.Network Network;

        public BusImplementations(RenderState rs)
        {
            RS = rs;
            Network = rs.Network;
        }

        public string Type(AST.Signal signal)
        {
            return RS.TypeScope.GetType(signal).Name;
        }
    }

    public partial class ProcessItem
    {
        private readonly RenderState RS;
        private readonly AST.Network Network;
        private readonly RenderStateProcess RSP;

        public ProcessItem(RenderState rs, RenderStateProcess process)
        {
            RS = rs;
            Network = rs.Network;
            RSP = process;
        }

        public string Type(AST.Signal signal)
        {
            return RS.TypeScope.GetType(signal).Name;
        }

        public string Type(AST.Variable variable)
        {
            return RS.TypeScope.GetType(variable).Name;
        }

        public string Type(AST.Parameter parameter)
        {
            return RS.TypeScope.GetType(parameter).Name;
        }

        public string Type(AST.DataElement el)
        {
            return RS.TypeScope.GetType(el).Name;
        }

    }

    public partial class ProcessHeader
    {
        private readonly RenderState RS;
        private readonly AST.Network Network;
        private readonly RenderStateProcess RSP;

        public ProcessHeader(RenderState rs, RenderStateProcess process)
        {
            RS = rs;
            Network = rs.Network;
            RSP = process;
        }

        public string Type(AST.Signal signal)
        {
            return RS.TypeScope.GetType(signal).Name;
        }

        public string Type(AST.Variable variable)
        {
            return RS.TypeScope.GetType(variable).Name;
        }

        public string Type(AST.Parameter parameter)
        {
            return RS.TypeScope.GetType(parameter).Name;
        }

        public string Type(AST.DataElement el)
        {
            return RS.TypeScope.GetType(el).Name;
        }
    }

    public partial class CustomTypes
    {
        private readonly RenderState RS;
        private readonly AST.Network Network;

        public CustomTypes(RenderState rs)
        {
            RS = rs;
            Network = RS.Network;
        }
    }

    public partial class Makefile
    {
        private readonly RenderState RS;
        private readonly AST.Network Network;

        public Makefile(RenderState rs)
        {
            RS = rs;
            Network = rs.Network;
        }

        public IEnumerable<string> CustomFiles
        {
            get
            {
                return RS.CustomFiles;
            }
        }

        public IEnumerable<string> Filenames
        {
            get
            {
                var known = new HashSet<Type>();
                foreach (var p in RS.Network.Processes)
                {
                    if (known.Contains(p.SourceType))
                        continue;
                    known.Add(p.SourceType);

                    yield return p.Name;
                }

                foreach (var p in RawNames)
                    yield return p;
            }
        }

        public IEnumerable<string> RawNames
        {
            get
            {
                var prefix = typeof(Templates.TopLevel).Namespace + ".";
                return
                    System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames()
                          .Where(x => x.EndsWith(".cpp", StringComparison.InvariantCultureIgnoreCase))
                          .Where(x => x.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase))
                          .Select(x => x.Substring(prefix.Length))
                          .Select(x => x.Substring(0, x.Length - ".cpp".Length));
            }
        }
    }

    public partial class SharedTypes
    {
        private readonly RenderState RS;
        private readonly AST.Network Network;

        public SharedTypes(RenderState rs)
        {
            RS = rs;
            Network = rs.Network;
        }
    }

    public partial class SimulationHeader
    {
        private readonly RenderState RS;
        private readonly AST.Network Network;

        public SimulationHeader(RenderState rs)
        {
            RS = rs;
            Network = rs.Network;
        }
    }

    public partial class SimulationImplementation
    {
        private readonly RenderState RS;
        private readonly AST.Network Network;
        private readonly DependencyGraph Graph;

        public SimulationImplementation(RenderState rs)
        {
            RS = rs;
            Network = rs.Network;
            Graph = new DependencyGraph(RS.Simulation.Processes.Select(x => x.Instance));
        }

        public AST.Process GetProcess(IProcess proc)
        {
            return Network.Processes.FirstOrDefault(x => x.SourceInstance.Instance == proc);
        }

        public AST.Bus GetBus(IBus bus)
        {
            return Network.Busses.First(x => x.SourceInstances.First() == bus);
        }

    }

}
