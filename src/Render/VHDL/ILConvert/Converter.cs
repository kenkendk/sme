using SME;
using System;
using System.Linq;
using System.Text;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.Decompiler;
using Mono.Cecil;
using System.Collections.Generic;
using SME.Render.VHDL.ILConvert;
using ICSharpCode.NRefactory.TypeSystem;
using SME.Render.VHDL.ILConvert.AugmentedExpression;

namespace SME.Render.VHDL.ILConvert
{
	public class Converter<T> : Converter
	{
		public Converter(GlobalInformation info, int indentation = 0)
			: base(typeof(T), info, indentation)
		{
		}
	}

	public partial class Converter
	{
		private readonly IndentedStringBuilder m_sb;
		private readonly TypeDefinition m_typedef;
		private readonly MethodDefinition m_methoddef;
		private readonly AssemblyDefinition m_asm;
		private readonly DecompilerContext m_context;

		private readonly List<string> m_imports = new List<string>();
		private readonly Dictionary<string, MemberItem> m_busVariableMap = new Dictionary<string, MemberItem>();

		private readonly Dictionary<string, Tuple<TypeReference, VHDLTypeDescriptor>> m_localVariables = new Dictionary<string, Tuple<TypeReference, VHDLTypeDescriptor>>();
		private readonly Dictionary<string, string> m_localRenames = new Dictionary<string, string>();
		private readonly Dictionary<string, TypeReference> m_classVariables = new Dictionary<string, TypeReference>();
		private readonly Dictionary<string, TypeReference> m_signals = new Dictionary<string, TypeReference>();
		private readonly Dictionary<string, object> m_fieldInitializers = new Dictionary<string, object>();
		private readonly Dictionary<PropertyDefinition, string> m_writtenSignals = new Dictionary<PropertyDefinition, string>();
		private readonly Dictionary<PropertyDefinition, string> m_simulationWrittenSignals = new Dictionary<PropertyDefinition, string>();
		private readonly Dictionary<MethodDefinition, Tuple<int, string[]>> m_compiledMethods = new Dictionary<MethodDefinition, Tuple<int, string[]>>();

		private readonly GlobalInformation m_globalInformation;

		public static bool SUPPORTS_VHDL_2008 = false;

		private int m_varcount = 0;

		public TypeDefinition ProcType { get { return m_typedef; } }
		public GlobalInformation Information { get { return m_globalInformation; } }

		public static TypeDefinition LoadType(Type t)
		{			
			var asm = AssemblyDefinition.ReadAssembly(t.Assembly.Location);
			if (asm == null)
				return null;
			
			return
				(from td in 
					from m in asm.Modules
					select m.GetType(t.FullName)
					where td != null
					select td).FirstOrDefault();
		}

		public Converter(IProcess process, GlobalInformation globalInformation, int indentation = 0)
			: this(process.GetType(), globalInformation, indentation)
		{
			
		}

		public Converter(Type process, GlobalInformation globalInformation, int indentation = 0)
			: this(LoadType(process), globalInformation, indentation)
		{
		}


		public TypeReference ImportType<T>()
		{
			return m_asm.MainModule.Import(typeof(T));
		}

		public TypeReference ImportType(Type t)
		{
			return m_asm.MainModule.Import(t);
		}

		public Converter(TypeDefinition process, GlobalInformation globalInformation, int indentation = 0)
		{
			m_globalInformation = globalInformation;

			m_typedef = process;
			m_asm = process.Module.Assembly;

			m_sb = new IndentedStringBuilder(indentation);
			m_context = new DecompilerContext(m_typedef.Module) { CurrentType = m_typedef };



			if (process == null)
			{
				m_sb.AppendFormat("-- Unable to find type {0} in {1}", process.FullName, process.Module.Assembly.Name.FullName);
				m_sb.AppendLine();
				return;
			}

			if (!process.IsAssignableFrom<SimpleProcess>())
			{
				m_sb.AppendFormat("-- Type {0} does not descend from {1}", process.FullName, typeof(SimpleProcess).FullName);
				m_sb.AppendLine();

				/*
				if (process.FullName == "BPUImplementation.MicrocodeDriver")
				{
					foreach (var n in process.NestedTypes)
					{
						Console.WriteLine("{0} : {1}", process.FullName, n.FullName);
						if (n.Name.StartsWith("<"))
						{
							var ct = new DecompilerContext(n.Module) { CurrentType = n };
							var astbuilder = new AstBuilder(ct);
							foreach(var m in n.Methods)
								astbuilder.AddMethod(m);
							astbuilder.RunTransformations();

							Console.WriteLine(astbuilder.SyntaxTree);
						}
					}

					foreach (var n in process.Methods)
					{
						Console.WriteLine("{0} {1}", process.FullName, n.Name);
						if (n.Name.StartsWith("<"))
						{
							var ct = new DecompilerContext(process.Module) { CurrentType = process };
							var astbuilder = new AstBuilder(ct);
							astbuilder.AddMethod(n);
							astbuilder.RunTransformations();

							Console.WriteLine(astbuilder.SyntaxTree);
						}
					}
				}
				*/
				return;
			}

			if (process.GetAttribute<VHDLSuppressOutputAttribute>() != null)
			{
				m_sb.AppendLine("-- Supressed all VHDL Output");
				return;
			}

			if (process.IsAssignableFrom<SimpleProcess>())
				m_methoddef = m_typedef.Methods.Where(x => x.Name == "OnTick" && x.Parameters.Count == 0).FirstOrDefault();


			m_globalInformation.AddTypeDefinition(m_typedef);

			if (m_methoddef == null)
			{
				m_sb.AppendFormat("-- Unable to find method OnTick in {0}", m_typedef.FullName);
				m_sb.AppendLine();
				return;
			}
				
			DoParse();
		}

		private Converter(MethodDefinition m, GlobalInformation globalInformation, int indentation = 0)
		{
			m_globalInformation = globalInformation;
			m_methoddef = m;
			m_typedef = m.DeclaringType;
			m_asm = m_typedef.Module.Assembly;

			m_sb = new IndentedStringBuilder(indentation);
			m_context = new DecompilerContext(m_typedef.Module) { CurrentType = m_typedef };

			m_sb.Indentation += 4;

			// Temporarily register parameters as local vars
			foreach (var n in m_methoddef.Parameters)
				m_localVariables[Renderer.ConvertToValidVHDLName(n.Name)] = new Tuple<TypeReference, VHDLTypeDescriptor>(n.ParameterType, m_globalInformation.VHDLTypes.GetVHDLType(n));

			DoParse();

			// Unregister parameters
			foreach (var n in m_methoddef.Parameters)
				m_localVariables.Remove(Renderer.ConvertToValidVHDLName(n.Name));
			
			m_sb.Indentation -= 4;
		}

		private IEnumerable<string> VHDLMethod
		{
			get
			{
				var method = Renderer.ConvertToValidVHDLName(m_methoddef.Name);

				if (!m_methoddef.IsStatic)
					throw new Exception("Non-static member functions are not yet supported");

				var returntype = m_methoddef.ReturnType.FullName == "System.Void" ? null : Renderer.ConvertToValidVHDLName(Information.VHDLType(m_methoddef.ReturnType));

				var margs = string.Join("; ",
					from n in m_methoddef.Parameters
					let inoutargstr = n.GetVHDLInOut()

					select string.Format(
						"{0}{1}: {2} {3}", 
						string.Equals(inoutargstr, "in", StringComparison.OrdinalIgnoreCase) ? "constant " : "",
						Renderer.ConvertToValidVHDLName(n.Name),
						inoutargstr,
						n.GetAttribute<VHDLRangeAttribute>() != null
						? Renderer.ConvertToValidVHDLName(m_methoddef.Name + "_" + n.Name + "_type")
						: Renderer.ConvertToValidVHDLName(m_globalInformation.VHDLTypes.GetVHDLType(n).ToString())
					));

				var indent = new string(' ', m_sb.Indentation);

				if (string.IsNullOrWhiteSpace(returntype))
					yield return string.Format("{0}procedure {1}({2}) {3} is", indent, method, margs, returntype);
				else
					yield return string.Format("{0}pure function {1}({2}) return {3} is", indent, method, margs, returntype);

				foreach (var n in m_localVariables)
				{
					var decl = string.Format("{0}    variable {1}: {2}", indent, Renderer.ConvertToValidVHDLName(n.Key), n.Value.Item2.ToSafeVHDLName());
					var assign = "";

					yield return decl + assign + ";";
				}

				yield return indent + "begin";

				foreach (var line in VHDLBody)
					yield return line;

				yield return string.Format("{0}end {1};", indent, method);
			}
		}

		public IEnumerable<string> VHDLBody { get { return m_sb.Lines; } }

		public bool IsClockedProcess { get { return m_typedef.GetAttribute<ClockedProcessAttribute>() != null; } }

		public IEnumerable<MemberItem> WrittenProperties(TypeDefinition t)
		{
			var lst = t.GetBusProperties();
			if (t.IsBusType() && m_typedef.GetAttribute<VHDLSuppressBodyAttribute>() == null && m_typedef.GetAttribute<VHDLSuppressOutputAttribute>() == null && m_typedef.GetAttribute<VHDLIgnoreAttribute>() == null)
				lst = lst.Where(x => m_writtenSignals.ContainsKey(x.Item as PropertyDefinition) || m_simulationWrittenSignals.ContainsKey(x.Item as PropertyDefinition));
			return lst;
		}

		public IEnumerable<MemberItem> BusFields
		{
			get
			{
				return
					from f in m_typedef.GetFieldsRecursive()
					let fr = f.FieldType.Resolve()
					where fr != null && f.FieldType.IsBusType()
					select new MemberItem(null, f, m_typedef);
			}
		}

		public IEnumerable<TypeDefinition> InputOnlyBusses
		{
			get
			{
				return 
					from n in InputBusses.Union(ClockedInputBusses)
					where !OutputBusses.Contains(n)
					select n; 
			}
		}

		public IEnumerable<TypeDefinition> OutputOnlyBusses
		{
			get
			{
				return 
					from n in OutputBusses
					where !InputBusses.Union(ClockedInputBusses).Contains(n)
					select n; 
			}
		}

		public IEnumerable<TypeDefinition> InputOutputBusses
		{
			get
			{
				return 
					from n in InputBusses.Union(ClockedInputBusses)
					where OutputBusses.Contains(n)
					select n; 
			}
		}

		private IEnumerable<TypeDefinition> InputBusList
		{
			get
			{
				return
					from n in BusFields
					let attrIn = n.GetAttribute<InputBusAttribute>()
					let attrOut = n.GetAttribute<OutputBusAttribute>()
					let attrInternal = n.GetAttribute<InternalBusAttribute>()
					where attrInternal == null && (attrOut == null || (attrIn == attrOut))
						select n.ItemType.Resolve();

			}
		}


		public IEnumerable<TypeDefinition> InputBusses
		{
			get
			{
				if (IsClockedProcess)
					return new TypeDefinition[0];
				else
					return InputBusList;
			}
		}

		public IEnumerable<TypeDefinition> ClockedInputBusses
		{
			get
			{
				if (!IsClockedProcess)
					return new TypeDefinition[0];
				else
					return InputBusList;
			}
		}


		public IEnumerable<TypeDefinition> OutputBusses
		{
			get
			{
				return
					from n in BusFields
					let attrIn = n.GetAttribute<InputBusAttribute>()
					let attrOut = n.GetAttribute<OutputBusAttribute>()
					let attrInternal = n.GetAttribute<InternalBusAttribute>()
					where attrInternal == null && (attrOut != null || (attrIn == attrOut))
					select n.ItemType.Resolve();
			}
		}

		public IEnumerable<TypeDefinition> InternalBusses
		{
			get
			{
				return
					from n in BusFields
					where n.GetAttribute<InternalBusAttribute>() != null
					select n.ItemType.Resolve();
			}
		}

		public IEnumerable<TypeDefinition> AllExternalBusses
		{
			get
			{				
				return InputBusses.Union(OutputBusses).Union(ClockedInputBusses);
			}
		}

		public IEnumerable<TypeDefinition> ClockedBusses
		{
			get
			{
				return AllExternalBusses.Where(x => x.GetAttribute<ClockedBusAttribute>() != null);
			}
		}


		public IEnumerable<string> VHDLVariables 
		{ 
			get 
			{ 
				return  
					(from n in m_classVariables
						let member = new VHDLIdentifierExpression(this, new IdentifierExpression(n.Key)).ResolvedItem
						where member.GetAttribute<VHDLIgnoreAttribute>() == null
							select string.Format("variable {0} : {1}", 
					      	Renderer.ConvertToValidVHDLName(n.Key),
                            member.ItemType.IsArray 
	                            ? Renderer.ConvertToValidVHDLName(member.Name + "_type")
								: VHDLType(member.Item)))
					
					.Union(
					from n in m_localVariables
					  select string.Format("variable {0} : {1}", 
						    Renderer.ConvertToValidVHDLName(n.Key), 
							n.Value.Item2.ToSafeVHDLName())
				);
			} 
		}
		public IEnumerable<KeyValuePair<string, MemberItem>> VHDLSignals 
		{ 
			get 
			{ 
				return 
					from n in m_signals
					let member = new VHDLIdentifierExpression(this, new IdentifierExpression(n.Key)).ResolvedItem
				 	where member.GetAttribute<VHDLIgnoreAttribute>() == null
						select new KeyValuePair<string, MemberItem>(n.Key, member);
			} 
		}

		public IEnumerable<string> VHDLProcessResetStaments
		{
			get
			{
				if (m_typedef != null)
				{
					var outputbusses = 
						from n in m_typedef.Fields
						where n.FieldType.IsBusType() && n.HasOutputBusAttribute()
						select n;

					var internalbusses = 
						from n in m_typedef.Fields
						where n.FieldType.IsBusType() && n.HasInternalBusAttribute()
						select n;
				
					foreach (var bus in outputbusses.Union(internalbusses))
					{
						var iftype = bus.FieldType.Resolve().IsInterface ? bus.FieldType.Resolve() : bus.FieldType.Resolve().Interfaces.Where(x => x.IsAssignableFrom<IBus>()).FirstOrDefault().Resolve();

						foreach (var signal in WrittenProperties(iftype))
							yield return ResolveMemberReset(signal, bus.Name);
					}

					foreach (var s in m_signals.Keys.Union(m_classVariables.Keys))
						yield return ResolveMemberReset(new VHDLIdentifierExpression(this, new IdentifierExpression(s)).ResolvedItem, null);

					foreach (var v in m_localVariables)
						yield return ResolveResetStatement(new IdentifierExpression(v.Key));
				}
			}
		}

		public IEnumerable<string> VHDLClockResetStaments
		{
			get
			{
				if (m_typedef != null)
				{
					var internalbusses = 
						from n in m_typedef.Fields
						where n.FieldType.IsBusType() && n.HasInternalBusAttribute()
						select n;
				
					foreach (var bus in internalbusses)
					{
						var iftype = bus.FieldType.Resolve().IsInterface ? bus.FieldType.Resolve() : bus.FieldType.Resolve().Interfaces.Where(x => x.IsAssignableFrom<IBus>()).FirstOrDefault().Resolve();

						foreach (var signal in iftype.Properties)
							yield return ResolveMemberReset(new MemberItem(null, signal, iftype), bus.Name).Substring("next_".Length);
					}
				}
			}
		}
			
		public IEnumerable<string> TypeDefinitions
		{
			get
			{
				foreach (var m in m_fieldInitializers)
				{
					var vhdlexpr = new VHDLIdentifierExpression(this, new IdentifierExpression(m.Key));
					var resolvedItem = vhdlexpr.ResolvedItem;

					var val = m.Value;

					if (val is IMemberDefinition)
					{
						var mr = val as IMemberDefinition;
						if (mr is FieldDefinition &&  m_globalInformation.Constants.ContainsKey(mr as FieldDefinition))
							val = mr.ToVHDLName(mr.DeclaringType, null);
						else if (mr.DeclaringType == m_typedef)
							val = Renderer.ConvertToValidVHDLName(mr.Name);
						else
							val = mr.ToVHDLName(m_typedef, null);

						val = string.Format("TO_INTEGER(UNSIGNED({0}))", val);
					}

					var vhdltype = resolvedItem.ItemType;

					if (vhdltype.IsArray)
					{
						if (val is ArrayCreateExpression)
							val = (val as ArrayCreateExpression).Initializer.Children.Count();

						vhdltype = vhdltype.GetElementType();

						var vlt = m_globalInformation.VHDLTypes.GetVHDLType(vhdltype);
						if (vlt.IsSystemType)
						{
							yield return string.Format("subtype {0} is {2}_ARRAY(0 to {1} - 1)", Renderer.ConvertToValidVHDLName(m.Key + "_type"), val, vlt.ToString());
						}
						else
						{
							yield return string.Format("type {0} is array(0 to {1} - 1) of {2}", Renderer.ConvertToValidVHDLName(m.Key + "_type"), val, vlt.ToString());
						}
					}
				}

				foreach (var m in m_compiledMethods)
				{
					foreach (var p in m.Key.Parameters)
					{
						if (p.ParameterType.IsArray)
						{
							var argrange = p.GetAttribute<VHDLRangeAttribute>();
							var vhdltype = m_globalInformation.VHDLTypes.GetVHDLType(p.ParameterType.GetElementType());
							if (vhdltype.IsSystemType)
							{
								if (argrange != null)
									yield return string.Format("subtype {0} is {3}_ARRAY({1} to {2} - 1)", Renderer.ConvertToValidVHDLName(m.Key.Name + "_" + p.Name + "_type"), argrange.ConstructorArguments.Count == 2 ? (int)argrange.ConstructorArguments.First().Value : 0, (int)argrange.ConstructorArguments.Last().Value, vhdltype.ToString());
								continue;
							}
								
							if (argrange == null)
								yield return string.Format("type {0} is array(natural range <>) of {1}", Renderer.ConvertToValidVHDLName(m.Key.Name + "_" + p.Name + "_type"), vhdltype.ToString());
							else
								yield return string.Format("type {0} is array({1} to {2} - 1) of {3}", Renderer.ConvertToValidVHDLName(m.Key.Name + "_" + p.Name + "_type"), argrange.ConstructorArguments.Count == 2 ? (int)argrange.ConstructorArguments.First().Value : 0, (int)argrange.ConstructorArguments.Last().Value, vhdltype.ToString());
						}
					}
				}
			}
		}

		public IEnumerable<IEnumerable<string>> Methods
		{
			get
			{
				foreach (var n in m_compiledMethods.Where(x => x.Value != null).OrderByDescending(x => x.Value.Item1))
					yield return n.Value.Item2;
					
				foreach (var n in m_compiledMethods.Where(x => x.Value == null))
					yield return CompileMethod(n.Key);
			}
		}


		public static object GetDefaultValue(TypeReference t)
		{
			if (t.IsArray)
				return null;

			if (IsSigned(t))
				return 0;
			else if (IsUnsigned(t))
				return 0u;
			else if (t.IsType<bool>())
				return false;
			else if (t.Resolve().IsEnum)
			{
				var prop = t.Resolve().Fields.Skip(1).First();
				return new MemberReferenceExpression(new TypeReferenceExpression(AstType.Create(prop.DeclaringType.FullName)), prop.Name);
			}
			else
				return null;
		}

		public static string GetDefaultInitializer(GlobalInformation information, TypeReference vartype, IMemberDefinition member)
		{
			VHDLTypeDescriptor vhdltype;
			TypeReference tr = vartype;

			if (member != null)
			{
				vhdltype = information.VHDLTypes.GetVHDLType(member, tr);
			}
			else
			{
				vhdltype = information.VHDLTypes.GetVHDLType(tr);
			}

			var b = "{0}";
			while (vhdltype.IsArray)
			{
				b = string.Format("(others => {0})", b);
				vhdltype = information.VHDLTypes.GetByName(vhdltype.ElementName);
			}

			if (vhdltype.IsStdLogic)
				return string.Format(b, "'0'");
			else
				return null;
			// TODO: GetDefaultValue(vhdltype);
		}

		public string ResolveMemberReset(MemberItem signal, string varname)
		{
			var itemtype = signal.ItemType;
			var vhdltype = m_globalInformation.VHDLTypes.GetVHDLType(signal.Item, signal.ItemType);
			object initialvalue = null;

			var attr = signal.GetAttribute<InitialValueAttribute>();
			if (attr != null && attr.HasConstructorArguments)
			{
				// Weirdness ...
				var f = attr.ConstructorArguments.First();
				if (f.Value is CustomAttributeArgument)
					f = (CustomAttributeArgument)f.Value;

				if (f.Value != null)
				{
					initialvalue = f.Value;

					if (itemtype.Resolve().IsEnum && initialvalue is int)
					{
						var prop = itemtype.Resolve().Fields.Where(x => x.HasConstant && x.Constant is int && x.Constant.Equals(initialvalue)).FirstOrDefault();
						if (prop == null)
							throw new Exception(string.Format("Unable to locate enum {0} with value {1}", itemtype.FullName, initialvalue));

						initialvalue = new MemberReferenceExpression(new TypeReferenceExpression(AstType.Create(prop.DeclaringType.FullName)), prop.Name);
					}
				}
			}
			else if (m_fieldInitializers.ContainsKey(signal.Name) && (vhdltype.IsSystemType || vhdltype.IsArray))
			{				
				initialvalue = m_fieldInitializers[signal.Name];
				if (vhdltype.IsArray && !(initialvalue is ArrayCreateExpression))
					initialvalue = null;
			}

			var target = string.IsNullOrWhiteSpace(varname) ?
				new MemberReferenceExpression(new ThisReferenceExpression(), signal.Name)
				:
				new MemberReferenceExpression(new MemberReferenceExpression(new ThisReferenceExpression(), varname), signal.Name);


			return ResolveResetStatement(target, initialvalue);
		}

		private string ResolveResetStatement(Expression target, object initialvalue = null)
		{
			var tg = ResolveExpression(target);
			var itemtype = tg.ResolvedSourceType;
			initialvalue = initialvalue ?? GetDefaultValue(itemtype);

			if (initialvalue == null)
			{
				var assignoperator = IsVariableExpression(tg) ? ":=" : "<=";

				string definit = null;
				var tgtstr = tg.ResolvedString;
				if (tg is VHDLMemberReferenceExpression)
				{
					var mr = (tg as VHDLMemberReferenceExpression).Member;
					if (mr.GetAttribute<VHDLIgnoreAttribute>() != null)
						return "-- " + target.ToString();


					if (mr.DeclaringType.IsBusType() && !this.IsClockedProcess)
					{
						if (mr.GetAttribute<InternalBusAttribute>() != null)
							tgtstr = "next_" + tg.ResolvedString;
						else // if ((s.Left as MemberReferenceExpression).Target is IdentifierExpression)
						{
							mr = ResolveMemberReference((target as MemberReferenceExpression).Target);
							if (mr.GetAttribute<InternalBusAttribute>() != null)
								tgtstr = "next_" + tg.ResolvedString;
						}
					}

					definit = GetDefaultInitializer(m_globalInformation, itemtype, mr.Item);
				}

				if (string.IsNullOrWhiteSpace(definit))
					definit = GetDefaultInitializer(m_globalInformation, itemtype, null);

				if (definit == null)
					return string.Format("-- {0} {1} ???", tgtstr, assignoperator);
				else
					return string.Format("{0} {1} {2}", tgtstr, assignoperator, definit);
			}
			else
			{
				return ResolveExpression(
					new AssignmentExpression(
						target,
						initialvalue is Expression ? (initialvalue as Expression).Clone() : new PrimitiveExpression(initialvalue)
					)
				).ResolvedString;
			}
		}

		private void ParseConstructor()
		{
			ParseConstructor(m_typedef.Methods.Where(x => x.IsConstructor && !x.HasParameters && !x.IsStatic).FirstOrDefault(), m_fieldInitializers, false);
		}

		public Dictionary<string, object> ParseStaticConstructor()
		{
			var res = new Dictionary<string, object>();
			ParseConstructor(m_typedef.Methods.Where(x => x.IsConstructor && !x.HasParameters && x.IsStatic).FirstOrDefault(), res, true);
			return res;
		}

		private void ParseConstructor(MethodDefinition condef, Dictionary<string, object> res, bool statics)
		{
			var astbuilder = new AstBuilder(m_context);
			if (condef != null)
			{
				astbuilder.AddMethod(condef);
				astbuilder.RunTransformations();

				var connode = astbuilder.SyntaxTree.Children.Where(x => x.NodeType == NodeType.Member).FirstOrDefault() as ConstructorDeclaration;
				if (connode != null)
				{
					foreach (var s in from n in connode.Body.Statements where n is ExpressionStatement select (n as ExpressionStatement).Expression)
					{
						if (s is AssignmentExpression)
						{
							try
							{
								var ae = s as AssignmentExpression;
								var lhs = ResolveMemberReference(ae.Left);

								if (lhs.Type == MemberItem.MemberType.Field && (lhs.Item as FieldDefinition).IsStatic == statics)
								{
									if (lhs.ItemType.IsArray && ae.Right is ArrayCreateExpression)
									{
										var arc = ae.Right as ArrayCreateExpression;
										var count = arc.Arguments.FirstOrDefault();
										while (count is CastExpression)
											count = (count as CastExpression).Expression;

										if (count == null)
										{
											res.Add(lhs.Name, arc);
											continue;
										}
										else if (count is PrimitiveExpression)
										{
											res.Add(lhs.Name, (count as PrimitiveExpression).Value);
											continue;
										}
										else if (count is IdentifierExpression || count is MemberReferenceExpression)
										{
											res.Add(lhs.Name, ResolveMemberReference(count));
											continue;
										}
									}
									else if (ae.Right is PrimitiveExpression)
									{
										res.Add(lhs.Name, (ae.Right as PrimitiveExpression).Value);
										continue;
									}
								}
							}
							catch
							{
							}

							m_sb.AppendLine(string.Format("-- Failed to parse constructor statement: {0}", s.ToString().Replace(Environment.NewLine, " ")));
						}
						else if (s is InvocationExpression)
						{
							if (s.ToString() != "base..ctor ()")
								m_sb.AppendLine(string.Format("-- Unparsed to parse constructor statement: {0}", s));
						}
						else
						{
							m_sb.AppendLine(string.Format("-- Unparsed constructor statement: {0}", s));
						}
					}	
				}
			}
		}

		private void DoParse()
		{
			var statics = ParseStaticConstructor();

			foreach (var f in m_methoddef.DeclaringType.Fields)
				if (!f.FieldType.IsBusType())
				{
					if (f.IsLiteral)
						m_globalInformation.Constants[f] = f.Constant;
					else if (f.IsStatic && f.IsInitOnly && statics.ContainsKey(f.Name))
						m_globalInformation.Constants[f] = statics[f.Name];
					else if (f.IsStatic)
						continue; // Don't care
					else if (f.GetAttribute<VHDLSignalAttribute>() == null)
						m_classVariables.Add(f.Name, f.FieldType);
					else
						m_signals.Add(f.Name, f.FieldType);
				}


			ParseConstructor();

			var astbuilder = new AstBuilder(m_context);
			astbuilder.AddMethod(m_methoddef);
			astbuilder.RunTransformations();
			var sx = astbuilder.SyntaxTree;

			foreach (var s in sx.Members.Where(x => x is UsingDeclaration).Cast<UsingDeclaration>())
				m_imports.Add(s.Import.ToString());

			var methodnode = sx.Members.Where(x => x is MethodDeclaration).FirstOrDefault() as MethodDeclaration;
			if (methodnode == null)
			{
				m_sb.AppendFormat("-- Failed to locate body in {0}", m_typedef.FullName);
				m_sb.AppendLine();
				return;
			}
				
			//Console.WriteLine(methodnode);

			if (m_typedef.GetAttribute<VHDLSuppressBodyAttribute>() != null)
			{
				m_sb.AppendLine("-- Supressed VHDL Body content");
			}
			else
			{
				this.ReturnVariable = m_methoddef.ReturnType.FullName == "System.Void" ? null : RegisterTemporaryVariable(m_methoddef.ReturnType);

				foreach (var n in methodnode.Body.Children)
					if (n.NodeType == NodeType.Statement)
					{
						try
						{
							OutputStatement(n);
						}
						catch (Exception ex)
						{
							//throw new Exception(string.Format("Failed while processing expression {0}, message was: {1}", n, ex.Message), ex);
							Console.WriteLine("Failed to process statement: {0} -> {1}", n, ex);
							foreach (var line in n.ToString().Split(new [] { Environment.NewLine }, StringSplitOptions.None))
							{
								if (!string.IsNullOrWhiteSpace(line))
									m_sb.AppendFormat("-- {0}", line);
								m_sb.AppendLine();
							}
						}
					}
					else
						throw new Exception(string.Format("Unsupported construct: {0}", n));

				while (true)
				{
					var newmethods = m_compiledMethods.Where(x => x.Value == null).Select(x => x.Key).ToArray();
					foreach (var c in newmethods)
					{
						var compiled = new Converter(c, m_globalInformation);
						m_compiledMethods[c] = new Tuple<int, string[]>(0, compiled.VHDLMethod.ToArray());

						foreach (var m in compiled.m_compiledMethods)
							if (!m_compiledMethods.ContainsKey(m.Key))
								m_compiledMethods[m.Key] = new Tuple<int, string[]>(m.Value.Item1 + 1, m.Value.Item2);
					}

					if (newmethods.Length == 0)
						break;
				}

			}
		}

		private IEnumerable<string> CompileMethod(MethodDefinition mdef)
		{
			return new Converter(mdef, m_globalInformation).VHDLMethod;
		}

		public IVHDLExpression ResolveExpression(Expression s)
		{
			if (s is AssignmentExpression)
				return new VHDLAssignmentExpression(this, s as AssignmentExpression);
			else if (s is IdentifierExpression)
				return new VHDLIdentifierExpression(this, s as IdentifierExpression);
			else if (s is MemberReferenceExpression)
				return new VHDLMemberReferenceExpression(this, s as MemberReferenceExpression);
			else if (s is PrimitiveExpression)
				return new VHDLPrimitiveExpression(this, s as PrimitiveExpression);
			else if (s is BinaryOperatorExpression)
				return new VHDLBinaryOperatorExpression(this, s as BinaryOperatorExpression);
			else if (s is UnaryOperatorExpression)
				return new VHDLUnaryOperatorExpression(this, s as UnaryOperatorExpression);
			else if (s is IndexerExpression)
				return new VHDLIndexerExpression(this, s as IndexerExpression);
			else if (s is CastExpression)
				return new VHDLCastExpression(this, s as CastExpression);
			else if (s is ConditionalExpression)
				return new VHDLConditionalExpression(this, s as ConditionalExpression);
			else if (s is InvocationExpression)
			{
				var si = s as InvocationExpression;
				var mt = si.Target as MemberReferenceExpression;

				// Catch common translations
				if (mt != null && (s as InvocationExpression).Arguments.Count == 1)
				{
					var mtm = new VHDLMemberReferenceExpression(this, mt);
					if (mt.MemberName == "op_Implicit" || mt.MemberName == "op_Explicit")
						return ResolveExpression(new CastExpression(AstType.Create(mtm.ResolvedSourceType.FullName), si.Arguments.First().Clone()));
					else if (mt.MemberName == "op_Increment")
						return ResolveExpression(new UnaryOperatorExpression(UnaryOperatorType.Increment, si.Arguments.First().Clone()));
					else if (mt.MemberName == "op_Decrement")
						return ResolveExpression(new UnaryOperatorExpression(UnaryOperatorType.Decrement, si.Arguments.First().Clone()));
				}

				return new VHDLInvocationExpression(this, s as InvocationExpression);
			}
			else if (s is ParenthesizedExpression)
				return new VHDLParenthesizedExpression(this, s as ParenthesizedExpression);
			else if (s is NullReferenceExpression)
				return new VHDLEmptyExpression(this, s as NullReferenceExpression);
			else if (s is ArrayCreateExpression)
				return new VHDLArrayCreateExpression(this, s as ArrayCreateExpression);
			else if (s is CheckedExpression)
				return new VHDLCheckedExpression(this, s as CheckedExpression);
			else if (s is UncheckedExpression)
				return new VHDLUncheckedExpression(this, s as UncheckedExpression);
			else if (s == Expression.Null)
				return new VHDLEmptyExpression(this, null);
			else
				throw new Exception(string.Format("Unsupported expression: {0} ({1})", s, s.GetType().FullName));			
		}

		public string RegisterTemporaryVariable(TypeReference vartype, VHDLTypeDescriptor vhdltype = null)
		{
			var varname = Renderer.ConvertToValidVHDLName("tmpvar_" + (m_varcount++).ToString());
			m_localVariables.Add(varname, new Tuple<TypeReference, VHDLTypeDescriptor>(vartype, vhdltype ?? m_globalInformation.VHDLTypes.GetVHDLType(vartype)));

			return varname;
		}

		public void RegisterMethodForCompilation(MethodDefinition mdef)
		{
			if (!m_compiledMethods.ContainsKey(mdef))
				m_compiledMethods[mdef] = null;
		}

		public void PostPendline(string line, params object[] args)
		{
			m_sb.PostpendLine(line, args);
		}

		public void PrePendline(string line, params object[] args)
		{
			m_sb.PrependLine(line, args);
		}

		public void RegisterSignalWrite(VHDLMemberReferenceExpression item, bool isSimulation)
		{
			var member = item.Member;

			// Register as written
			if (member.Type == MemberItem.MemberType.Property && member.DeclaringType.IsBusType())
			{
				if (isSimulation)
					m_simulationWrittenSignals[member.Item as PropertyDefinition] = string.Empty;
				else
					m_writtenSignals[member.Item as PropertyDefinition] = string.Empty;
			}
		}

		public IVHDLExpression WrapConverted(IVHDLExpression s, VHDLTypeDescriptor target, bool fromCast = false)
		{
			if (s.VHDLType == target)
				return s;

			if (!s.VHDLType.IsStdLogicVector && !s.VHDLType.IsUnsigned && !s.VHDLType.IsSigned && s.VHDLType.IsArray && target.IsArray && m_globalInformation.VHDLTypes.GetByName(s.VHDLType.ElementName) == m_globalInformation.VHDLTypes.GetByName(target.ElementName))
				return s;

			var targetlengthstr = string.IsNullOrWhiteSpace(target.Alias) ? target.Length.ToString() : target.Alias + "'length";

			if (target == VHDLTypes.SYSTEM_BOOL)
			{
				if (string.Equals("STD_LOGIC", s.VHDLType.Name, StringComparison.OrdinalIgnoreCase))
					return s;
				
				if (s.VHDLType.IsNumeric || s.VHDLType.IsStdLogicVector)
				{
					return ResolveExpression(
						new BinaryOperatorExpression(
							s.Expression.Clone(),
							BinaryOperatorType.InEquality,
							new PrimitiveExpression(0)
						)
					);	
				}
				else if (s.VHDLType == VHDLTypes.BOOL)
				{
					return ResolveExpression(
						new ConditionalExpression(
							s.Expression.Clone(),
							new PrimitiveExpression(true),
							new PrimitiveExpression(false)
						)
					);	

				}
				else
					throw new Exception(string.Format("Unexpected conversion from {0} to {1}", s.VHDLType, target));
			}
			else if (s.VHDLType == VHDLTypes.INTEGER && (target.IsStdLogicVector || target.IsNumeric))
			{
				if (target.IsSigned && target.IsNumeric)
					return new VHDLConvertedExpression(s, target, string.Format("TO_SIGNED({0}, {1})", "{0}", targetlengthstr));
				else if (target.IsUnsigned && target.IsNumeric)
					return new VHDLConvertedExpression(s, target, string.Format("TO_UNSIGNED({0}, {1})", "{0}", targetlengthstr));
				else if (target.IsStdLogicVector)
					return new VHDLConvertedExpression(s, target, string.Format("STD_LOGIC_VECTOR(TO_UNSIGNED({0}, {1}))", "{0}", targetlengthstr));
				else
					throw new Exception(string.Format("Unexpected conversion from {0} to {1}", s.VHDLType, target));
			}
			else if (target.IsNumeric)
			{
				if (s.VHDLType.IsStdLogicVector || s.VHDLType.IsSigned || s.VHDLType.IsUnsigned)
				{
					var str = "{0}";
					var resized = false;
					string tmpvar = null;
					if (target.Length != s.VHDLType.Length)
					{
						if (s.VHDLType.IsVHDLSigned || s.VHDLType.IsVHDLUnsigned)
						{
							resized = true;
							str = string.Format("resize({0}, {1})", str, targetlengthstr);
						}
						else if (target.Length > s.VHDLType.Length)
						{
							// This must be a variable as bit concatenation is only allowed in assignment statements:
							// http://stackoverflow.com/questions/209458/concatenating-bits-in-vhdl

							// TODO: Not correct ResolvedSourceType, should be target
							tmpvar = RegisterTemporaryVariable(s.ResolvedSourceType, m_globalInformation.VHDLTypes.GetStdLogicVector(target.Length));	
							m_sb.PrependLine(string.Format("{0} := \"{1}\" & {2};", tmpvar, new string('0', target.Length - s.VHDLType.Length), s.ResolvedString));

							resized = true;
						}
					}

					if (s.VHDLType.IsVHDLSigned != target.IsSigned || s.VHDLType.IsVHDLUnsigned != target.IsUnsigned)
						str = string.Format("{1}({0})", str, target.IsSigned ? "SIGNED" : "UNSIGNED");

					if (target.Length != s.VHDLType.Length && !resized)
						str = string.Format("resize({0}, {1})", str, targetlengthstr);

					if (tmpvar != null)
						s = ResolveExpression(new IdentifierExpression(tmpvar));
					return new VHDLConvertedExpression(WrapIfComposite(s), target, str);
				}


				/*if (s.VHDLType.IsStdLogicVector && target.IsSigned)
					return new VHDLConvertedExpression(s, target, "SIGNED({0})");
				else if (s.VHDLType.IsStdLogicVector && target.IsUnsigned)
					return new VHDLConvertedExpression(s, target, "UNSIGNED({0})");
				else*/
					throw new Exception(string.Format("Unexpected conversion from {0} to {1}", s.VHDLType, target));
			}
			else if (target.IsStdLogicVector)
			{
				if (s.VHDLType.IsNumeric)
				{
					if (s.VHDLType.Length == target.Length)
						return new VHDLConvertedExpression(s, target, "STD_LOGIC_VECTOR({0})");
					else
					{
						if (!fromCast)
							Console.WriteLine("WARN: Incompatible array lengths, from {0} to {1}", s.VHDLType, target);
							//throw new Exception(string.Format("Incompatible array lengths, from {0} to {1}", s.VHDLType, target));

						return new VHDLConvertedExpression(s, target, string.Format("STD_LOGIC_VECTOR(resize({0}, {1}))", "{0}", targetlengthstr));
					}
						
				}
				else if (s.VHDLType.IsStdLogicVector)
				{
					if (target.Length == s.VHDLType.Length)
						return new VHDLConvertedExpression(s, target, "{0}");

					if (!fromCast)
						Console.WriteLine("WARN: Incompatible array lengths, from {0} to {1}", s.VHDLType, target);
						//throw new Exception(string.Format("Incompatible array lengths, from {0} to {1}", s.VHDLType, target));

					if (target.Length < s.VHDLType.Length)
					{
						// We cannot select bits from a typecast
						// TODO: Dirty to rely on the string, there are likely other cases that need the same wrapping
						if (s.ResolvedString.StartsWith("STD_LOGIC_VECTOR(", StringComparison.OrdinalIgnoreCase))
						{
							var tmp = RegisterTemporaryVariable(s.ResolvedSourceType, s.VHDLType);	
							m_sb.PrependLine(string.Format("{0} := {1};", tmp, s.ResolvedString));

							return new VHDLConvertedExpression(ResolveExpression(new IdentifierExpression(tmp)), target, string.Format("{0}({1} downto 0)", "{0}", target.Length - 1));
						}

						return new VHDLConvertedExpression(s, target, string.Format("{0}({1} downto 0)", "{0}", target.Length - 1));

					}
					else if (s.VHDLType.IsSigned)
						return new VHDLConvertedExpression(s, target, string.Format("STD_LOGIC_VECTOR(resize(SIGNED({0}), {1}))", "{0}", targetlengthstr));
					else if (s.VHDLType.IsUnsigned)
						return new VHDLConvertedExpression(s, target, string.Format("STD_LOGIC_VECTOR(resize(UNSIGNED({0}), {1}))", "{0}", targetlengthstr));
					else
					{
						// TODO: Not correct ResolvedSourceType, should be target
						var tmp = RegisterTemporaryVariable(s.ResolvedSourceType, target);	
						m_sb.PrependLine(string.Format("{0} := \"{1}\" & {2};", tmp, new string('0', target.Length - s.VHDLType.Length), s.ResolvedString));

						return ResolveExpression(new IdentifierExpression(tmp));

						// This must be a variable as bit concatenation is only allowed in assignment statements:
						// http://stackoverflow.com/questions/209458/concatenating-bits-in-vhdl

						//return new VHDLConvertedExpression(s, target, string.Format("\"{1}\" & {0}", "{0}", new string('0', target.Length - s.VHDLType.Length)), true);

					}

				}
				else if (s.VHDLType.IsSigned || s.VHDLType.IsUnsigned)
				{
					if (target.Length == s.VHDLType.Length)
						return new VHDLConvertedExpression(s, target, string.Format("STD_LOGIC_VECTOR({0})", "{0}"));
					else
						return new VHDLConvertedExpression(s, target, string.Format("STD_LOGIC_VECTOR(resize({0}, {1}))", "{0}", targetlengthstr));
				}
				else
					throw new Exception(string.Format("Unexpected conversion from {0} to {1}", s.VHDLType.Name, target.Name));
			}
			else if (target == VHDLTypes.INTEGER && (s.VHDLType.IsStdLogicVector || s.VHDLType.IsNumeric))
			{
				if (s.VHDLType.IsNumeric)
					return new VHDLConvertedExpression(s, target, "TO_INTEGER({0})");

				if (s.VHDLType.IsSigned)
					return new VHDLConvertedExpression(s, target, "TO_INTEGER(SIGNED({0}))");
				else
					return new VHDLConvertedExpression(s, target, "TO_INTEGER(UNSIGNED({0}))");
			}
			else if (target == VHDLTypes.BOOL && s.VHDLType == VHDLTypes.SYSTEM_BOOL)
			{
				return new VHDLConvertedExpression(s, target, "{0} = '1'", true);
			}
			else if ((target.IsSigned || target.IsUnsigned) && s.VHDLType.IsStdLogicVector)
			{
				if (target.Length == s.VHDLType.Length)
					return new VHDLConvertedExpression(s, target, string.Format("{1}({0})", "{0}", target.IsSigned ? "SIGNED" : "UNSIGNED"));
				else
					return new VHDLConvertedExpression(s, target, string.Format("resize({1}({0}), {2})", "{0}", target.IsSigned ? "SIGNED" : "UNSIGNED", targetlengthstr));
			}
			else if ((target.IsSigned || target.IsUnsigned) && s.VHDLType == VHDLTypes.INTEGER)
			{
				if (target.IsSigned)
					return new VHDLConvertedExpression(s, target, string.Format("TO_SIGNED({0}, {1})", "{0}", target.Length));
				else if (target.IsUnsigned)
					return new VHDLConvertedExpression(s, target, string.Format("TO_UNSIGNED({0}, {1})", "{0}", target.Length));
				else
					throw new Exception("Unexpected case");
			}
			else
				throw new Exception(string.Format("Unexpected target type: {0} for source: {1}", target, s.VHDLType));
		}

		public IVHDLExpression WrapIfComposite(IVHDLExpression s)
		{
			if (s is VHDLIndexerExpression || s is VHDLMemberReferenceExpression || s is VHDLPrimitiveExpression || s is VHDLIdentifierExpression || s is VHDLIndexerExpression || s is VHDLInvocationExpression || s is VHDLParenthesizedExpression || s is VHDLCastExpression)
				return s;
			else
			{
				if (s is VHDLConvertedExpression && !(s as VHDLConvertedExpression).NeedsWrapping)
					return s;
				
				return new VHDLConvertedExpression(s, s.VHDLType, "({0})");
			}
		}

			
		public TypeDefinition ResolveType(AstType t)
		{
			if (t is MemberType)
				return ResolveType(t as MemberType);
			else if (t is SimpleType)
				return ResolveType((t as SimpleType).Identifier, m_typedef);
			else if (t is PrimitiveType)
			{
				switch ((t as PrimitiveType).KnownTypeCode)
				{
					case KnownTypeCode.Boolean:
						return ImportType<bool>().Resolve();
					case KnownTypeCode.Byte:
						return ImportType<byte>().Resolve();
					case KnownTypeCode.UInt16:
						return ImportType<ushort>().Resolve();
					case KnownTypeCode.UInt32:
						return ImportType<uint>().Resolve();
					case KnownTypeCode.UInt64:
						return ImportType<ulong>().Resolve();
					case KnownTypeCode.SByte:
						return ImportType<sbyte>().Resolve();
					case KnownTypeCode.Int16:
						return ImportType<short>().Resolve();
					case KnownTypeCode.Int32:
						return ImportType<int>().Resolve();
					case KnownTypeCode.Int64:
						return ImportType<long>().Resolve();
					default:
						throw new Exception(string.Format("Unsupported type: {0}", (t as PrimitiveType).KnownTypeCode));
				}
			}
			else
				throw new Exception(string.Format("Unable to resolve {0} ({1})", t, t.GetType().FullName));
		}

		private TypeDefinition ResolveType(MemberType t)
		{
			TypeDefinition parent;
			if (t.Target is SimpleType)
			{
				var xt = t.Annotation<TypeReference>();

				if (xt is TypeDefinition)
					return xt as TypeDefinition;

				if (xt != null)
				{
					var rs = xt.Resolve();
					if (rs != null)
						return rs;
				}

				if ((t.Target as SimpleType).Identifier == m_typedef.Name)
					return m_typedef.NestedTypes.Where(x => x.Name == t.MemberName).FirstOrDefault();
				else if ((t.Target as SimpleType).Identifier == m_typedef.Namespace)
					return ResolveType(t.MemberName, m_typedef);
				else
					parent = ResolveType((t.Target as SimpleType).Identifier, m_typedef);
			}
			else
				throw new Exception(string.Format("Unable to resolve {0} ({1})", t.Target, t.Target.GetType().FullName));

			var m = parent.NestedTypes.Where(x => x.Name == t.MemberName).FirstOrDefault();
			if (m == null)
				throw new Exception(string.Format("Unable to find {0} in {1} ({2})", t.MemberName, parent, t.GetType().FullName));

			return m;
		}

		private static bool IsSigned(TypeReference tr)
		{
			var signedtypes = new Type[] {
				typeof(sbyte),
				typeof(short),
				typeof(int),
				typeof(long)
			};
			return signedtypes.Any(x => x.FullName == tr.FullName || tr.IsAssignableFrom(x));
		}

		private static bool IsUnsigned(TypeReference tr)
		{
			var signedtypes = new Type[] {
				typeof(byte),
				typeof(ushort),
				typeof(uint),
				typeof(ulong)
			};

			return signedtypes.Any(x => x.FullName == tr.FullName || tr.IsAssignableFrom(x));
		}

		public Tuple<MemberItem, VHDLTypeDescriptor, string> ResolveLocalOrClassIdentifier(string name, Expression exp)
		{
			MemberItem res = null;
			VHDLTypeDescriptor vhdl = null;

			if (m_localRenames.ContainsKey(name))
				name = m_localRenames[name];

			if (m_busVariableMap.ContainsKey(name))
				res = m_busVariableMap[name];
			else if (m_classVariables.ContainsKey(name) || m_signals.ContainsKey(name))
				res = m_globalInformation.StoreType(new MemberItem(exp, m_typedef.Fields.Cast<IMemberDefinition>().Union(m_typedef.Properties).Where(x => x.Name == name).FirstOrDefault(), m_typedef));
			else if (m_localVariables.ContainsKey(name) && exp is IdentifierExpression)
			{
				res = m_globalInformation.StoreType(new MemberItem(exp as IdentifierExpression, m_localVariables[name].Item1));
				vhdl = m_localVariables[name].Item2;
			}
			else
				throw new Exception(string.Format("Unable to resolve identifier: {0} in {1}", name, exp));

			if (vhdl == null)
				vhdl = m_globalInformation.VHDLTypes.GetVHDLType(res.Item, res.ItemType);

			return new Tuple<MemberItem, VHDLTypeDescriptor, string>(res, vhdl, name);
		}
			
			
		private TypeDefinition ResolveType(TypeReferenceExpression s, TypeDefinition scope)
		{
			if (s.Type is SimpleType)
				return ResolveType((s.Type as SimpleType).Identifier, scope);
			else if (s.Type is MemberType)
				return ResolveType(s.Type as MemberType);
			else
				throw new Exception(string.Format("Unable to resolve type for {0} ({1})", s, s.GetType().FullName));
		}

		private TypeDefinition ResolveType(string name, TypeDefinition scope)
		{
			if (name == scope.Name)
				return scope;

			var res = 
				from m in m_asm.Modules
				from t in m.Types
				where t.Name == name
				select t;

			var tr = res.FirstOrDefault();

			if (tr == null)
			{
				tr = (from n in m_imports
					let nt = Type.GetType(n + "." + name)
					where nt != null
					select ImportType(nt).Resolve()).FirstOrDefault();
			}

			if (tr == null)
				throw new Exception(string.Format("Unable to resolve type named {0}", name));
			return tr;
			
		}
			
		public string VHDLType(IMemberDefinition m, TypeDefinition decl = null)
		{
			if (decl != null)
				return m_globalInformation.VHDLType(new MemberItem(null, m, decl));

			return m_globalInformation.VHDLType(m);
		}

		public string VHDLType(MemberItem m)
		{
			return m_globalInformation.VHDLType(m);
		}

		public string VariableVHDLType(MemberItem m)
		{
			return VHDLType(m);
		}
			

		public string ProcessNameToVHDLName()
		{
			var processname = m_typedef.FullName;
			var asmname = m_typedef.Module.Assembly.Name.Name + '.';
			if (processname.StartsWith(asmname))
				processname = processname.Substring(asmname.Length);

			return Renderer.ConvertToValidVHDLName(processname);
		}

		public MemberItem ResolveMemberReferenceToVariable(MemberReferenceExpression s)
		{
			if (s != null && (s.Target is IdentifierExpression) && m_busVariableMap.ContainsKey((s.Target as IdentifierExpression).Identifier))
			{
				var propdef = s.Annotation<PropertyDefinition>();
				var fielddef = s.Annotation<FieldDefinition>();

				var targetpropdef = s.Target.Annotation<PropertyDefinition>();
				var targetfielddef = s.Target.Annotation<FieldDefinition>();
				var decl = targetpropdef == null ? (targetfielddef == null ? null : targetfielddef.FieldType.Resolve()) : targetpropdef.PropertyType.Resolve();

				if (propdef != null)
					return new MemberItem(s, propdef, decl ?? propdef.DeclaringType);
				if (fielddef != null)
					return new MemberItem(s, fielddef, decl ?? fielddef.DeclaringType);
			}

			return null;
		}

		public MemberItem ResolveMemberReference(Expression s)
		{
			if (s is MemberReferenceExpression)
				return ResolveMemberReference(s as MemberReferenceExpression);
			else if (s is IdentifierExpression)
				return ResolveMemberReference(s as IdentifierExpression);
			else
				throw new Exception(string.Format("Unsupported expressionfield: {0} ({1})", s, s.GetType().FullName));
		}

		public MemberItem ResolveMemberReference(IdentifierExpression s)
		{
			if (m_busVariableMap.ContainsKey(s.Identifier))
				return m_busVariableMap[s.Identifier];
			else if (m_classVariables.ContainsKey(s.Identifier) || m_signals.ContainsKey(s.Identifier))
				return m_globalInformation.StoreType(new MemberItem(s, m_typedef.Fields.Cast<IMemberDefinition>().Union(m_typedef.Properties).Where(x => x.Name == s.Identifier).FirstOrDefault(), m_typedef));
			else
				throw new Exception(string.Format("Unable to resolve identifier: {0}", s));
		}

		public MemberItem ResolveMemberReference(MemberReferenceExpression s)
		{
			if (!(s.Target is IdentifierExpression) || !m_busVariableMap.ContainsKey((s.Target as IdentifierExpression).Identifier))
			{
				var propdef = s.Annotation<PropertyDefinition>();
				var fielddef = s.Annotation<FieldDefinition>();

				var targetpropdef = s.Target.Annotation<PropertyDefinition>();
				var targetfielddef = s.Target.Annotation<FieldDefinition>();
				var decl = targetpropdef == null ? (targetfielddef == null ? null : targetfielddef.FieldType.Resolve()) : targetpropdef.PropertyType.Resolve();

				if (propdef != null)
					return m_globalInformation.StoreType(new MemberItem(s, propdef, decl ?? propdef.DeclaringType));
				if (fielddef != null)
					return m_globalInformation.StoreType(new MemberItem(s, fielddef, decl ?? fielddef.DeclaringType));
			}

			var resolvestack = new Stack<MemberReferenceExpression>();
			var cur = s;
			while (cur.Target is MemberReferenceExpression)
			{
				resolvestack.Push(cur.Target as MemberReferenceExpression);
				cur = cur.Target as MemberReferenceExpression;
			}

			// Assume "this"
			TypeReference curtype = m_typedef;

			// If something else, resolve it
			if (cur.Target is TypeReferenceExpression)
			{
				curtype = ResolveType(cur.Target as TypeReferenceExpression, curtype.Resolve());
			}
			else if (cur.Target is IdentifierExpression)
			{
				var name = (cur.Target as IdentifierExpression).Identifier;
				if (m_localRenames.ContainsKey(name))
					name = m_localRenames[name];

				if (m_busVariableMap.ContainsKey(name))
					curtype = m_busVariableMap[name].ItemType;
				else if (m_localVariables.ContainsKey(name))
					curtype = m_localVariables[name].Item1;
				else if (m_classVariables.ContainsKey(name))
					curtype = m_classVariables[name];
				else if (m_signals.ContainsKey(name))
					curtype = m_signals[name];
			}
			else if (cur.Target is BaseReferenceExpression)
			{
				curtype = curtype.Resolve().BaseType;
			}
			else if (!(cur.Target is ThisReferenceExpression))
			{
				throw new Exception(string.Format("Failure while resolving memberreferenceexpression: {0}, did not reference a \"this\" element", s.ToString()));
			}

			while (resolvestack.Count > 0)
			{
				var target = resolvestack.Pop();
				var next = curtype.Resolve().GetFieldRecursive(target.MemberName);
				if (next == null)
					throw new Exception(string.Format("Failure while locating memberreferenceexpression {0} in {1}", target, curtype));


				curtype = next.FieldType;
			}

			IMemberDefinition lookup = null;
			var sourcetype = curtype;
			var probes = new Queue<TypeReference>();
			probes.Enqueue(curtype);


			while (lookup == null && probes.Count > 0)
			{
				curtype = probes.Dequeue();
				var curresolved = curtype.Resolve();
				if (curtype.IsArray)
					curresolved = m_asm.MainModule.Import(typeof(Array)).Resolve();

				lookup = curresolved.Fields.Where(x => x.Name == s.MemberName).FirstOrDefault() as IMemberDefinition;
				if (lookup == null)
					lookup = curresolved.Properties.Where(x => x.Name == s.MemberName).FirstOrDefault();
				if (lookup == null)
					lookup = curresolved.Methods.Where(x => x.Name == s.MemberName).FirstOrDefault();

				if (lookup == null)
				{
					if (curresolved.BaseType != null)
						probes.Enqueue(curresolved.BaseType.Resolve());
					if (curresolved.IsInterface)
						foreach(var v in curresolved.Interfaces)
							if (!v.IsSameTypeReference(m_asm.MainModule.Import(typeof(IBus))))
								probes.Enqueue(v.Resolve());
				}
			}

			if (lookup == null)
				throw new Exception(string.Format("Unable to find member {0} in {1}", s.MemberName, sourcetype.FullName));


			return m_globalInformation.StoreType(new MemberItem(s, lookup, sourcetype.Resolve()));
		}

		public bool IsVariableExpression(IVHDLExpression s)
		{
			var name = s is VHDLIdentifierExpression ? (s as VHDLIdentifierExpression).Identifier : null;
			if (name != null && m_localRenames.ContainsKey(name))
				name = m_localRenames[name];

			if (s is VHDLMemberReferenceExpression && (s as VHDLMemberReferenceExpression).Member.IsVariable)
				return true;
			else if (s is VHDLIdentifierExpression && (m_localVariables.ContainsKey(name) || m_classVariables.ContainsKey(name)))
				return true;
			else if (s is VHDLIndexerExpression)
				return IsVariableExpression((s as VHDLIndexerExpression).Target);
			else
				return false;
		}

		public bool IsConstantReference(VHDLMemberReferenceExpression s)
		{
			if (s.Member.Item is FieldDefinition)
			{
				var ft = s.Member.Item as FieldDefinition;
				if (m_globalInformation.Constants.ContainsKey(ft))
					return true;

				if (ft.IsLiteral || (ft.IsStatic && ft.IsInitOnly))
					return true;
			}

			return false;
		}
	}
}

