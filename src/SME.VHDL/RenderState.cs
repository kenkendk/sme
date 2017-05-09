using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Mono.Cecil;
using SME.AST;
using SME.VHDL.Templates;

namespace SME.VHDL
{
	/// <summary>
	/// Class to encapsulate the state associated with rendering an AST network as VHDL files
	/// </summary>
	public class RenderState
	{
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
		}

		/// <summary>
		/// The network being rendered
		/// </summary>
		public readonly Network Network;

		/// <summary>
		/// Enable this to use the VHDL 2008 features if the VHDL compiler supports it
		/// </summary>
		public readonly bool SUPPORTS_VHDL_2008 = false;

		/// <summary>
		/// Activates explicit selection of the IEEE_1164 concatenation operator
		/// </summary>
		public readonly bool USE_EXPLICIT_CONCATENATION_OPERATOR = true;

		/// <summary>
		/// This makes the array lengths use explicit lengths instead of &quot;(x - 1)&quot;
		/// </summary>
		public readonly bool USE_EXPLICIT_LITERAL_ARRAY_LENGTH = true;

		/// <summary>
		/// This avoids emitting code with SLL and SRL, and uses shift_left() and shift_right() instead
		/// </summary>
		public readonly bool AVOID_SLL_AND_SRL = true;

		/// <summary>
		/// The processes forming the basis of the network
		/// </summary>
		public readonly IEnumerable<IProcess> Processes;

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
		/// Sequence of custom VHDL files to include in the compilation
		/// </summary>
		public readonly IEnumerable<string> CustomFiles;

		/// <summary>
		/// The unique types found in the network
		/// </summary>
		public readonly Mono.Cecil.TypeReference[] Types;

		/// <summary>
		/// A lookup associating an AST node with a VHDL type
		/// </summary>
		public readonly Dictionary<ASTItem, VHDLType> TypeLookup = new Dictionary<ASTItem, VHDLType>();

		/// <summary>
		/// The list of registered temporary variables
		/// </summary>
		public readonly Dictionary<Method, Dictionary<string, Variable>> TemporaryVariables = new Dictionary<Method, Dictionary<string, Variable>>();

		/// <summary>
		/// The type scope used to resolve VHDL types
		/// </summary>
		public readonly VHDLTypeScope TypeScope;

		/// <summary>
		/// Gets the length of a clock pulse period
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
		/// <param name="processes">The processes to parse.</param>
		/// <param name="targetfolder">The folder where the output is stored.</param>
		/// <param name="backupfolder">The folder where backups are stored.</param>
		/// <param name="csvtracename">The name of the CSV trace file.</param>
		/// <param name="customfiles">A list of VHDL files to include in the Makefile, without the VHDL extension</param>
		public RenderState(IEnumerable<IProcess> processes, string targetfolder, string backupfolder = null, string csvtracename = null, IEnumerable<string> customfiles = null)
		{
			Processes = processes;
			TargetFolder = targetfolder;
			BackupFolder = backupfolder;
			CSVTracename = csvtracename;
			CustomFiles = customfiles;

			Network = ParseProcesses.BuildNetwork(processes, true);

			ValidateNetwork(Network);

			TypeScope = new VHDLTypeScope(Network.Processes.First(x => x.MainMethod != null).MainMethod.SourceMethod.Module);

			Types = Network
				.All()
				.OfType<DataElement>()
				.Select(x => x.CecilType)
				.Distinct(new TypeRefComp())
				.ToArray();

			Network.Name = Naming.AssemblyToValidName(Processes);

			Transformations.BuildTransformations.Transform(Network, this);
		}

		/// <summary>
		/// Performs some checks to see if the network uses features that are not supported by the VHDL render
		/// </summary>
		/// <param name="network">The network to validate.</param>
		private static void ValidateNetwork(AST.Network network)
		{
			var sp = network.Processes.SelectMany(x => x.InternalBusses).FirstOrDefault(x => x.IsTopLevelInput || x.IsTopLevelOutput);
			if (sp != null)
				throw new Exception($"Cannot have an internal bus that is also toplevel input or output: {sp.Name}");
		}

		/// <summary>
		/// Renders this instance to files
		/// </summary>
		public void Render()
		{
			BackupExistingTarget(Processes, TargetFolder, BackupFolder);

			var targetTopLevel = Path.Combine(TargetFolder, Naming.AssemblyNameToFileName(Processes));

			File.WriteAllText(targetTopLevel, MergeUserData(new TopLevel(this).TransformText(), targetTopLevel));

			var targetTestBench = Path.Combine(TargetFolder, "TestBench_" + Naming.AssemblyNameToFileName(Processes));
			File.WriteAllText(targetTestBench, MergeUserData(new TracefileTester(this).TransformText(), targetTestBench));

			var targetTypeLib = Path.Combine(TargetFolder, "Types_" + Naming.AssemblyNameToFileName(Processes));
			File.WriteAllText(targetTypeLib, MergeUserData(new CustomTypes(this).TransformText(), targetTypeLib));

			var exportTypeLib = Path.Combine(TargetFolder, "Export_" + Naming.AssemblyNameToFileName(Processes));
			File.WriteAllText(exportTypeLib, MergeUserData(new ExportTopLevel(this).TransformText(), exportTypeLib));

			File.WriteAllText(Path.Combine(TargetFolder, "Makefile"), new GHDL_Makefile(this).TransformText());

			foreach (var p in Network.Processes)
			{
				var rsp = new RenderStateProcess(this, p);
				var targetfile = Path.Combine(TargetFolder, Naming.ProcessNameToFileName(p.SourceInstance));
				File.WriteAllText(targetfile, MergeUserData(new Entity(this, rsp).TransformText(), targetfile));
			}

			foreach (
				var vhdlfile in from f in System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames()
								where
									f.EndsWith(".vhdl", StringComparison.InvariantCultureIgnoreCase)
									&&
								f.StartsWith(typeof(Templates.TopLevel).Namespace + ".", StringComparison.InvariantCultureIgnoreCase)
								select f)
				using (var rs = new System.IO.StreamReader(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(vhdlfile)))
					File.WriteAllText(Path.Combine(TargetFolder, vhdlfile.Substring(typeof(Templates.TopLevel).Namespace.Length + 1)), rs.ReadToEnd());
			
		}

		/// <summary>
		/// Makes a backup of all target files
		/// </summary>
		/// <param name="processes">The processes being rendered.</param>
		/// <param name="targetfolder">The folder where output is stored.</param>
		/// <param name="backupfolder">The folder where backups are stored.</param>
		private void BackupExistingTarget(IEnumerable<IProcess> processes, string targetfolder, string backupfolder)
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

				var s = Path.Combine(targetfolder, Naming.AssemblyNameToFileName(processes));
				if (File.Exists(s))
					File.Copy(s, Path.Combine(backupname, Naming.AssemblyNameToFileName(processes)));


				foreach (var p in processes)
				{
					var source = Path.Combine(targetfolder, Naming.ProcessNameToFileName(p));
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
		/// Merges a newly rendered file with user supplied data from an existing file
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
		/// Returns the VHDL type for a data element
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

			if (element.Source is IMemberDefinition)
				return TypeLookup[element] = TypeScope.GetVHDLType(element.Source as IMemberDefinition, element.CecilType);
			else if (element.Source is ParameterDefinition)
				return TypeLookup[element] = TypeScope.GetVHDLType(element.Source as ParameterDefinition);
			else if (element.Source is System.Reflection.PropertyInfo)
				return TypeLookup[element] = TypeScope.GetVHDLType(element.Source as System.Reflection.PropertyInfo);
			else
				return TypeLookup[element] = TypeScope.GetVHDLType(element.CecilType);
		}

		/// <summary>
		/// Returns the VHDL type for a data element
		/// </summary>
		/// <returns>The VHDL type.</returns>
		/// <param name="element">The element to get the type for.</param>
		public VHDLType VHDLType(AST.Method element)
		{
			return VHDLType(element.ReturnVariable);
		}

		/// <summary>
		/// Wraps the VHDL typename if the type is an array type
		/// </summary>
		/// <returns>The wrapped type name.</returns>
		/// <param name="element">The element to wrap.</param>
		public string VHDLWrappedTypeName(AST.DataElement element)
		{
			var vt = VHDLType(element);
			if (element.CecilType.IsArrayType())
			{
				if (element.Parent is AST.Bus)
					return element.Parent.Name + "_" + element.Name + "_type";
				
				return element.Name + "_type";
			}

			return vt.ToSafeVHDLName();
		}

		/// <summary>
		/// Wraps the VHDL typename if the type is an array type
		/// </summary>
		/// <returns>The wrapped type name.</returns>
		/// <param name="fd">The Field to wrap.</param>
		public string VHDLWrappedTypeName(Mono.Cecil.FieldDefinition fd)
		{
			var vt = TypeScope.GetVHDLType(fd);
			if (fd.FieldType.IsArrayType())
			{
				return fd.Name + "_type";
			}

			return vt.ToSafeVHDLName();
		}

		/// <summary>
		/// Returns the VHDL type for an expression
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

				if (vleft == vright)
					return vleft;

				return TypeLookup[element] = TypeScope.GetVHDLType(element.SourceResultType);
			}
			else if (element is UnaryOperatorExpression)
			{
				var uoe = element as UnaryOperatorExpression;
				if (uoe.Operator == ICSharpCode.NRefactory.CSharp.UnaryOperatorType.Not)
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
		/// Gets the default value for an item, expressed as a VHDL expression
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

			if (element.Source is Mono.Cecil.PropertyDefinition)
			{
				var pd = element.Source as Mono.Cecil.PropertyDefinition;
				var init = pd.GetAttribute<InitialValueAttribute>();

				if (init != null && init.HasConstructorArguments)
				{
					pval = init.ConstructorArguments.First().Value;
					if (pval is CustomAttributeArgument)
						pval = ((CustomAttributeArgument)pval).Value;
				}

				if (pd.PropertyType.IsType<bool>())
					return ((object)true).Equals(pval) ? "'1'" : "'0'";
				else if (pd.PropertyType.Resolve().IsEnum)
				{
					if (pval == null)
						return Naming.ToValidName(pd.PropertyType.FullName + "." + pd.PropertyType.Resolve().Fields.Skip(1).First().Name);
					else
						return Naming.ToValidName(pd.PropertyType.FullName + "." + pd.PropertyType.Resolve().Fields.Where(x => pval.Equals(x.Constant)).First().Name);
				}
			}

			var tvhdl = VHDLType(element);
			var elvhdl = tvhdl;
			while (elvhdl.IsArray && !elvhdl.IsStdLogicVector)
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
		/// Gets a list of custom types to implement
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
					.OfType<BusSignal>()
					.Cast<DataElement>()
					.Union(Network
						   .All()
						   .OfType<AST.Process>()
						   .SelectMany(x =>
									   x
									   .SharedSignals
									   .Cast<DataElement>()
									   .Union(x
											  .SharedVariables
											  .Cast<DataElement>()
											 )
									  )
						  )
					.Where(x =>
					{
						var rd = x.CecilType.Resolve();
						var custom = rd.CustomAttributes.Any(y => y.AttributeType.IsSameTypeReference(typeof(VHDLTypeAttribute)));

						return
							(!custom)
							   &&
							   (rd.IsEnum || (rd.IsValueType && !rd.IsPrimitive));
					})
					.Select(x => VHDLType(x))
					.Select(x => x.IsArray && !x.IsStdLogicVector ? TypeScope.GetByName(x.ElementName) : x)
					.Where(x => !ignores.ContainsKey(x.ToString()))
					.Distinct();
			}
		}

		/// <summary>
		/// Gets all arrays used in bus signals
		/// </summary>
		/// <value>The bus arrays.</value>
		public IEnumerable<BusSignal> BusArrays
		{
			get
			{
				var res = Network
					.All()
					.OfType<BusSignal>()
					.Where(x => x.CecilType.IsFixedArrayType())
					.Distinct();
				
				return res;
			}
		}

		/// <summary>
		/// Gets the length of an array attached to a bus
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
		/// Lists all members for a given value type
		/// </summary>
		/// <returns>The member VHDL strings.</returns>
		/// <param name="type">The VHDL type.</param>
		public IEnumerable<string> ListMembers(VHDLType type)
		{
			var td = type.SourceType.Resolve();

			if (td.IsEnum)
			{
				yield return string.Format(
					"({0});",
					string.Join("," + Environment.NewLine + "     ",
						td.Fields.Where(x => x.Name != "value__").Select(m => Naming.ToValidName(td.FullName + "_" + m.Name)))
				);
			}
			else if (td.IsValueType && !td.IsPrimitive)
			{
				yield return "record";

				foreach (var m in td.Fields)
					if (!m.IsStatic)
						yield return string.Format("    {0}: {1};", Naming.ToValidName(m.Name), VHDLWrappedTypeName(m));


				yield return "end record;";
			}
		}

		/// <summary>
		/// Gets all enum types in the network
		/// </summary>
		public IEnumerable<VHDLType> EnumTypes
		{
			get
			{
				return Types
					.Where(x => x.Resolve().IsEnum)
					.Select(x => TypeScope.GetVHDLType(x));

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
					Exception nex = null;
					string convm = null;
					try
					{
						if (nx is ArrayCreateExpression)
						{
							var eltype = n.CecilType.GetElementType();
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
							if (nx is ICSharpCode.NRefactory.CSharp.ArrayCreateExpression)
							{
								var arc = nx as ICSharpCode.NRefactory.CSharp.ArrayCreateExpression;

								var varname = Naming.ToValidName(n.Name);
								var eltype = n.CecilType.GetElementType();
								var vhdl_eltype = TypeScope.GetVHDLType(eltype);

								var ellength = arc.Initializer.Elements.Count;
								var eltrail = " - 1";
								if (USE_EXPLICIT_LITERAL_ARRAY_LENGTH)
								{
									ellength--;
									eltrail = "";
								}

								yield return string.Format("type {0}_type is array (0 to {1}{3}) of {2}", varname, ellength, vhdl_eltype, eltrail);

								string values;
								if (new[] { typeof(sbyte), typeof(byte), typeof(ushort), typeof(short), typeof(int) }.Select(x => eltype.Module.Import(x).Resolve()).Contains(eltype.Resolve()))
									values = string.Join(", ", arc.Initializer.Elements.Select(x => string.Format("{0}({1})", convm, x)));
								else
								{
									if (eltype.Resolve() == eltype.Module.Import(typeof(uint)).Resolve())
										values = string.Join(", ", arc.Initializer.Elements.Select(x => string.Format("\"{1}\"", convm, Convert.ToString((uint)(x as ICSharpCode.NRefactory.CSharp.PrimitiveExpression).Value, 2).PadLeft(32, '0'))));
									else if (eltype.Resolve() == eltype.Module.Import(typeof(long)).Resolve())
										values = string.Join(", ", arc.Initializer.Elements.Select(x => string.Format("\"{1}\"", convm, Convert.ToString((long)(x as ICSharpCode.NRefactory.CSharp.PrimitiveExpression).Value, 2).PadLeft(64, '0'))));
									/*else if (eltype.Resolve() == eltype.Module.Import(typeof(ulong)).Resolve())
										values = string.Join(", ", arc.Initializer.Elements.Select(x => string.Format("{0}({1})", convm, Convert.ToString((ulong)(x as ICSharpCode.NRefactory.CSharp.PrimitiveExpression).Value, 2).PadLeft(64, '0'))));*/
									else
										values = " ??? unsupported type ??? ";
								}

								yield return string.Format("constant {0}: {0}_type := ({1})", varname, values);

							}
							else if (nx is AST.ArrayCreateExpression)
							{
								var arc = nx as AST.ArrayCreateExpression;

								var varname = Naming.ToValidName(n.Name);
								var eltype = n.CecilType.GetElementType();
								var vhdl_eltype = TypeScope.GetVHDLType(eltype);

								var ellength = arc.ElementExpressions.Length;
								var eltrail = " - 1";
								if (USE_EXPLICIT_LITERAL_ARRAY_LENGTH)
								{
									ellength--;
									eltrail = "";
								}

								yield return string.Format("type {0}_type is array (0 to {1}{3}) of {2}", varname, ellength, vhdl_eltype, eltrail);

								string values;
								if (new[] { typeof(sbyte), typeof(byte), typeof(ushort), typeof(short), typeof(int) }.Select(x => eltype.Module.Import(x).Resolve()).Contains(eltype.Resolve()))
									values = string.Join(", ", arc.ElementExpressions.Select(x => string.Format("{0}({1})", convm, (x as AST.PrimitiveExpression).Value)));
								else
								{
									if (eltype.Resolve() == eltype.Module.Import(typeof(uint)).Resolve())
										values = string.Join(", ", arc.ElementExpressions.Select(x => string.Format("\"{1}\"", convm, Convert.ToString((uint)(x as AST.PrimitiveExpression).Value, 2).PadLeft(32, '0'))));
									else if (eltype.Resolve() == eltype.Module.Import(typeof(long)).Resolve())
										values = string.Join(", ", arc.ElementExpressions.Select(x => string.Format("\"{1}\"", convm, Convert.ToString((long)(x as AST.PrimitiveExpression).Value, 2).PadLeft(64, '0'))));
									/*else if (eltype.Resolve() == eltype.Module.Import(typeof(ulong)).Resolve())
										values = string.Join(", ", arc.Initializer.Elements.Select(x => string.Format("{0}({1})", convm, Convert.ToString((ulong)(x as ICSharpCode.NRefactory.CSharp.PrimitiveExpression).Value, 2).PadLeft(64, '0'))));*/
									else
										values = " ??? unsupported type ??? ";
								}

								yield return string.Format("constant {0}: {0}_type := ({1})", varname, values);
							}
							else if (nx is AST.EmptyArrayCreateExpression)
							{
								var arc = nx as AST.EmptyArrayCreateExpression;

								var varname = Naming.ToValidName(n.Name);
								var eltype = n.CecilType.GetElementType();
								var vhdl_eltype = TypeScope.GetVHDLType(eltype);

								var ellength = (int)((AST.PrimitiveExpression)arc.SizeExpression).Value;
								var eltrail = " - 1";
								if (USE_EXPLICIT_LITERAL_ARRAY_LENGTH)
								{
									ellength--;
									eltrail = "";
								}

								yield return string.Format("type {0}_type is array (0 to {1}{3}) of {2}", varname, ellength, vhdl_eltype, eltrail);
							}
							else
							{
								yield return string.Format("constant {0}: {1} := {2}({3})", Naming.ToValidName(n.Name), VHDLType(n), convm, nx);
							}
						}
						else
							yield return string.Format("-- constant {0}: {1} := ???", Naming.ToValidName(n.Name), VHDLType(n));
					}

				}

			}
		}

		/// <summary>
		/// Returns all signals in the network
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
		private IEnumerable<BusSignal> SplitArray(BusSignal signal)
		{
			if (!signal.CecilType.IsArrayType())
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
						CecilType = signal.CecilType.GetArrayElementType(),
						Parent = signal.Parent,
						Name = string.Format("{0}({1})", signal.Name, i)
					};
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
			var name = st.Name + "." + s.Name;
			if (st.DeclaringType != null)
				name = st.DeclaringType.Name + "." + name;
			
			return name;
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
					          .OrderBy(x => TestBenchSignalName(x))
					          .SelectMany(x => SplitArray(x));
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
							  .OrderBy(x => TestBenchSignalName(x))
							  .SelectMany(x => SplitArray(x));
			}
		}

		/// <summary>
		/// Creates a new variable in the process space
		/// </summary>
		/// <returns>The temporary variable to create.</returns>
		/// <param name="variabletype">The type of the variable to use.</param>
		public Variable RegisterTemporaryVariable(Method method, TypeReference variabletype)
		{
			Dictionary<string, Variable> table;
			if (!TemporaryVariables.TryGetValue(method, out table))
				TemporaryVariables[method] = table = new Dictionary<string, Variable>();

			object def = null;
			if (variabletype.IsValueType && Type.GetType(variabletype.FullName) != null)
				def = Activator.CreateInstance(Type.GetType(variabletype.FullName));

			var name = "local_var_" + table.Count.ToString();
			return table[name] = new Variable()
			{
				Name = name,
				CecilType = variabletype,
				Parent = method,
				DefaultValue = def
			};
		}

		/// <summary>
		/// Gets all bus instances that require a feedback loop
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
		/// Returns all signals written to a bus from within the process
		/// </summary>
		/// <returns>The written signals.</returns>
		/// <param name="proc">The process to see if the signals are written</param>
		/// <param name="bus">The bus to get the signals for.</param>
		public IEnumerable<BusSignal> WrittenSignals(AST.Process proc, AST.Bus bus)
		{
			// Components are assumed to use their outputs
			if (proc.SourceInstance is IVHDLComponent)
				return bus.Signals;

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
				.Distinct();
		}
	}
}
