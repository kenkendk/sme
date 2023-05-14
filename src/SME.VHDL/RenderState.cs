using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SME.AST;
using SME.VHDL.Templates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SME.VHDL
{
    /// <summary>
    /// Class to encapsulate the state associated with rendering an AST network as VHDL files.
    /// </summary>
    public class RenderState
    {
        /* TODO double check if this is needed
        /// <summary>
        /// A type reference comparer, used to compare type references loaded from different contexts
        /// </summary>
        private class TypeRefComp : IEqualityComparer<TypeReference>
        {
            /// <summary>
            /// Returns a value indicating if x is equal to y.
            /// </summary>
            /// <returns><c>True</c> if x is equal to y, <c>false</c> otherwise.</returns>
            /// <param name="x">The x value.</param>
            /// <param name="y">The y value.</param>
            public bool Equals(TypeReference x, TypeReference y)
            { return x.FullName == y.FullName; }

            /// <summary>
            /// Gets the hash code of an object.
            /// </summary>
            /// <returns>The hash code.</returns>
            /// <param name="obj">The item to get the hash code for.</param>
            public int GetHashCode(TypeReference obj)
            { return obj.FullName.GetHashCode(); }
        }*/

        /// <summary>
        /// The network being rendered.
        /// </summary>
        public readonly Network Network;

        /// <summary>
        /// The render configuration.
        /// </summary>
        public readonly RenderConfig Config;

        /// <summary>
        /// The simulation forming the basis of the network.
        /// </summary>
        public readonly Simulation Simulation;

        /// <summary>
        /// The folder where data is place.
        /// </summary>
        public readonly string TargetFolder;
        /// <summary>
        /// The folder where backups are stored.
        /// </summary>
        public readonly string BackupFolder;
        /// <summary>
        /// The name of the file where a CSV trace is stored.
        /// </summary>
        public readonly string CSVTracename;

        /// <summary>
        /// Sequence of custom VHDL files to include in the compilation.
        /// </summary>
        public readonly IEnumerable<string> CustomFiles;

        /// <summary>
        /// The unique types found in the network.
        /// </summary>
        public readonly ITypeSymbol[] Types;

        /// <summary>
        /// A lookup associating an AST node with a VHDL type.
        /// </summary>
        public readonly Dictionary<ASTItem, VHDLType> TypeLookup = new Dictionary<ASTItem, VHDLType>();

        /// <summary>
        /// The list of registered temporary variables.
        /// </summary>
        public readonly Dictionary<Method, Dictionary<string, Variable>> TemporaryVariables = new Dictionary<Method, Dictionary<string, Variable>>();

        /// <summary>
        /// The table with all custom defined enums.
        /// </summary>
        public readonly Dictionary<VHDLType, Dictionary<string, object>> CustomEnumValues = new Dictionary<VHDL.VHDLType, Dictionary<string, object>>();

        /// <summary>
        /// The fully custom renderers to use.
        /// </summary>
        public readonly Dictionary<Type, IFullCustomRenderer> FullCustomRenderers = new Dictionary<Type, IFullCustomRenderer>();

        /// <summary>
        /// The custom renderers to use.
        /// </summary>
        public readonly Dictionary<Type, ICustomRenderer> CustomRenderers = new Dictionary<Type, ICustomRenderer>();

        /// <summary>
        /// The type scope used to resolve VHDL types.
        /// </summary>
        public readonly VHDLTypeScope TypeScope;

        /// <summary>
        /// Gets the length of a clock pulse period.
        /// </summary>
        public int ClockPulseLength { get { return ClockLength / 2; } }
        /// <summary>
        /// Gets or sets the length of the clock period.
        /// </summary>
        /// <value>The length of the clock.</value>
        public int ClockLength { get; set; } = 10;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SME.VHDL.RenderState"/> class.
        /// </summary>
        /// <param name="simulation">The simulation to parse.</param>
        /// <param name="targetfolder">The folder where the output is stored.</param>
        /// <param name="backupfolder">The folder where backups are stored.</param>
        /// <param name="csvtracename">The name of the CSV trace file.</param>
        /// <param name="customfiles">A list of VHDL files to include in the Makefile, without the VHDL extension.</param>
        public RenderState(Simulation simulation, string targetfolder, string backupfolder = null, string csvtracename = null, IEnumerable<string> customfiles = null, RenderConfig config = null)
        {
            Simulation = simulation;
            TargetFolder = targetfolder;
            BackupFolder = backupfolder;
            CSVTracename = csvtracename;
            CustomFiles = customfiles;
            Config = config ?? new RenderConfig();

            Network = ParseProcesses.BuildNetwork(simulation, true);

            ValidateNetwork(Network);

            var methodsource = Network.Processes.FirstOrDefault(x => x.MainMethod != null);
            if (methodsource == null)
            {
                // This happens if we only have components in the design
                TypeScope = new VHDLTypeScope(Network.Processes.First().MSCAType.ContainingAssembly);
            }
            else
                TypeScope = new VHDLTypeScope(methodsource.MSCAType.ContainingAssembly);

            Types = Network
                .All()
                .OfType<DataElement>()
                .Select(x => x.MSCAType)
                .Distinct((IEqualityComparer<ITypeSymbol>) SymbolEqualityComparer.Default)
                .ToArray();

            Network.Name = Naming.AssemblyToValidName();

            SME.AST.Transform.Apply.Transform(
                Network,
                new SME.AST.Transform.IASTTransform[] {
                    new Transformations.AssignNames(),
                    new SME.AST.Transform.RenameDuplicateVariables(),
                    new SME.AST.Transform.BuildStateMachine(Network.compilation),
                },
                m => new SME.AST.Transform.IASTTransform[] {
                    new Transformations.RewriteChainedAssignments(this, m),
                },
                m => new SME.AST.Transform.IASTTransform[] {
                    new SME.AST.Transform.RemoveDoubleCast(),
                    new Transformations.WrapIfComposite(),
                    new Transformations.AssignNames(),
                    new SME.AST.Transform.RecontructSwitchStatement(),
                    new SME.AST.Transform.RemoveTrailingBreakStatement(),
                    new Transformations.AssignVhdlType(this),
                    new Transformations.FixSwitchStatementTypes(this),
                    new Transformations.RemoveNonstaticSwitchLabels(this),
                    new Transformations.RemoveConditionals(this, m),
                    new Transformations.InsertReturnAssignments(this, m),
                    new Transformations.InjectTypeConversions(this, m),
                    new SME.AST.Transform.RewireCompositeAssignment(),
                    new Transformations.FixForLoopIncrements(this, m),
                    new Transformations.RewireUnaryOperators(this),
                    new Transformations.UntangleElseStatements(this, m),
                },
                m => new SME.AST.Transform.IASTTransform[] {
                    new SME.AST.Transform.RemoveExtraParenthesis()
                }
            );

            SetupCustomRenderers(Config.COMPONENT_RENDERER_STRATEGY);
        }

        /// <summary>
        /// Resets the custom renderers to fit the desired rendering strategy.
        /// </summary>
        /// <param name="strategy">The rendering strategy.</param>
        public void SetupCustomRenderers(ComponentRendererStrategy strategy)
        {
            CustomRenderers.Clear();

            if (strategy == ComponentRendererStrategy.Inferred)
            {
                CustomRenderers[typeof(SME.Components.SinglePortMemory<>)] = new CustomRenders.Inferred.SinglePortRam();
                CustomRenderers[typeof(SME.Components.SimpleDualPortMemory<>)] = new CustomRenders.Inferred.SimpleDualPortRam();
                CustomRenderers[typeof(SME.Components.TrueDualPortMemory<>)] = new CustomRenders.Inferred.TrueDualPortRam();
            }
            else if (strategy == ComponentRendererStrategy.Native)
            {
                if (Config.DEVICE_VENDOR == FPGAVendor.Xilinx)
                {
                    CustomRenderers[typeof(SME.Components.SinglePortMemory<>)] = new CustomRenders.Native.XilinxSinglePortRam();
                    CustomRenderers[typeof(SME.Components.SimpleDualPortMemory<>)] = new CustomRenders.Native.XilinxSimpleDualPortRam();
                    CustomRenderers[typeof(SME.Components.TrueDualPortMemory<>)] = new CustomRenders.Native.XilinxTrueDualPortRam();
                }
                else
                {
                    CustomRenderers[typeof(SME.Components.SinglePortMemory<>)] = new CustomRenders.Inferred.SinglePortRam();
                    CustomRenderers[typeof(SME.Components.SimpleDualPortMemory<>)] = new CustomRenders.Inferred.SimpleDualPortRam();
                    CustomRenderers[typeof(SME.Components.TrueDualPortMemory<>)] = new CustomRenders.Inferred.TrueDualPortRam();
                }

            }
        }

        /// <summary>
        /// Performs some checks to see if the network uses features that are not supported by the VHDL render.
        /// </summary>
        /// <param name="network">The network to validate.</param>
        private static void ValidateNetwork(AST.Network network)
        {
            var sp = network.Processes.SelectMany(x => x.InternalBusses).FirstOrDefault(x => x.IsTopLevelInput || x.IsTopLevelOutput);
            if (sp != null)
                throw new Exception($"Cannot have an internal bus that is also toplevel input or output: {sp.Name}");
        }

        /// <summary>
        /// Renders this instance to files.
        /// </summary>
        public void Render()
        {
            // Build a distinct map based on the types
            var used = new HashSet<Type>();
            var processes =
                Network.Processes.Select(x =>
                {
                    if (used.Contains(x.SourceType))
                        return null;

                    used.Add(x.SourceType);
                    return x;
                })
            .Where(x => x != null)
            .ToArray();

            var extrafiles = from f in System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames()
                             where
                                 f.EndsWith(".vhdl", StringComparison.InvariantCultureIgnoreCase)
                                 &&
                                 f.StartsWith(typeof(Templates.TopLevel).Namespace + ".", StringComparison.InvariantCultureIgnoreCase)
                             select f;

            var filenames = new
            {
                toplevel = Naming.AssemblyNameToFileName(),
                export = "Export_" + Naming.AssemblyNameToFileName(),
                testbench = "TestBench_" + Naming.AssemblyNameToFileName(),
                types = "Types_" + Naming.AssemblyNameToFileName(),
                makefile = "Makefile",
                projectfile = Config.DEVICE_VENDOR == FPGAVendor.Xilinx ? System.IO.Path.ChangeExtension(Naming.AssemblyNameToFileName(), "xpr") : null
            };



            var protectedfiles =
                filenames.GetType().GetFields().Where(x => x.FieldType == typeof(string)).Select(x => x.GetValue(x) as string)
                .Concat(extrafiles)
                .Concat(processes.Select(x => Naming.ProcessNameToFileName(x.SourceInstance.Instance)));

            BackupExistingTarget(protectedfiles, TargetFolder, BackupFolder);

            var targetTopLevel = Path.Combine(TargetFolder, filenames.toplevel);
            File.WriteAllText(targetTopLevel, MergeUserData(new TopLevel(this).TransformText(), targetTopLevel));

            var targetTestBench = Path.Combine(TargetFolder, filenames.testbench);
            File.WriteAllText(targetTestBench, MergeUserData(new TracefileTester(this).TransformText(), targetTestBench));

            var targetTypeLib = Path.Combine(TargetFolder, filenames.types);
            File.WriteAllText(targetTypeLib, MergeUserData(new CustomTypes(this).TransformText(), targetTypeLib));

            var exportTypeLib = Path.Combine(TargetFolder, filenames.export);
            File.WriteAllText(exportTypeLib, MergeUserData(new ExportTopLevel(this).TransformText(), exportTypeLib));

            File.WriteAllText(Path.Combine(TargetFolder, filenames.makefile), new GHDL_Makefile(this).TransformText());

            if (Config.DEVICE_VENDOR == FPGAVendor.Xilinx)
                File.WriteAllText(Path.Combine(TargetFolder, filenames.projectfile), new VivadoProject(this, processes).TransformText());

            foreach (var p in processes)
            {
                var rsp = new RenderStateProcess(this, p);
                var targetfile = Path.Combine(TargetFolder, Naming.ProcessNameToFileName(p.SourceInstance.Instance));
                File.WriteAllText(targetfile, MergeUserData(new Entity(this, rsp).TransformText(), targetfile));
            }

            foreach (var vhdlfile in extrafiles)
                using (var rs = new System.IO.StreamReader(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(vhdlfile)))
                    File.WriteAllText(Path.Combine(TargetFolder, vhdlfile.Substring(typeof(Templates.TopLevel).Namespace.Length + 1)), rs.ReadToEnd());

        }

        /// <summary>
        /// Makes a backup of all target files.
        /// </summary>
        /// <param name="targetfolder">The folder where output is stored.</param>
        /// <param name="backupfolder">The folder where backups are stored.</param>
        /// <param name="filenames">A list of extra files that may be overwritten/regenerated.</param>
        private void BackupExistingTarget(IEnumerable<string> filenames, string targetfolder, string backupfolder)
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

                foreach (var f in (filenames ?? new string[0]).Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    var source = Path.Combine(targetfolder, f);
                    if (File.Exists(source))
                        File.Copy(source, Path.Combine(backupname, f));
                }
            }
            else
            {
                Directory.CreateDirectory(targetfolder);
            }
        }

        /// <summary>
        /// Merges a newly rendered file with user supplied data from an existing file.
        /// </summary>
        /// <returns>The user data.</returns>
        /// <param name="text">The newly rendered content.</param>
        /// <param name="targetfile">The file to write.</param>
        private string MergeUserData(string text, string targetfile)
        {
            if (File.Exists(targetfile))
            {
                var core = @"[ \t]*--[ \t]+####[ \t]+USER-DATA-{0}-{1}[ \t]*(\r|\r\n|\n)";

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
        /// Returns the VHDL type for a data element.
        /// </summary>
        /// <returns>The VHDL type.</returns>
        /// <param name="element">The element to get the type for.</param>
        public VHDLType VHDLType(AST.DataElement element)
        {
            VHDLType res;
            if (TypeLookup.TryGetValue(element, out res))
                return res;

            if (element is AST.Constant && ((Constant)element).ArrayLengthSource != null)
                return TypeLookup[element] = VHDLTypes.INTEGER;

            if (element.Source is IFieldSymbol)
                return TypeLookup[element] = TypeScope.GetVHDLType(element.Source as IFieldSymbol, element.MSCAType);
            else if (element.Source is IParameterSymbol)
                return TypeLookup[element] = TypeScope.GetVHDLType(element.Source as IParameterSymbol);
            else if (element.Source is System.Reflection.PropertyInfo)
                return TypeLookup[element] = TypeScope.GetVHDLType(element.Source as System.Reflection.PropertyInfo);
            else
                return TypeLookup[element] = TypeScope.GetVHDLType(element.MSCAType);
        }

        /// <summary>
        /// Returns the VHDL type for a data element.
        /// </summary>
        /// <returns>The VHDL type.</returns>
        /// <param name="element">The element to get the type for.</param>
        public VHDLType VHDLType(AST.Method element)
        {
            return VHDLType(element.ReturnVariable);
        }

        /// <summary>
        /// Wraps the VHDL typename if the type is an array type.
        /// </summary>
        /// <returns>The wrapped type name.</returns>
        /// <param name="element">The element to wrap.</param>
        public string VHDLWrappedTypeName(AST.DataElement element)
        {
            var vt = VHDLType(element);
            var isarray = element.MSCAType.IsArrayType() || (element.Parent is AST.Bus && (element.Parent as AST.Bus).SourceInstances.Length > 1);
            if (element.MSCAType.IsArrayType())
            {
                if (element.Parent is AST.Bus)
                    return element.Parent.Name + "_" + element.Name + "_type";

                var p = element.Parent;
                while (p != null && !(p is AST.Process))
                    p = p.Parent;

                if (p is AST.Process)
                    return p.Name + "_" + element.Name + "_type";

                return element.Name + "_type";
            }

            return vt.ToSafeVHDLName();
        }

        /// <summary>
        /// Gets the type name for the given element in the top level export file.
        /// </summary>
        /// <param name="element">The element to convert.</param>
        public string VHDLExportTypeName(AST.DataElement element, int multiplier = 1)
        {
            var vt = VHDLType(element);
            if (vt == VHDLTypes.BOOL || vt == VHDLTypes.SYSTEM_BOOL)
                if (multiplier > 1) return
                    "STD_LOGIC_VECTOR(" + (multiplier - 1) + " downto 0)";
                else
                    return "STD_LOGIC";

            if (vt.IsSystemType || vt.IsVHDLSigned || vt.IsVHDLUnsigned)
                return TypeScope.StdLogicVectorEquivalent(vt).ToSafeVHDLName();

            // TODO: Figure out how to best export array types
            if (element.MSCAType.IsArrayType())
            {
                if (element.Parent is AST.Bus)
                    return element.Parent.Name + "_" + element.Name + "_type";

                var p = element.Parent;
                while (p != null && !(p is AST.Process))
                    p = p.Parent;

                if (p is AST.Process)
                    return p.Name + "_" + element.Name + "_type";

                return element.Name + "_type";
            }

            return vt.ToSafeVHDLName();
        }

        /// <summary>
        /// Gives the function to convert from SME type to export type
        /// </summary>
        /// <returns>The type cast function name</returns>
        /// <param name="vt">The type to convert</param>
        public string VHDLExportTypeCast(VHDLType vt)
        {
            if (vt == VHDLTypes.BOOL || vt == VHDLTypes.SYSTEM_BOOL)
                return "STD_LOGIC";

            if (vt.IsVHDLSigned)
                return "signed";

            if (vt.IsVHDLUnsigned)
                return "unsigned";

            if (vt.IsSystemType)
                return "std_logic_vector";

            return vt.ToSafeVHDLName();
        }

        /// <summary>
        /// Gives the type name suited for the export file
        /// </summary>
        /// <returns>The type name</returns>
        /// <param name="vt">The type to get the name for</param>
        public string VHDLExportTypeName(VHDLType vt, int multiplier = 1)
        {
            if (vt == VHDLTypes.BOOL || vt == VHDLTypes.SYSTEM_BOOL)
                if (multiplier > 1) return
                    "STD_LOGIC_VECTOR(" + (multiplier - 1) + " downto 0)";
                else
                    return "STD_LOGIC";

            if (vt.IsSystemType || vt.IsVHDLSigned || vt.IsVHDLUnsigned)
                return TypeScope.StdLogicVectorEquivalent(vt, multiplier).ToSafeVHDLName();

            return vt.ToSafeVHDLName();
        }

        /// Wraps the VHDL typename if the type is an array type.
        /// </summary>
        /// <returns>The wrapped type name.</returns>
        /// <param name="fd">The Field to wrap.</param>
        public string VHDLWrappedTypeName(IFieldSymbol fd)
        {
            var vt = TypeScope.GetVHDLType(fd);
            if (fd.Type.IsArrayType())
            {
                if (fd.ContainingType.IsSameTypeReference(typeof(Process)))
                    return fd.ContainingType.Name + "_" + fd.Name + "_type";

                return fd.Name + "_type";
            }

            return vt.ToSafeVHDLName();
        }

        /// <summary>
        /// Returns the VHDL type for an expression.
        /// </summary>
        /// <returns>The VHDL type.</returns>
        /// <param name="element">The expression to get the type for.</param>
        public VHDLType VHDLType(AST.Expression element)
        {
            if (element is ParenthesizedExpression)
                return VHDLType(((ParenthesizedExpression)element).Expression);

            VHDLType res;
            if (TypeLookup.TryGetValue(element, out res))
                return res;

            if (element is MemberReferenceExpression)
                return TypeLookup[element] = VHDLType(((MemberReferenceExpression)element).Target);
            else if (element is IdentifierExpression)
                return TypeLookup[element] = VHDLType(((IdentifierExpression)element).Target);
            else if (element is InvocationExpression)
                return TypeLookup[element] = VHDLType(((InvocationExpression)element).Target);
            else if (element is CastExpression)
                return TypeLookup[element] = TypeScope.GetVHDLType(((CastExpression)element).SourceResultType);
            else if (element is PrimitiveExpression)
            {
                var rawtype = TypeScope.GetVHDLType(element.SourceResultType);
                if (rawtype.IsNumeric || rawtype.IsSigned || rawtype.IsUnsigned)
                    return TypeLookup[element] = VHDLTypes.INTEGER;
                else
                    return TypeLookup[element] = rawtype;
            }
            else if (element is BinaryOperatorExpression)
            {
                var boe = element as BinaryOperatorExpression;
                if (boe.Operator.IsCompareOperator() || boe.Operator.IsLogicalOperator())
                    return TypeLookup[element] = VHDLTypes.BOOL;

                var vleft = VHDLType(boe.Left);
                var vright = VHDLType(boe.Right);

                return TypeLookup[element] = TypeScope.GetVHDLType(element.SourceResultType);
            }
            else if (element is UnaryOperatorExpression)
            {
                var uoe = element as UnaryOperatorExpression;
                if (uoe.Operator == SyntaxKind.ExclamationToken)
                    return TypeLookup[element] = VHDLTypes.BOOL;
                else
                    return TypeLookup[element] = VHDLType(uoe.Operand);
            }
            else if (element is WrappingExpression)
                return TypeLookup[element] = VHDLType(((WrappingExpression)element).Expression);
            else
                return TypeLookup[element] = TypeScope.GetVHDLType(element.SourceResultType);
        }

        /// <summary>
        /// Gets the default value for an item, expressed as a VHDL expression.
        /// </summary>
        /// <returns>The default value.</returns>
        /// <param name="element">The element to get the default value for.</param>
        public string DefaultValue(AST.DataElement element)
        {
            object pval = element.DefaultValue;

            if (pval == null && element.Type != null && element.Type.IsPrimitive)
                pval = Activator.CreateInstance(element.Type);

            if (element.Source is System.Reflection.PropertyInfo)
            {
                var pd = element.Source as System.Reflection.PropertyInfo;
                var init = pd.GetCustomAttributes(typeof(InitialValueAttribute), true).FirstOrDefault() as InitialValueAttribute;

                if (init != null && init.Value != null)
                    pval = init.Value;

                if (pd.PropertyType == typeof(bool))
                    return ((object)true).Equals(pval) ? "'1'" : "'0'";
                else if (pd.PropertyType.IsEnum)
                {
                    if (pval == null)
                        return Naming.ToValidName(pd.PropertyType.FullName + "." + pd.PropertyType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly).Skip(1).First().Name);
                    else
                        return Naming.ToValidName(pd.PropertyType.FullName + "." + pval.ToString());
                }
            }

            if (element.Source is IPropertySymbol)
            {
                var pd = element.Source as IPropertySymbol;
                var init = pd.GetAttribute<InitialValueAttribute>();

                if (init != null && init.ConstructorArguments.Count() > 0)
                {
                    pval = init.ConstructorArguments.First().Value;
                }

                if (pd.Type.IsType<bool>())
                    return ((object)true).Equals(pval) ? "'1'" : "'0'";
                else if (pd.Type.IsEnum())
                {
                    if (pval == null)
                        return Naming.ToValidName(pd.Type.ToDisplayString() + "." + pd.Type.GetMembers().OfType<IFieldSymbol>().Skip(1).First().Name);
                    else
                        return Naming.ToValidName(pd.Type.ToDisplayString() + "." + pd.Type.GetMembers().OfType<IFieldSymbol>().Where(x => pval.Equals(x.ConstantValue)).First().Name);
                }
            }

            var tvhdl = VHDLType(element);
            var elvhdl = tvhdl;
            while (elvhdl.IsArray && !elvhdl.IsStdLogicVector && !elvhdl.IsVHDLSigned && !elvhdl.IsVHDLUnsigned)
                elvhdl = TypeScope.GetByName(elvhdl.ElementName);

            if (pval != null)
            {
                var pstr = tvhdl.ToString();
                if (pstr.StartsWith("T_", StringComparison.InvariantCultureIgnoreCase))
                    pstr = pstr.Substring(2);
                pval = string.Format("{0}({1})", pstr, pval);
            }

            while (tvhdl.IsArray && tvhdl != elvhdl)
            {
                pval = string.Format("(others => {0})", pval);
                tvhdl = TypeScope.GetByName(tvhdl.ElementName);
            }

            return pval.ToString();
        }

        /// <summary>
        /// Gets a list of custom types to implement.
        /// </summary>
        /// <value>The custom types.</value>
        public IEnumerable<VHDLType> CustomTypes
        {
            get
            {
                var ignores = TypeScope.BuiltinNames.ToDictionary(x => x, y => String.Empty);
                ignores["System.Void"] = string.Empty;

                return Network
                    .All()
                    .OfType<DataElement>()
                    .Where(x =>
                    {
                        var rd = x.MSCAType;
                        // TODO handle custom attributes
                        //var custom = rd.CustomAttributes.Any(y => y.AttributeType.IsSameTypeReference(typeof(VHDLTypeAttribute)));

                        return
                            //(!custom)
                               //&&
                               (rd.IsEnum() || (rd.IsValueType && rd.SpecialType == SpecialType.None));
                    })
                    .Select(x => VHDLType(x))
                    .Select(x => x.IsArray && !x.IsStdLogicVector ? TypeScope.GetByName(x.ElementName) : x)
                    .Where(x => !ignores.ContainsKey(x.ToString()))
                    .Distinct();
            }
        }

        /// <summary>
        /// Gets all arrays used in bus signals.
        /// </summary>
        /// <value>The bus arrays.</value>
        public IEnumerable<BusSignal> BusArrays
        {
            get
            {
                var res = Network
                    .All()
                    .OfType<BusSignal>()
                    .Where(x => x.MSCAType.IsArrayType())
                    .Distinct()
                    // Distinct() doesn't truely capture distinction:
                    .GroupBy(x => x.Parent.Name)
                    .SelectMany(x => x
                        .GroupBy(y => y.Name)
                        .Select(y => y.First())
                    );

                return res;
            }
        }

        /// <summary>
        /// Gets the length of an array attached to a bus.
        /// </summary>
        /// <returns>The array length.</returns>
        /// <param name="signal">The signal to get the length for.</param>
        public int GetArrayLength(BusSignal signal)
        {
            if (signal.Source is System.Reflection.MemberInfo)
            {
                var fixedattr = ((System.Reflection.MemberInfo)signal.Source).GetCustomAttributes(typeof(FixedArrayLengthAttribute), true).FirstOrDefault() as FixedArrayLengthAttribute;
                if (fixedattr != null)
                    return fixedattr.Length;
            }

            throw new Exception($"Unable to find the array length for {signal.Parent.Name}_{signal.Name}");
        }

        /// <summary>
        /// Returns a map of key/value pairs from an enum type.
        /// </summary>
        /// <returns>The enum values.</returns>
        /// <param name="t">T.</param>
        public IEnumerable<KeyValuePair<string, object>> GetEnumValues(VHDLType t)
        {
            var td = t.SourceType;
            if (!td.IsEnum())
                throw new InvalidOperationException("Cannot list enum values from a non-enum type");

            var fields = td
                .GetMembers()
                .OfType<IFieldSymbol>()
                .Select(m =>
                    new KeyValuePair<string, object>(
                        Naming.ToValidName(td.ToDisplayString() + "_" + m.Name),
                        m.ConstantValue
                    )
                );

            Dictionary<string, object> customs;
            CustomEnumValues.TryGetValue(t, out customs);

            if (customs != null)
                fields = fields.Concat(customs);

            return fields.ToArray();
        }

        /// <summary>
        /// Lists all members for a given value type.
        /// </summary>
        /// <returns>The member VHDL strings.</returns>
        /// <param name="type">The VHDL type.</param>
        public IEnumerable<string> ListMembers(VHDLType type)
        {
            var td = type.SourceType;

            if (type.IsEnum)
            {
                Dictionary<string, object> customs;
                CustomEnumValues.TryGetValue(type, out customs);
                customs = customs ?? new Dictionary<string, object>();

                var members = td
                    .GetMembers()
                    .OfType<IFieldSymbol>()
                    .Select(m =>
                        Naming.ToValidName($"{td.ToDisplayString()}_{m.Name}")
                    )
                    .Concat(customs.Keys);
                foreach (var member in members)
                    yield return member;
            }
            else if (td.IsValueType && td.SpecialType == SpecialType.None)
            {
                yield return "record";

                foreach (var m in td.GetMembers().OfType<IFieldSymbol>())
                    if (!m.IsStatic)
                        yield return string.Format("    {0}: {1};", Naming.ToValidName(m.Name), VHDLWrappedTypeName(m));

                yield return "end record;";
            }
        }

        /// <summary>
        /// Gets all enum types in the network.
        /// </summary>
        public IEnumerable<VHDLType> EnumTypes
        {
            get
            {
                return Types
                    .Where(x => x.IsEnum())
                    .Select(x => TypeScope.GetVHDLType(x));
            }
        }

        /// <summary>
        /// Gets all constant definition strings.
        /// </summary>
        public IEnumerable<string> Constants
        {
            get
            {
                foreach (var n in Network.Constants)
                {
                    object nx = n.DefaultValue;
                    Exception nex = null;
                    string convm = null;
                    try
                    {
                        if (nx is ArrayCreateExpression || nx is Array)
                        {
                            var eltype = n.MSCAType.GetArrayElementType();
                            convm = TypeScope.GetVHDLType(eltype).ToString().Substring("T_".Length);
                        }
                        else
                        {
                            convm = VHDLType(n).ToString().Substring("T_".Length);
                        }
                    }
                    catch (Exception ex)
                    {
                        nex = ex;
                    }

                    if (nex != null)
                    {
                        foreach (var m in nex.Message.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                            yield return string.Format("-- {0}", m);
                    }
                    else
                    {
                        if (nx != null && convm != null)
                        {
                            if (nx is ArrayCreateExpression)
                            {
                                var arc = nx as ArrayCreateExpression;

                                var varname = Naming.ToValidName(n.Name);
                                var eltype = n.MSCAType.GetArrayElementType();
                                var vhdl_eltype = TypeScope.GetVHDLType(eltype);

                                var ellength = arc.ElementExpressions.Count();
                                var eltrail = " - 1";
                                if (Config.USE_EXPLICIT_LITERAL_ARRAY_LENGTH)
                                {
                                    ellength--;
                                    eltrail = "";
                                }

                                yield return string.Format("type {0}_type is array (0 to {1}{3}) of {2}", varname, ellength, vhdl_eltype, eltrail);

                                var values = string.Join(", ", arc.ElementExpressions.Select(x => string.Format("{0}({1})", convm, VHDLTypeConversion.GetPrimitiveLiteral(x, vhdl_eltype, this))));
                                yield return string.Format("constant {0}: {0}_type := ({1})", varname, values);

                            }
                            else if (nx is AST.ArrayCreateExpression)
                            {
                                var arc = nx as AST.ArrayCreateExpression;

                                var varname = Naming.ToValidName(n.Name);
                                var eltype = n.MSCAType.GetArrayElementType();
                                var vhdl_eltype = TypeScope.GetVHDLType(eltype);

                                var ellength = arc.ElementExpressions.Length;
                                var eltrail = " - 1";
                                if (Config.USE_EXPLICIT_LITERAL_ARRAY_LENGTH)
                                {
                                    ellength--;
                                    eltrail = "";
                                }

                                yield return string.Format("type {0}_type is array (0 to {1}{3}) of {2}", varname, ellength, vhdl_eltype, eltrail);

                                var values = string.Join(", ", arc.ElementExpressions.Select(x => string.Format("{0}({1})", convm, VHDLTypeConversion.GetPrimitiveLiteral(x as AST.PrimitiveExpression, vhdl_eltype, this))));

                                yield return string.Format("constant {0}: {0}_type := ({1})", varname, values);
                            }
                            else if (nx is AST.EmptyArrayCreateExpression)
                            {
                                var arc = nx as AST.EmptyArrayCreateExpression;

                                var varname = Naming.ToValidName(n.Name);
                                var eltype = n.MSCAType.GetArrayElementType();
                                var vhdl_eltype = TypeScope.GetVHDLType(eltype);

                                var ellength = (int)((AST.PrimitiveExpression)arc.SizeExpression).Value;
                                var eltrail = " - 1";
                                if (Config.USE_EXPLICIT_LITERAL_ARRAY_LENGTH)
                                {
                                    ellength--;
                                    eltrail = "";
                                }

                                yield return string.Format("type {0}_type is array (0 to {1}{3}) of {2}", varname, ellength, vhdl_eltype, eltrail);
                            }
                            else if (nx is Array)
                            {
                                var arc = nx as Array;

                                var varname = Naming.ToValidName(n.Name);
                                var eltype = n.MSCAType.GetArrayElementType();
                                var vhdl_eltype = TypeScope.GetVHDLType(eltype);

                                var ellength = arc.Length;
                                var eltrail = " - 1";
                                if (Config.USE_EXPLICIT_LITERAL_ARRAY_LENGTH)
                                {
                                    ellength--;
                                    eltrail = "";
                                }

                                yield return string.Format("type {0}_type is array (0 to {1}{3}) of {2}", varname, ellength, vhdl_eltype, eltrail);

                                var elements = Enumerable.Range(0, arc.Length).Select(x => arc.GetValue(x));

                                var values = string.Join(", ", elements.Select(x => string.Format("{0}({1})", convm,  VHDLTypeConversion.GetPrimitiveLiteral(x, vhdl_eltype, this))));
                                yield return string.Format("constant {0}: {0}_type := ({1})", varname, values);
                            }
                            else
                            {
                                yield return string.Format("constant {0}: {1} := {2}({3})", Naming.ToValidName($"{n.Name}"), VHDLType(n), convm, nx);
                            }
                        }
                        else
                            yield return string.Format("-- constant {0}: {1} := ???", Naming.ToValidName(n.Name), VHDLType(n));
                    }

                }

            }
        }

        /// <summary>
        /// Returns all signals in the network.
        /// </summary>
        public IEnumerable<BusSignal> AllSignals
        {
            get
            {
                return Network.Busses.Where(x => !x.IsInternal).SelectMany(x => x.Signals);
            }
        }

        /// <summary>
        /// Splits a signal into a signal for each array element.
        /// </summary>
        /// <returns>The array signals.</returns>
        /// <param name="signal">The signal to split.</param>
        public IEnumerable<BusSignal> SplitArray(BusSignal signal)
        {
            if (!signal.MSCAType.IsArrayType())
            {
                yield return signal;
            }
            else
            {
                var attr = (signal.Source as System.Reflection.MemberInfo).GetCustomAttributes(typeof(FixedArrayLengthAttribute), true).FirstOrDefault() as FixedArrayLengthAttribute;
                if (attr == null)
                    throw new Exception($"Expected an array length on {signal.Name} ({signal.Source})");

                for (var i = 0; i < attr.Length; i++)
                    yield return new BusSignal()
                    {
                        MSCAType = signal.MSCAType.GetArrayElementType(),
                        Parent = signal.Parent,
                        Name = string.Format("{0}({1})", signal.Name, i)
                    };
            }
        }

        /// <summary>
        /// Returns the name written in the trace file for a given signal.
        /// </summary>
        /// <returns>The signal name as written by the tracer.</returns>
        /// <param name="s">The signal to find the name for.</param>
        public string TestBenchSignalName(BusSignal s)
        {
            var bus = (AST.Bus)s.Parent;
            var st = bus.SourceType;
            var bi = bus.SourceInstances.First();
            if (bi != null && Simulation.BusNames.ContainsKey(bi))
            {
                var res = (Simulation.BusNames[bi] + "." + s.Name).Replace(",", "_");
                return res;
            }

            var name = st.Name + "." + s.Name;
            if (st.DeclaringType != null)
                name = st.DeclaringType.Name + "." + name;

            return name.Replace(",", "_");
        }

        /// <summary>
        /// Returns all the top-level input busses.
        /// </summary>
        public IEnumerable<SME.AST.Bus> DriverBusses
        {
            get
            {
                return Network.Busses.Where(x => x.IsTopLevelInput).OrderBy(x => x.InstanceName);
            }
        }

        /// <summary>
        /// Returns all signals from top-level input busses.
        /// </summary>
        public IEnumerable<BusSignal> DriverSignals
        {
            get
            {
                return Network.Busses
                              .Where(x => x.IsTopLevelInput)
                              .SelectMany(x => x.Signals)
                              .OrderBy(x => TestBenchSignalName(x))
                              .SelectMany(x => SplitArray(x));
            }
        }

        /// <summary>
        /// Returns all the top-level output busses.
        /// </summary>
        public IEnumerable<SME.AST.Bus> VerifyBusses
        {
            get
            {
                return Network.Busses.Where(x => !x.IsTopLevelInput && !x.IsInternal).OrderBy(x => x.InstanceName);
            }
        }

        /// <summary>
        /// Returns all signals from non-top-level input busses.
        /// </summary>
        public IEnumerable<BusSignal> VerifySignals
        {
            get
            {
                return Network.Busses
                              .Where(x => !x.IsTopLevelInput && !x.IsInternal)
                              .SelectMany(x => x.Signals)
                              .OrderBy(x => TestBenchSignalName(x))
                              .SelectMany(x => SplitArray(x));
            }
        }

        /// <summary>
        /// Creates a new variable in the process space.
        /// </summary>
        /// <returns>The temporary variable to create.</returns>
        /// <param name="variabletype">The type of the variable to use.</param>
        public Variable RegisterTemporaryVariable(Method method, ITypeSymbol variabletype)
        {
            Dictionary<string, Variable> table;
            if (!TemporaryVariables.TryGetValue(method, out table))
                TemporaryVariables[method] = table = new Dictionary<string, Variable>();

            object def = null;
            if (variabletype.IsValueType && Type.GetType(variabletype.ToDisplayString()) != null)
                def = Activator.CreateInstance(Type.GetType(variabletype.ToDisplayString()));

            var name = "local_var_" + table.Count.ToString();
            return table[name] = new Variable()
            {
                Name = name,
                MSCAType = variabletype,
                Parent = method,
                DefaultValue = def
            };
        }

        /// <summary>
        /// Gets all bus instances that require a feedback loop.
        /// </summary>
        /// <value>The feedback busses.</value>
        public IEnumerable<AST.Bus> FeedbackBusses
        {
            get
            {
                return Network.Processes.SelectMany(x => x.InputBusses.Where(y => x.OutputBusses.Contains(y) && y.IsTopLevelOutput));
            }
        }

        /// <summary>
        /// Returns all signals written to a bus from within the process.
        /// </summary>
        /// <returns>The written signals.</returns>
        /// <param name="proc">The process to see if the signals are written.</param>
        /// <param name="bus">The bus to get the signals for.</param>
        public IEnumerable<BusSignal> WrittenSignals(AST.Process proc, AST.Bus bus)
        {
            // TODO: Apply this logic?
            // Components are assumed to write their outputs
            //if (proc.SourceInstance.Instance is IVHDLComponent)
                //return bus.Signals;

            return proc
                .All()
                .Select(x =>
                {
                    if (x is AST.AssignmentExpression)
                    {
                        var ax = x as AST.AssignmentExpression;
                        if (ax.Left is AST.MemberReferenceExpression)
                            return ((AST.MemberReferenceExpression)ax.Left).Target;
                        else if (ax.Left is AST.IndexerExpression)
                            return ((AST.IndexerExpression)ax.Left).Target;
                    }
                    else if (x is AST.UnaryOperatorExpression)
                    {
                        var ux = x as AST.UnaryOperatorExpression;
                        if (ux.Operand is AST.MemberReferenceExpression)
                            return ((AST.MemberReferenceExpression)ux.Operand).Target;
                        else if (ux.Operand is AST.IndexerExpression)
                            return ((AST.IndexerExpression)ux.Operand).Target;
                    }

                    return null;
                })
                .Where(x => x != null)
                .OfType<AST.BusSignal>()
                .Where(x => x.Parent == bus)
                // TODO Temp fix. Signals on busses, which carry a default
                // value, but are never written to, are wrongfully discarded.
                // This results in the trace file containing the default value,
                // while the resulting VHDL produces "U" (undefined) values,
                // thus making the testbench fail. There are multiple
                // "correct" fixes:
                // - There can be only 1 process with the same bus instance as
                //   output bus. This could be annoying as the user would have
                //   to explicitly specify bus merging processes.
                // - The signal is also discarded by the testbench, rather
                //   than just the process.
                // - The tracer detects this and outputs "U" instead of the
                //   default value. The problem is that this check is made in
                //   SME.VHDL, but SME.Tracer should be render agnostic.
                .Union(bus.Signals.Where(x => !(x.DefaultValue == null)))
                .Distinct();
        }

        /// <summary>
        /// Creates a reset statement for the specified data element.
        /// </summary>
        /// <returns>The statement expressing reset of the date element.</returns>
        /// <param name="element">The target element.</param>
        public AST.Statement GetResetStatement(DataElement element)
        {
            var exp = new AST.AssignmentExpression()
            {
                Left = new MemberReferenceExpression()
                {
                    Name = element.Name,
                    Target = element,
                    SourceResultType = element.MSCAType
                }
            };

            var tvhdl = VHDLType(exp.Left);

            var res = new AST.ExpressionStatement()
            {
                Expression = exp
            };
            exp.Parent = res;

            if (element.DefaultValue is AST.ArrayCreateExpression)
            {
                var asexp = (AST.ArrayCreateExpression)element.DefaultValue;

                var nae = new ArrayCreateExpression()
                {
                    SourceExpression = asexp.SourceExpression,
                    SourceResultType = asexp.SourceResultType,
                };

                nae.ElementExpressions = asexp.ElementExpressions
                    .Select(x => new PrimitiveExpression()
                    {
                        SourceExpression = x.SourceExpression,
                        SourceResultType = x.SourceResultType,
                        Parent = nae,
                        Value = ((PrimitiveExpression)x).Value
                    }).Cast<Expression>().ToArray();

                var elvhdl = TypeScope.GetByName(tvhdl.ElementName);

                for (var i = 0; i < nae.ElementExpressions.Length; i++)
                    VHDLTypeConversion.ConvertExpression(this, null, nae.ElementExpressions[i], elvhdl, nae.ElementExpressions[i].SourceResultType, false);

                exp.Right = nae;
                TypeLookup[nae] = tvhdl;
            }
            else if (element.DefaultValue is Array)
            {
                var asexp = (Array)element.DefaultValue;

                var nae = new ArrayCreateExpression()
                {
                    SourceExpression = null,
                    SourceResultType = element.MSCAType
                };

                nae.ElementExpressions = Enumerable.Range(0, asexp.Length)
                    .Select(x => new PrimitiveExpression()
                    {
                        SourceExpression = null,
                        SourceResultType = element.MSCAType.GetArrayElementType(),
                        Parent = nae,
                        Value = asexp.GetValue(x)
                    }).Cast<Expression>().ToArray();

                var elvhdl = TypeScope.GetByName(tvhdl.ElementName);

                for (var i = 0; i < nae.ElementExpressions.Length; i++)
                    VHDLTypeConversion.ConvertExpression(this, null, nae.ElementExpressions[i], elvhdl, nae.ElementExpressions[i].SourceResultType, false);

                exp.Right = nae;
                TypeLookup[nae] = tvhdl;
            }
            else if (element.DefaultValue is SyntaxNode)
            {
                var eltype = Type.GetType(element.MSCAType.GetFullMetadataName());
                var defaultvalue = eltype != null && element.MSCAType.IsValueType ? Activator.CreateInstance(eltype) : null;

                exp.Right = new AST.PrimitiveExpression()
                {
                    Value = defaultvalue,
                    Parent = exp,
                    SourceResultType = element.MSCAType
                };
            }
            else if (element.DefaultValue is AST.EmptyArrayCreateExpression)
            {
                var ese = element.DefaultValue as AST.EmptyArrayCreateExpression;
                exp.Right = new AST.EmptyArrayCreateExpression()
                {
                    Parent = exp,
                    SizeExpression = ese.SizeExpression.Clone(),
                    SourceExpression = ese.SourceExpression,
                    SourceResultType = ese.SourceResultType
                };

                TypeLookup[exp.Right] = tvhdl;
            }
            else if (element.MSCAType.IsArrayType() && element.DefaultValue == null)
            {
                exp.Right = new EmptyArrayCreateExpression()
                {
                    Parent = exp,
                    SourceExpression = null,
                    SourceResultType = element.MSCAType,
                    SizeExpression = new MemberReferenceExpression()
                    {
                        Name = element.Name,
                        SourceExpression = null,
                        SourceResultType = element.MSCAType,
                        Target = element
                    }
                };

                if (element.Source is IFieldSymbol)
                    TypeLookup[exp.Right] = TypeScope.GetVHDLType((IFieldSymbol)element.Source, element.MSCAType);
                else if (element.Source is System.Reflection.PropertyInfo)
                    TypeLookup[exp.Right] = TypeScope.GetVHDLType((System.Reflection.PropertyInfo)element.Source);
            }
            else
            {
                exp.Right = new AST.PrimitiveExpression()
                {
                    Value = element.DefaultValue == null ? 0 : element.DefaultValue,
                    Parent = exp,
                    SourceResultType = element.MSCAType
                };

                var primitiveVHDL = TypeScope.GetVHDLType(exp.Right.SourceResultType);
                var n = new[] { typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong) };

                if (primitiveVHDL.IsVHDLSigned || primitiveVHDL.IsVHDLUnsigned)
                    TypeLookup[exp.Right] = VHDLTypes.INTEGER;
                else if (primitiveVHDL.IsFloating)
                    TypeLookup[exp.Right] = primitiveVHDL;
                else if (element.DefaultValue != null && !element.DefaultValue.GetType().IsEnum && element.DefaultValue.GetType() != typeof(bool))
                    TypeLookup[exp.Right] = tvhdl; //VHDLTypes.INTEGER;
            }

            res.UpdateParents();
            if ((tvhdl.IsArray && !(tvhdl.IsStdLogicVector || tvhdl.IsSigned || tvhdl.IsUnsigned)) && !tvhdl.IsNumeric && !tvhdl.IsStdLogicVector && !tvhdl.IsSystemType && (exp.Right is PrimitiveExpression || exp.Right is EmptyArrayCreateExpression))
            {

            }
            else
            {
                VHDLTypeConversion.ConvertExpression(this, null, exp.Right, tvhdl, element.MSCAType, false);
            }

            return res;
        }

        /// <summary>
        /// Gets the left-hand-side value for a reset expression.
        /// </summary>
        /// <returns>The reset expression.</returns>
        /// <param name="element">The element to get the value for.</param>
        public string GetResetExpression(DataElement element)
        {
            var st = GetResetStatement(element) as ExpressionStatement;
            if (st == null)
                return string.Empty;

            var ae = st.Expression as AssignmentExpression;
            if (ae == null)
                return string.Empty;

            if (ae.Right is PrimitiveExpression && ae.Right.SourceResultType.IsEnum())
            {
                var pe = ae.Right as PrimitiveExpression;
                var rs = ae.Right.SourceResultType;
                if (pe.Value == null)
                {
                    var c = rs
                        .GetMembers()
                        .OfType<IFieldSymbol>()
                        .OrderBy(x => x.ConstantValue)
                        .First()
                        .ConstantValue;
                    return new RenderHelper(this, null).RenderExpression(new PrimitiveExpression(c, rs));
                }
            }

            return new RenderHelper(this, null).RenderExpression(ae.Right);
        }

        /// <summary>
        /// Returns all type definitions.
        /// </summary>
        /// <value>The type definitions.</value>
        public IEnumerable<string> TypeDefinitions
        {
            get
            {
                var used = new HashSet<Type>();

                foreach (var p in Network.Processes)
                {
                    if (used.Contains(p.SourceInstance.Instance.GetType()))
                        continue;

                    used.Add(p.SourceInstance.Instance.GetType());
                    foreach (var n in new RenderHelper(this, p).TypeDefinitions)
                        yield return n;
                }
            }
        }

        /// <summary>
        /// Performs a reverse lookup into the dependency graph to find the processes that this process depends on.
        /// </summary>
        /// <returns>The processes that the given instance depends on.</returns>
        /// <param name="p">The process to find the dependencies for.</param>
        public IEnumerable<AST.Process> DependsOn(AST.Process p)
        {
            var self = Simulation.Graph.ExecutionPlan.FirstOrDefault(x => x.Item == p.SourceInstance.Instance);
            if (self != null)
                foreach (var mp in self.Parents)
                {
                    var e = Network.Processes.FirstOrDefault(x => x.SourceInstance.Instance == mp.Item);
                    if (e != null)
                        yield return e;
                }
        }

        /// <summary>
        /// Registers the given custom enum type.
        /// </summary>
        /// <param name="sourcetype">The type of the source.</param>
        /// <param name="enumtype">The VHDL type of the enum.</param>
        /// <param name="value">The value to register.</param>
        public string RegisterCustomEnum(ITypeSymbol sourcetype, VHDLType enumtype, object value)
        {
            Dictionary<string, object> v;
            if (!CustomEnumValues.TryGetValue(enumtype, out v))
                CustomEnumValues[enumtype] = v = new Dictionary<string, object>();

            var name = Naming.ToValidName(string.Format("{0}_sme_extra_{1}", sourcetype.ToDisplayString(), 1));
            v[name] = value;
            return name;
        }

        /// <summary>
        /// Gets the local name of the given bus in the given process.
        /// </summary>
        /// <param name="bus">The given bus.</param>
        /// <param name="process">The given process.</param>
        public string GetLocalBusName(AST.Bus bus, AST.Process process)
        {
            if (process != null && process.LocalBusNames.ContainsKey(bus))
                return process.LocalBusNames[bus];
            return bus.Name;
        }

        /// <summary>
        /// Gets the custom renderer for this instance, or null.
        /// </summary>
        /// <returns>The custom renderer.</returns>
        internal ICustomRenderer GetCustomRenderer(AST.Process process)
        {
            if (CustomRenderers.TryGetValue(process.SourceType, out var customRenderer))
                return customRenderer;
            if (process.SourceType.IsGenericType && CustomRenderers.TryGetValue(process.SourceType.GetGenericTypeDefinition(), out customRenderer))
                return customRenderer;

            return process.SourceInstance.Instance.CustomRenderer as ICustomRenderer;
        }

        /// <summary>
        /// Gets a value indicating if the process has a custom renderer.
        /// </summary>
        /// <param name="process">The process to check.</param>
        public bool HasCustomRenderer(AST.Process process)
        {
            return GetCustomRenderer(process) != null;
        }
    }
}
