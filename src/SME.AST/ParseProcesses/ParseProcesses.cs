using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace SME.AST
{
    // This partial part of the parser deals with the Reflection side of the parsing.

    /// <summary>
    /// The main entry point for building an AST of a SME network.
    /// </summary>
    public partial class ParseProcesses
    {
        /// <summary>
        /// A class that contains intermediate information collected while traversing the network.
        /// </summary>
        protected class NetworkState : Network
        {
            /// <summary>
            /// The table of all the public constants in a convenient lookup table.
            /// </summary>
            public readonly Dictionary<Tuple<ProcessState, IFieldSymbol>, Constant> ConstantLookup = new Dictionary<Tuple<ProcessState, IFieldSymbol>, Constant>();
            /// <summary>
            /// A variable counter, used to make unique variable names.
            /// </summary>
            public int VariableCount;
            /// <summary>
            /// A lookup table of bus instances.
            /// </summary>
            public readonly Dictionary<IBus, Bus> BusInstanceLookup = new Dictionary<IBus, Bus>();
        }

        /// <summary>
        /// A class that contains intermediate information collected while traversion a process.
        /// </summary>
        protected class ProcessState : Process
        {
            /// <summary>
            /// The variables shared in the current process.
            /// </summary>
            public readonly Dictionary<string, Bus> BusInstances = new Dictionary<string, Bus>();
            /// <summary>
            /// The variables shared in the current process.
            /// </summary>
            public readonly Dictionary<string, Variable> Variables = new Dictionary<string, Variable>();
            /// <summary>
            /// The signals shared in the current process.
            /// </summary>
            public readonly Dictionary<string, Signal> Signals = new Dictionary<string, Signal>();
            /// <summary>
            /// The constants in the current process.
            /// </summary>
            public readonly Dictionary<string, Constant> Constants = new Dictionary<string, Constant>();
            /// <summary>
            /// The list of methods to process.
            /// </summary>
            public readonly Queue<Tuple<AST.Statement, MethodState, AST.InvocationExpression>> MethodTargets = new Queue<Tuple<AST.Statement, MethodState, AST.InvocationExpression>>();
            /// <summary>
            /// The compilation of the project.
            /// </summary>
            public Compilation compilation;
            /// <summary>
            /// The import statements.
            /// </summary>
            public readonly List<string> Imports = new List<string>();
            /// <summary>
            /// A flag indicating if the method should be decompiled.
            /// </summary>
            public bool Decompile;

            /// <summary>
            /// The map of generic parameters on this process.
            /// </summary>
            public readonly Dictionary<string, ITypeParameterSymbol> GenericMap = new Dictionary<string, ITypeParameterSymbol>();

            /// <summary>
            /// The map of generic types on this process.
            /// </summary>
            public readonly Dictionary<string, ITypeSymbol> GenericTypes = new Dictionary<string, ITypeSymbol>();

            /// <summary>
            /// Converts a generic type description into a non-generic version.
            /// </summary>
            /// <returns>The resolved type.</returns>
            /// <param name="ft">The type to resolve.</param>
            /// <param name="method">The method context, if any.</param>
            public ITypeSymbol ResolveGenericType(ITypeSymbol its, MethodState method = null)
            {
                if (its.TypeKind is TypeKind.Array)
                {
                    var elt = (its as IArrayTypeSymbol).ElementType;
                    if ((elt is INamedTypeSymbol && ((INamedTypeSymbol) elt).IsGenericType) || elt is ITypeParameterSymbol)
                    {
                        return m_compilation.CreateArrayTypeSymbol(GenericTypes[elt.Name]);
                    }
                    else
                    {
                        return its;
                    }
                }
                if (its.IsBusType())
                    return its;
                var ft = its as INamedTypeSymbol;
                if (ft != null && ft.IsGenericType)
                {
                    return this.GenericTypes[ft.Name];
                }
                return ft;
            }
        }

        /// <summary>
        /// A class that contains intermediate information collected while traversing a method.
        /// </summary>
        protected class MethodState : Method
        {
            /// <summary>
            /// List of all variables found in the method.
            /// </summary>
            public readonly List<Variable> CollectedVariables = new List<Variable>();

            /// <summary>
            /// The stack of scopes.
            /// </summary>
            public readonly List<KeyValuePair<ASTItem, Dictionary<string, Variable>>> Scopes = new List<KeyValuePair<ASTItem, Dictionary<string, Variable>>>();

            /// <summary>
            /// Initializes a new instance of the <see cref="T:SME.AST.ParseProcesses.MethodState"/> class.
            /// </summary>
            public MethodState()
            {
                Scopes.Add(new KeyValuePair<ASTItem, Dictionary<string, Variable>>(this, new Dictionary<string, Variable>()));
            }

            /// <summary>
            /// Adds a variable to the current scope.
            /// </summary>
            /// <param name="variable">The variable to add.</param>
            public Variable AddVariable(AST.Variable variable)
            {
                Scopes.Last().Value.Add(variable.Name, variable);
                CollectedVariables.Add(variable);
                return variable;
            }

            /// <summary>
            /// Starts a new local scope.
            /// </summary>
            /// <param name="scope">The item starting the scope.</param>
            public void StartScope(ASTItem scope)
            {
                if (scope == null)
                    throw new ArgumentNullException(nameof(scope));

                Scopes.Add(new KeyValuePair<ASTItem, Dictionary<string, Variable>>(scope, new Dictionary<string, Variable>()));
            }

            /// <summary>
            /// Closes the current local scope.
            /// </summary>
            /// <param name="scope">The scope to close.</param>
            public void FinishScope(ASTItem scope)
            {
                if (scope == null)
                    throw new ArgumentNullException(nameof(scope));

                if (Scopes.Last().Key != scope)
                    throw new Exception("PopScope had incorrect scope");

                if (scope is BlockStatement)
                    ((BlockStatement)scope).Variables = Scopes.Last().Value.Values.ToArray();

                Scopes.RemoveAt(Scopes.Count - 1);
            }

            /// <summary>
            /// Attempts to locate a variable with the given name, looking through all active scopes.
            /// </summary>
            /// <returns><c>true</c>, if the variable was found, <c>false</c> otherwise.</returns>
            /// <param name="name">The name of the variable to locate.</param>
            /// <param name="variable">The variable, if any.</param>
            public bool TryGetVariable(string name, out Variable variable)
            {
                variable = null;

                for (var i = Scopes.Count - 1; i >= 0; i--)
                    if (Scopes[i].Value.TryGetValue(name, out variable))
                        return true;

                return false;
            }
        }

        /// <summary>
        /// Static method for building an AST.
        /// </summary>
        /// <returns>The network.</returns>
        /// <param name="simulation">The simulation instance to build the AST for.</param>
        /// <param name="decompile">Set to <c>true</c> to enable decompilation of the IL code.</param>
        public static Network BuildNetwork(Simulation simulation, bool decompile = false)
        {
            return new ParseProcesses().Parse(simulation, decompile);
        }

        /// <summary>
        /// Static method for building an AST with a custom subclass.
        /// </summary>
        /// <returns>The network.</returns>
        /// <param name="simulation">The simulation instance to build the AST for.</param>
        /// <param name="decompile">Set to <c>true</c> to enable decompilation of the IL code.</param>
        public static Network BuildNetwork<T>(Simulation simulation, bool decompile = false)
            where T : ParseProcesses, new()
        {
            return new T().Parse(simulation, decompile);
        }


        /// <summary>
        /// Builds an AST by inspecting the processes.
        /// </summary>
        /// <param name="simulation">The simulation instance to build the AST for.</param>
        /// <param name="decompile">Set to <c>true</c> to enable decompilation of the IL code.</param>
        public virtual Network Parse(Simulation simulation, bool decompile = false)
        {
            // Get the path to the .csproj file
            var sourceasm = simulation.Processes.First().Instance.GetType().Assembly;
            var project_path = Simulation.ProjectPath;
            if (project_path == null)
            {
                var folder_path = Directory.GetParent(sourceasm.Location).FullName;
                project_path = GetCSProjInParents(folder_path);
            }

            // Build the project
            // runtime will throw an exception, if MSBuild assemblies are loaded multiple times during runtime.
            try
            {
                var instance = Microsoft.Build.Locator.MSBuildLocator.RegisterDefaults();
                // workaround from https://github.com/microsoft/MSBuildLocator/issues/86
                System.Runtime.Loader.AssemblyLoadContext.Default.Resolving += (assemblyLoadContext, assemblyName) =>
                {
                    var path = Path.Combine(instance.MSBuildPath, assemblyName.Name + ".dll");
                    if (File.Exists(path))
                    {
                        return assemblyLoadContext.LoadFromAssemblyPath(path);
                    }
                    return null;
                };
            }
            catch (Exception) { }
            var workspace = MSBuildWorkspace.Create();
            var proj_task = workspace.OpenProjectAsync(project_path);
            proj_task.Wait();
            if (workspace.Diagnostics.Count > 0) {
                var messages = workspace.Diagnostics.Select(x => x.Message);
                var message_string = string.Join(Environment.NewLine, messages);
                throw new Exception($"{workspace.Diagnostics.Count} errors when opening the workspace.{Environment.NewLine}{message_string}");
            }
            var project = proj_task.Result;
            var compile_task = project.GetCompilationAsync();
            compile_task.Wait();
            m_compilation = compile_task.Result;
            var diags = m_compilation.GetDiagnostics();
            foreach (var diag in diags)
                if (diag.WarningLevel == 0)
                    throw new Exception($"Error when compiling the project: {diag.GetMessage()}");
            m_syntaxtrees = m_compilation.SyntaxTrees;
            m_semantics = m_compilation.SyntaxTrees.Select(x => m_compilation.GetSemanticModel(x));

            var network = new NetworkState()
            {
                Name = sourceasm.GetName().Name,
                Parent = null,
                compilation = m_compilation
            };

            network.Processes = simulation.Processes
                .Where(n =>
                       n.Instance.GetType().GetCustomAttributes(typeof(IgnoreAttribute), true).FirstOrDefault() == null
                            &&
                       !(n.Instance is SimulationProcess))
                .Select(x => Parse(network, x, simulation))
                .ToArray();

            if (network.Processes.Length == 0)
                throw new Exception("No processes were found in the list");

            network.Busses = network.Processes.SelectMany(x => x.InternalBusses.Concat(x.InputBusses).Concat(x.OutputBusses)).Distinct().ToArray();
            network.Constants = network.ConstantLookup.Values.ToArray();

            if (decompile)
            {
                foreach (var pr in network.Processes.Cast<ProcessState>().Where(x => x.Decompile))
                {
                    if (pr.SourceInstance.Instance is SimpleProcess)
                    {

                        var st = pr.SourceType;
                        var method = st.GetMethod("OnTick", BindingFlags.Instance | BindingFlags.NonPublic, null, CallingConventions.Any, new Type[] { }, null);
                        if (method == null)
                            throw new Exception($"Could not find the \"OnTick\" method in class: {st.FullName}");

                        Decompile(network, pr, method);
                    }
                    else if (pr.SourceInstance.Instance is StateProcess)
                    {
                        var st = pr.SourceType;
                        var method = st.GetMethod("OnTickAsync", BindingFlags.Instance | BindingFlags.NonPublic, null, CallingConventions.Any, new Type[] { }, null);
                        if (method == null)
                            throw new Exception($"Could not find the \"OnTickAsync\" method in class: {st.FullName}");

                        Decompile(network, pr, method);
                    }
                    else
                        throw new Exception("Unexpected decompile flag on unsupported process type");
                }
            }
            network.Constants = network.ConstantLookup.Values.ToArray();

            // Patch up all types if they are missing
            foreach (var el in network.All().OfType<DataElement>().Where(x => x.MSCAType == null))
                el.MSCAType = LoadType(el.Type);

            foreach (var el in network.All().OfType<DataElement>().Where(x => x.DefaultValue == null))
            {
                if (new[] { typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong) }
                        .Any(x => Type.GetType(el.MSCAType.ToDisplayString()) == x))
                    el.DefaultValue = 0;
                else if (Type.GetType(el.MSCAType.ToDisplayString()) == typeof(bool))
                    el.DefaultValue = false;
            }

            // Assign bus names, processes are handled elsewhere
            foreach (var el in network.All().OfType<AST.Bus>())
            {
                foreach (var src in el.SourceInstances)
                    if (simulation.BusNames.ContainsKey(src))
                        el.InstanceName = simulation.BusNames[src];
            }

            return network;
        }

        /// <summary>
        /// Recursively check folders parents until a *.csproj file is found.
        /// </summary>
        /// <param name="folder_path">The folder to check in.</param>
        private static string GetCSProjInParents(string folder_path)
        {
            var csprojs = Directory.GetFiles(folder_path, "*.csproj");
            if (csprojs.Any())
                return csprojs.First();
            else
                return GetCSProjInParents(Directory.GetParent(folder_path).FullName);
        }

        /// <summary>
        /// Attempts to remove redundant prefixes to names.
        /// </summary>
        /// <returns>The name without the prefix.</returns>
        /// <param name="network">The top-level network instance.</param>
        /// <param name="fullname">The name to shorten.</param>
        /// <param name="sourcetype">The type of the source process.</param>
        protected virtual string NameWithoutPrefix(NetworkState network, string fullname, Type sourcetype)
        {
            var extras = string.Empty;
            if (sourcetype.IsGenericType)
            {
                fullname = sourcetype.GetGenericTypeDefinition().FullName;
                extras = "<" + string.Join(", ", sourcetype.GenericTypeArguments.Select(x => x.Name)) + ">";
            }

            if (fullname.StartsWith(network.Name + ".", StringComparison.Ordinal) && fullname.Length > network.Name.Length - 1)
                fullname = fullname.Substring(network.Name.Length + 1);

            return fullname + extras;
        }

        /// <summary>
        /// Parses a process and builds an AST component for it.
        /// </summary>
        /// <param name="network">The top-level network.</param>
        /// <param name="process">The process to build the AST for.</param>
        /// <param name="simulation">The current simulation context.</param>
        protected virtual Process Parse(NetworkState network, ProcessMetadata process, Simulation simulation)
        {
            var st = process.Instance.GetType();

            var inputbusses = process.Instance.InputBusses.Union(process.Instance.ClockedInputBusses).ToArray();
            var outputbusses = process.Instance.OutputBusses.Distinct().ToArray();

            var res = new ProcessState()
            {
                Name = NameWithoutPrefix(network, st.FullName, st),
                SourceType = st,
                MSCAType = LoadType(st),
                SourceInstance = process,
                InstanceName = process.InstanceName,
                Parent = network,
                IsSimulation = process.Instance is SimulationProcess,
                IsClocked = st.GetCustomAttribute<ClockedProcessAttribute>() != null,
                Decompile =
                    (
                        typeof(SimpleProcess).IsAssignableFrom(st)
                        ||
                        typeof(StateProcess).IsAssignableFrom(st)
                    )
                    &&
                    !st.HasAttribute<SuppressOutputAttribute>()
                    &&
                    !st.HasAttribute<SuppressBodyAttribute>(),
                compilation = network.compilation
            };

            var proctype = res.MSCAType as INamedTypeSymbol;

            if (proctype.IsGenericType)
            {
                var gennames = proctype.TypeParameters.Select(x => x.Name).ToArray();
                var gentypes = LoadGenericTypes(st).ToArray();
                if (gennames.Length != gentypes.Length)
                    throw new Exception("Error when mapping generic parameters to instance types.");
                for (int i = 0; i < gennames.Length; i++)
                    res.GenericTypes[gennames[i]] = gentypes[i];
            }

            res.InputBusses = inputbusses.Select(x => Parse(network, res, x, simulation)).ToArray();
            res.OutputBusses = outputbusses.Select(x => Parse(network, res, x, simulation)).ToArray();
            res.InternalBusses = process.Instance.InternalBusses.Select(x => Parse(network, res, x,simulation)).ToArray();
            foreach (var ib in res.InternalBusses)
                ib.IsInternal = true;

            // Set up the local names by finding the field that holds the instance reference
            foreach(var b in res.InputBusses.Union(res.OutputBusses).Union(res.InternalBusses))
            {
                var f = st
                    .GetFields(
                        BindingFlags.Instance |
                        BindingFlags.FlattenHierarchy |
                        BindingFlags.Public |
                        BindingFlags.NonPublic)
                    .FirstOrDefault(x =>
                        {
                            var bus = x.GetValue(process.Instance);
                            var bust = bus.GetType();
                            if (!(bus is IBus || (bust.IsArray && (bust.GetElementType().GetInterfaces().Any(y => y == typeof(IBus))))))
                                return false;
                            var busarr = bust.IsArray ? bus as IBus[] : new[] { bus as IBus };
                            return busarr.Zip(b.SourceInstances)
                                .All(y => y.First == y.Second);
                        });
                if (f != null)
                    res.LocalBusNames[b] = f.Name;
            }

            if (res.Decompile)
            {
                var Fields = proctype.GetMembers()
                        .OfType<IFieldSymbol>()
                        .Where(x => !x.GetAttributes<IgnoreAttribute>()
                        .Any());
                while (!proctype.BaseType.ToDisplayString().StartsWith("SME"))
                {
                    proctype = proctype.BaseType;
                    Fields = Fields.Union(proctype
                        .GetMembers()
                        .OfType<IFieldSymbol>()
                        .Where(x => !x
                            .GetAttributes<IgnoreAttribute>()
                            .Any()
                        ), (IEqualityComparer<IFieldSymbol>) SymbolEqualityComparer.Default);
                }
                // Register all variables
                foreach (var f in Fields)
                {
                    var ft = res.ResolveGenericType(f.Type);

                    if (ft.IsBusType())
                        RegisterBusReference(network, res, f);
                    else
                        RegisterVariable(network, res, f);
                }
            }

            foreach (var f in process.Initialization)
            {
                if (f.Value == null)
                    continue;

                Variable v;
                if (res.Variables.TryGetValue(f.Key, out v))
                    SetDataElementDefaultValue(network, res, v, f.Value, false);
            }

            res.SharedSignals = res.Signals.Values.ToArray();
            res.SharedVariables = res.Variables.Values.ToArray();
            res.SharedConstants = res.Constants.Values.ToArray();
            res.InternalDataElements = new DataElement[0];
            return res;
        }

        /// <summary>
        /// Parses a bus and builds an AST component for it.
        /// </summary>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the bus is located.</param>
        /// <param name="bus">The bus to build the AST for.</param>
        /// <param name="simulation">The simulation the AST is built for.</param>
        protected virtual Bus Parse(NetworkState network, ProcessState proc, IBus[] bus, Simulation simulation)
        {
            var bu = bus.FirstOrDefault();
            var st = ((IRuntimeBus)bu).BusType;

            if (network.BusInstanceLookup.ContainsKey(bu))
                return network.BusInstanceLookup[bu];

            var res = new Bus()
            {
                Name = NameWithoutPrefix(network, st.FullName, st),
                SourceType = st,
                SourceInstances = bus,
                IsTopLevelInput = bus.Any(x => simulation.TopLevelInputBusses.Contains(x)),
                IsTopLevelOutput = bus.Any(x => simulation.TopLevelOutputBusses.Contains(x)),
                IsClocked = st.HasAttribute<ClockedBusAttribute>(),
                IsInternal = st.HasAttribute<InternalBusAttribute>(),
                Parent = network,
                MSCAType = bus.Length > 1 ? LoadType(bus.GetType()) : LoadType(st)
            };

            // TODO currently working on the first element of the bus array, as arrays and dictionaries are a bad match.
            foreach (var b in bus)
                network.BusInstanceLookup[b] = res;
            res.Signals = st.GetPropertiesRecursive().Where(x => x.DeclaringType != typeof(IBus)).Select(x => Parse(network, proc, res, x)).ToArray();
            return res;
        }

        /// <summary>
        /// Parses the bus property and builds an AST component for it.
        /// </summary>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the bus is located.</param>
        /// <param name="bus">The bus element.</param>
        /// <param name="pi">The property to build the AST for.</param>
        protected virtual BusSignal Parse(NetworkState network, ProcessState proc, Bus bus, PropertyInfo pi)
        {
            var st = pi.PropertyType;
            object defaultvalue =
                    pi.PropertyType.IsValueType
                      ? Activator.CreateInstance(pi.PropertyType)
                      : null;

            var initvalattr = pi.GetCustomAttribute(typeof(InitialValueAttribute), true) as InitialValueAttribute;

            if (initvalattr != null && initvalattr.Value != null)
                defaultvalue = Convert.ChangeType(initvalattr.Value, pi.PropertyType);

            return new BusSignal()
            {
                Name = pi.Name,
                Type = pi.PropertyType,
                Source = pi,
                Parent = bus,
                DefaultValue = defaultvalue,
                MSCAType = LoadType(pi.PropertyType)
            };
        }

        /// <summary>
        /// Parses the specified parameter and builds an AST component for it.
        /// </summary>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method that the parameter belongs to.</param>
        /// <param name="p">The parameter to build an AST for.</param>
        protected virtual Parameter Parse(NetworkState network, ProcessState proc, Method method, ParameterInfo p)
        {
            return new Parameter()
            {
                Name = p.Name,
                Source = p,
                Type = p.ParameterType,
                Parent = method
            };
        }

        /// <summary>
        /// Parse the specified local variable and builds and AST component for it.
        /// </summary>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method that the parameter belongs to.</param>
        /// <param name="lv">The local variable to parse.</param>
        protected virtual Variable Parse(NetworkState network, ProcessState proc, Method method, LocalVariableInfo lv)
        {
            return new Variable()
            {
                Name = "local_" + lv.LocalIndex,
                Source = lv,
                Type = lv.LocalType,
                Parent = method
            };
        }
    }
}
