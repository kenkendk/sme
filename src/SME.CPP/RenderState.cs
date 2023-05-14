using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SME.AST;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SME.CPP
{
    public class RenderState
    {
        /// <summary>
        /// A type reference comparer, used to compare type references loaded from different contexts
        /// </summary>
        private class TypeRefComp : IEqualityComparer<ITypeSymbol>
        {
            /// <summary>
            /// Returns a value indicating if x is equal to y.
            /// </summary>
            /// <returns><c>True</c> if x is equal to y, <c>false</c> otherwise.</returns>
            /// <param name="x">The x value.</param>
            /// <param name="y">The y value.</param>
            public bool Equals(ITypeSymbol x, ITypeSymbol y)
            { return x.ToDisplayString() == y.ToDisplayString(); }

            /// <summary>
            /// Gets the hash code of an object.
            /// </summary>
            /// <returns>The hash code.</returns>
            /// <param name="obj">The item to get the hash code for.</param>
            public int GetHashCode(ITypeSymbol obj)
            { return obj.ToDisplayString().GetHashCode(); }
        }

        /// <summary>
        /// The network being rendered
        /// </summary>
        public readonly Network Network;

        /// <summary>
        /// The simulation forming the basis of the network
        /// </summary>
        public readonly Simulation Simulation;

        /// <summary>
        /// The folder where data is place
        /// </summary>
        public readonly string TargetFolder;
        /// <summary>
        /// The folder where backups are stored
        /// </summary>
        public readonly string BackupFolder;
        /// <summary>
        /// The name of the file where a CSV trace is stored
        /// </summary>
        public readonly string CSVTracename;

        /// <summary>
        /// Sequence of custom CPP files to include in the compilation
        /// </summary>
        public readonly IEnumerable<string> CustomFiles;

        /// <summary>
        /// The unique types found in the network
        /// </summary>
        public readonly ITypeSymbol[] Types;

        /// <summary>
        /// The type scope
        /// </summary>
        public readonly CppTypeScope TypeScope;

        /// <summary>
        /// The renderer
        /// </summary>
        public readonly RenderHandler Renderer;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SME.VHDL.RenderState"/> class.
        /// </summary>
        /// <param name="simulation">The simulation to parse.</param>
        /// <param name="targetfolder">The folder where the output is stored.</param>
        /// <param name="backupfolder">The folder where backups are stored.</param>
        /// <param name="csvtracename">The name of the CSV trace file.</param>
        /// <param name="customfiles">A list of VHDL files to include in the Makefile, without the VHDL extension</param>
        public RenderState(Simulation simulation, string targetfolder, string backupfolder = null, string csvtracename = null, IEnumerable<string> customfiles = null)
        {
            Simulation = simulation;
            TargetFolder = targetfolder;
            BackupFolder = backupfolder;
            CSVTracename = csvtracename;
            CustomFiles = customfiles;

            Network = ParseProcesses.BuildNetwork(simulation, true);

            ValidateNetwork(Network);

            TypeScope = new CppTypeScope(Network.Processes.First(x => x.MainMethod != null).MainMethod.MSCAReturnType.ContainingAssembly);

            Types = Network
                .All()
                .OfType<DataElement>()
                .Select(x => x.MSCAType)
                .Distinct((IEqualityComparer<ITypeSymbol>) SymbolEqualityComparer.Default)
                .ToArray();

            Network.Name = Naming.ToValidName(simulation.Processes.First().Instance.GetType().Assembly.GetName().Name);

            SME.AST.Transform.Apply.Transform(
                Network,
                new SME.AST.Transform.IASTTransform[] {
                    new Transformations.AssignNames(),
                },
                m => new SME.AST.Transform.IASTTransform[] {

                },
                m => new SME.AST.Transform.IASTTransform[] {
                    new SME.AST.Transform.WrapIfComposite()
                },
                m => new SME.AST.Transform.IASTTransform[] {
                    new SME.AST.Transform.RemoveExtraParenthesis()
                }
            );

            Renderer = new RenderHandler(TypeScope);
        }

        /// <summary>
        /// Render this instance.
        /// </summary>
        public void Render()
        {
            BackupExistingTarget(TargetFolder, BackupFolder);

            var targetTopLevel = Path.Combine(TargetFolder, Path.ChangeExtension(Naming.AssemblyNameToFileName(Network), ".cpp"));
            File.WriteAllText(targetTopLevel, MergeUserData(new Templates.TopLevel(this).TransformText(), targetTopLevel));

            var targetDefinitions = Path.Combine(TargetFolder, Naming.BusDefinitionsFileName(Network));
            File.WriteAllText(targetDefinitions, MergeUserData(new Templates.BusDefinitions(this).TransformText(), targetDefinitions));

            var busImplementations = Path.Combine(TargetFolder, Path.ChangeExtension(Naming.BusImplementationsFileName(Network), ".cpp"));
            File.WriteAllText(busImplementations, MergeUserData(new Templates.BusImplementations(this).TransformText(), busImplementations));

            var sharedDefinitions = Path.Combine(TargetFolder, Naming.SharedDefinitionsFileName(Network));
            File.WriteAllText(sharedDefinitions, MergeUserData(new Templates.SharedTypes(this).TransformText(), sharedDefinitions));

            var simulatorHeader = Path.Combine(TargetFolder, Path.ChangeExtension(Naming.SimulatorFileName(Network), ".hpp"));
            File.WriteAllText(simulatorHeader, MergeUserData(new Templates.SimulationHeader(this).TransformText(), simulatorHeader));

            var simulatorImplementation = Path.Combine(TargetFolder, Path.ChangeExtension(Naming.SimulatorFileName(Network), ".cpp"));
            File.WriteAllText(simulatorImplementation, MergeUserData(new Templates.SimulationImplementation(this).TransformText(), simulatorImplementation));

            var makeFileTarget = Path.Combine(TargetFolder, "Makefile");
            File.WriteAllText(makeFileTarget, MergeUserData(new Templates.Makefile(this).TransformText(), makeFileTarget));

            var duplicates = new HashSet<Type>();
            foreach (var p in Network.Processes)
            {
                if (duplicates.Contains(p.SourceType))
                    continue;
                duplicates.Add(p.SourceType);

                Renderer.Process = p;
                var rsp = new RenderStateProcess(this, p);
                var targetheaderfile = Path.Combine(TargetFolder, p.Name + ".hpp");
                File.WriteAllText(targetheaderfile, MergeUserData(new Templates.ProcessHeader(this, rsp).TransformText(), targetheaderfile));

                var targetfile = Path.Combine(TargetFolder, Naming.ProcessNameToFileName(p));
                File.WriteAllText(targetfile, MergeUserData(new Templates.ProcessItem(this, rsp).TransformText(), targetfile));
                Renderer.Process = null;
            }

            var names = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames();

            foreach (
                var cppfile in from f in System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames()
                                where
                                (
                                    f.EndsWith(".cpp", StringComparison.InvariantCultureIgnoreCase)
                                    ||
                                     f.EndsWith(".hpp", StringComparison.InvariantCultureIgnoreCase)
                                )
                                &&
                                f.StartsWith(typeof(Templates.TopLevel).Namespace + ".", StringComparison.InvariantCultureIgnoreCase)
                                select f)
                using (var rs = new System.IO.StreamReader(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(cppfile)))
                    File.WriteAllText(Path.Combine(TargetFolder, cppfile.Substring(typeof(Templates.TopLevel).Namespace.Length + 1)), rs.ReadToEnd());

        }


        /// <summary>
        /// Performs some checks to see if the network uses features that are not supported by the CPP render
        /// </summary>
        /// <param name="network">The network to validate.</param>
        private static void ValidateNetwork(AST.Network network)
        {
            var sp = network.Processes.SelectMany(x => x.InternalBusses).FirstOrDefault(x => x.IsTopLevelInput || x.IsTopLevelOutput);
            if (sp != null)
                throw new Exception($"Cannot have an internal bus that is also toplevel input or output: {sp.Name}");
        }

        /// <summary>
        /// Makes a backup of all target files
        /// </summary>
        /// <param name="targetfolder">The folder where output is stored.</param>
        /// <param name="backupfolder">The folder where backups are stored.</param>
        private void BackupExistingTarget(string targetfolder, string backupfolder)
        {
            backupfolder = backupfolder ?? targetfolder;

            if (Directory.Exists(targetfolder))
            {
                var backupname = Path.Combine(Path.GetDirectoryName(backupfolder), Path.GetFileName(backupfolder) + "-backup-" + DateTime.Now.ToString("yyyyMMddTHHmmss"));
                while (Directory.Exists(backupname))
                {
                    System.Threading.Thread.Sleep(TimeSpan.FromSeconds(1.5));
                    backupname = Path.Combine(Path.GetDirectoryName(backupfolder), Path.GetFileName(backupfolder) + "-backup-" + DateTime.Now.ToString("yyyyMMddTHHmmss"));
                }

                Directory.CreateDirectory(backupname);

                foreach (var fn in new[] {
                    Path.ChangeExtension(Naming.AssemblyNameToFileName(Network), ".cpp"),
                    Naming.SharedDefinitionsFileName(Network),
                    Naming.BusDefinitionsFileName(Network),
                    Path.ChangeExtension(Naming.SimulatorFileName(Network), ".cpp"),
                    Path.ChangeExtension(Naming.SimulatorFileName(Network), ".hpp"),
                    Path.ChangeExtension(Naming.BusImplementationsFileName(Network), ".cpp") })
                {
                    var s = Path.Combine(targetfolder, fn);
                    if (File.Exists(s))
                        File.Copy(s, Path.Combine(backupname, fn));
                }

                var duplicates = new HashSet<string>();
                foreach (var p in Network.Processes)
                {
                    var source = Path.Combine(targetfolder, Naming.ProcessNameToFileName(p));
                    if (duplicates.Contains(source))
                        continue;
                    duplicates.Add(source);
                    if (File.Exists(source))
                        File.Copy(source, Path.Combine(backupname, Naming.ProcessNameToFileName(p)));
                }
            }
            else
            {
                Directory.CreateDirectory(targetfolder);
            }
        }

        /// <summary>
        /// Returns the name written in the trace file for a given signal
        /// </summary>
        /// <returns>The signal name as written by the tracer.</returns>
        /// <param name="s">The signal to find the name for.</param>
        public string TestBenchSignalName(BusSignal s)
        {
            var bus = (AST.Bus)s.Parent;
            var st = bus.SourceType;
            var bi = bus.SourceInstances.First();
            if (bi != null && Simulation.BusNames.ContainsKey(bi))
                return Simulation.BusNames[bi] + "." + s.Name;

            var name = st.Name + "." + s.Name;
            if (st.DeclaringType != null)
                name = st.DeclaringType.Name + "." + name;

            return name;
        }


        /// <summary>
        /// Gets all enum types in the network
        /// </summary>
        public IEnumerable<ITypeSymbol> EnumTypes
        {
            get
            {
                return Types
                    .Where(x => x.IsEnum())
                    .Select(x => x);
            }
        }

        /// <summary>
        /// Gets all enum types in the network
        /// </summary>
        public IEnumerable<ITypeSymbol> StructTypes
        {
            get
            {
                return Types
                    .Where(x =>
                    {
                        var tr = x;
                        return !tr.IsEnum() && tr.IsValueType && tr.SpecialType == SpecialType.None && !tr.IsSameTypeReference(typeof(void));
                    })
                    .Select(x => x);
            }
        }

        /// <summary>
        /// Lists all members for a given value type
        /// </summary>
        /// <returns>The member VHDL strings.</returns>
        /// <param name="type">The VHDL type.</param>
        public IEnumerable<string> ListMembers(ITypeSymbol type)
        {
            if (type.IsEnum())
            {
                foreach (var e in type.GetMembers().OfType<IFieldSymbol>().Where(x => x.Name != "value__"))
                    yield return e.Name;
            }
            else if (type.IsValueType && type.SpecialType == SpecialType.None)
            {
                foreach (var m in type.GetMembers().OfType<IFieldSymbol>())
                    if (!m.IsStatic)
                        yield return string.Format("{0} {1}", TypeScope.GetType(m.Type), Naming.ToValidName(m.Name));
            }
        }

        /// <summary>
        /// Gets all constant definition strings
        /// </summary>
        public IEnumerable<string> Constants
        {
            get
            {
                foreach (var n in Network.Constants)
                {
                    object nx = n.DefaultValue;

                    if (nx is ArrayCreationExpressionSyntax)
                    {
                        var arc = nx as ArrayCreationExpressionSyntax;
                        var eltype = n.MSCAType.GetArrayElementType();
                        var cpptype = TypeScope.GetType(n);

                        string values;
                        if (new[] { typeof(sbyte), typeof(byte), typeof(ushort), typeof(short), typeof(int), typeof(uint), typeof(long), typeof(ulong) }.Select(x => eltype.LoadType(x)).Contains(eltype, (IEqualityComparer<ITypeSymbol>) SymbolEqualityComparer.Default))
                            values = string.Join(", ", arc.Initializer.Expressions.Select(x => string.Format("{0}", x)));
                        else
                            throw new Exception("Unexpected initializer type");

                        yield return string.Format("const {0} {1}[{2}] = {{ {3} }}", cpptype.ElementName, n.Name, arc.Initializer.Expressions.Count, values);
                    }
                    else if (nx is AST.ArrayCreateExpression)
                    {
                        var arc = nx as AST.ArrayCreateExpression;
                        var eltype = n.MSCAType.GetArrayElementType();
                        var cpptype = TypeScope.GetType(n);

                        string values;
                        if (new[] { typeof(sbyte), typeof(byte), typeof(ushort), typeof(short), typeof(int), typeof(uint), typeof(long), typeof(ulong) }.Select(x => eltype.LoadType(x)).Contains(eltype, (IEqualityComparer<ITypeSymbol>) SymbolEqualityComparer.Default))
                            values = string.Join(", ", arc.ElementExpressions.Select(x => Renderer.RenderExpression(x)));
                        else
                            throw new Exception("Unexpected initializer type");

                        yield return string.Format("const {0} {1}[{2}] = {{ {3} }}", cpptype.ElementName, n.Name, arc.ElementExpressions.Length, values);

                    }
                    else if (nx is AST.EmptyArrayCreateExpression)
                    {
                        var arc = nx as AST.EmptyArrayCreateExpression;
                        var cpptype = TypeScope.GetType(n);
                        yield return $"const {cpptype.ElementName} {n.Name}[{Renderer.RenderExpression(((EmptyArrayCreateExpression)nx).SizeExpression)}]()";
                    }
                    else if (nx is Array)
                    {
                        var arc = nx as Array;
                        var eltype = n.MSCAType.GetArrayElementType();
                        var cpptype = TypeScope.GetType(n);

                        string values;
                        if (new[] { typeof(sbyte), typeof(byte), typeof(ushort), typeof(short), typeof(int), typeof(uint), typeof(long), typeof(ulong) }.Select(x => eltype.LoadType(x)).Contains(eltype, (IEqualityComparer<ITypeSymbol>) SymbolEqualityComparer.Default))
                            values = string.Join(", ", Enumerable.Range(0, arc.GetLength(0)).Select(x => arc.GetValue(x).ToString()));
                        else
                            throw new Exception("Unexpected initializer type");

                        yield return string.Format("const {0} {1}[{2}] = {{ {3} }}", cpptype.ElementName, n.Name, arc.GetLength(0), values);

                    }
                    else if (nx != null)
                    {
                        var cpptype = TypeScope.GetType(n);
                        yield return $"const {cpptype.Name} {n.Name} = {nx}";
                    }
                    else
                    {
                        yield return $"const {n.Name} = 0";
                    }
                }
            }
        }
        /// <summary>
        /// Returns all signals from top-level input busses
        /// </summary>
        public IEnumerable<BusSignal> DriverSignals
        {
            get
            {
                return Network.Busses
                              .Where(x => x.IsTopLevelInput)
                              .SelectMany(x => x.Signals)
                              .OrderBy(x => TestBenchSignalName(x));
            }
        }

        /// <summary>
        /// Returns all signals from non-top-level input busses
        /// </summary>
        public IEnumerable<BusSignal> VerifySignals
        {
            get
            {
                return Network.Busses
                              .Where(x => !x.IsTopLevelInput && !x.IsInternal)
                              .SelectMany(x => x.Signals)
                              .OrderBy(x => TestBenchSignalName(x));
            }
        }

        /// <summary>
        /// Merges a newly rendered file with user supplied data from an existing file
        /// </summary>
        /// <returns>The user data.</returns>
        /// <param name="text">The newly rendered content.</param>
        /// <param name="targetfile">The file to write.</param>
        private string MergeUserData(string text, string targetfile)
        {
            if (File.Exists(targetfile))
            {
                var core = @"[ \t]*//[ \t]+####[ \t]+USER-DATA-{0}-{1}[ \t]*(\r|\r\n|\n)";

                var rxfind = new Regex(string.Format(core, "(?<groupname>[A-Z]+)", "START"));
                var inputgroups = (from n in rxfind.Matches(text).Cast<Match>() select n.Groups["groupname"].Value).ToArray();

                var targettext = File.ReadAllText(targetfile);
                if (string.IsNullOrWhiteSpace(targettext))
                    return text;

                var targetgroups = (from n in rxfind.Matches(targettext).Cast<Match>() select n.Groups["groupname"].Value).ToArray();

                if (targetgroups.Length != inputgroups.Length)
                    throw new Exception(string.Format("The file {0} does not match the template!", targetfile));

                if (targetgroups.Distinct().Count() != targetgroups.Length)
                    throw new Exception(string.Format("The file {0} has duplicate user regions!", targetfile));

                if (inputgroups.Distinct().Count() != inputgroups.Length)
                    throw new Exception(string.Format("The source template for {0} has duplicate user regions!", targetfile));

                if (!targetgroups.All(n => inputgroups.Contains(n)))
                    throw new Exception(string.Format("The file {0} does not match the template!", targetfile));

                foreach (var g in inputgroups)
                {
                    var rxsource = new Regex(string.Format("(?<startexp>" + core + ")", g, "START") + "(?<content>.*)" + string.Format("(?<endexp>" + core + ")", g, "END"), RegexOptions.Singleline);
                    var sourcematch = rxsource.Matches(targettext);
                    if (sourcematch == null)
                        continue;

                    if (sourcematch.Count > 1)
                        throw new Exception("Multiple regions found in previous file for -- #### USER-DATA-{0}");

                    if (sourcematch.Count > 0)
                    {
                        var userdata = sourcematch[0].Groups["content"].Value;

                        text = rxsource.Replace(text, m => m.Groups["startexp"].Value + userdata + m.Groups["endexp"].Value);
                    }
                }
            }

            return text;
        }

        /// <summary>
        /// Finds the length of an array type-signal
        /// </summary>
        /// <returns>The array length.</returns>
        /// <param name="element">The element to get the length for.</param>
        public AST.Constant GetArrayLength(AST.DataElement element)
        {
            if (element.MSCAType.IsFixedArrayType())
            {
                if (element.Source is IFieldSymbol)
                {
                    return new Constant
                    {
                        Source = element,
                        DefaultValue = ((IFieldSymbol)element.Source).Type.GetFixedArrayLength(),
                        MSCAType = element.MSCAType.LoadType(typeof(uint))
                    };
                }
                else if (element.Source is System.Reflection.MemberInfo)
                {
                    return new Constant
                    {
                        Source = element,
                        DefaultValue = ((System.Reflection.MemberInfo)element.Source).GetFixedArrayLength(),
                        MSCAType = element.MSCAType.LoadType(typeof(uint))
                    };
                }
            }

            if (element.DefaultValue is Array)
            {
                return new Constant()
                {
                    Source = element,
                    DefaultValue = (element.DefaultValue as Array).Length,
                    MSCAType = element.MSCAType.LoadType(typeof(uint))
                };
            }

            throw new Exception($"Unable to guess length for signal: {element.Name}");

        }

    }
}
