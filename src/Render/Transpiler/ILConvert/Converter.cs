using SME;
using System;
using System.Linq;
using System.Text;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.Decompiler;
using Mono.Cecil;
using System.Collections.Generic;
using ICSharpCode.NRefactory.TypeSystem;

namespace SME.Render.Transpiler.ILConvert
{
	public abstract partial class Converter<TExpression, TTypeClass>
		where TExpression : IAugmentedExpression
		where TTypeClass : class
	{
		protected readonly IndentedStringBuilder m_sb;
		protected readonly TypeDefinition m_typedef;
		protected readonly MethodDefinition m_methoddef;
		protected readonly AssemblyDefinition m_asm;
		protected readonly DecompilerContext m_context;

		protected readonly List<string> m_imports = new List<string>();
		protected readonly Dictionary<string, MemberItem> m_busVariableMap = new Dictionary<string, MemberItem>();

		protected readonly Dictionary<string, Tuple<TypeReference, TTypeClass>> m_localVariables = new Dictionary<string, Tuple<TypeReference, TTypeClass>>();
		protected readonly Dictionary<string, string> m_localRenames = new Dictionary<string, string>();
		protected readonly Dictionary<string, TypeReference> m_classVariables = new Dictionary<string, TypeReference>();
		protected readonly Dictionary<string, TypeReference> m_signals = new Dictionary<string, TypeReference>();
		protected readonly Dictionary<string, object> m_fieldInitializers = new Dictionary<string, object>();
		protected readonly Dictionary<PropertyDefinition, string> m_writtenSignals = new Dictionary<PropertyDefinition, string>();
		protected readonly Dictionary<PropertyDefinition, string> m_simulationWrittenSignals = new Dictionary<PropertyDefinition, string>();
		protected readonly Dictionary<MethodDefinition, Tuple<int, string[]>> m_compiledMethods = new Dictionary<MethodDefinition, Tuple<int, string[]>>();

		protected readonly GlobalInformation<TTypeClass> m_globalInformation;

		protected int m_varcount = 0;

		public TypeDefinition ProcType { get { return m_typedef; } }
		//public GlobalInformation<TTypeClass> Information { get { return m_globalInformation; } }
		public MethodDefinition MethodDef { get { return m_methoddef; } }
		public IDictionary<string, Tuple<TypeReference, TTypeClass>> LocalVariables { get { return m_localVariables; } }

		public static TypeDefinition LoadType(Type t)
		{			
			var asm = AssemblyDefinition.ReadAssembly(t.Assembly.Location);
			if (asm == null)
				return null;
			
			var res =
				(from td in 
					from m in asm.Modules
					select m.GetType(t.FullName)
					where td != null
					select td).FirstOrDefault();

			if (res == null && t.IsNested)
				res = asm.Modules.SelectMany(m => m.GetTypes().Where(x => x.Name == t.Name && x.DeclaringType.FullName == t.DeclaringType.FullName)).FirstOrDefault();
			
			return res;
		}

		public Converter(IProcess process, GlobalInformation<TTypeClass> globalInformation, int indentation = 0)
			: this(process.GetType(), globalInformation, indentation)
		{
			
		}

		public Converter(Type process, GlobalInformation<TTypeClass> globalInformation, int indentation = 0)
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

		public Converter(TypeDefinition process, GlobalInformation<TTypeClass> globalInformation, int indentation = 0)
		{
			m_globalInformation = globalInformation;

			m_typedef = process;
			m_asm = process.Module.Assembly;

			m_sb = new IndentedStringBuilder(indentation);
			m_context = new DecompilerContext(m_typedef.Module) { CurrentType = m_typedef };

			if (process == null)
			{
				m_sb.AppendLine(m_globalInformation.ToComment($"Unable to find type {process.FullName} in {process.Module.Assembly.Name.FullName}"));
				return;
			}

			if (!process.IsAssignableFrom<SimpleProcess>())
			{
				m_sb.AppendLine(m_globalInformation.ToComment($"Type {process.FullName} does not descend from {typeof(SimpleProcess).FullName}"));

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

			if (process.GetAttribute<SuppressOutputAttribute>() != null)
			{
				m_sb.AppendLine(m_globalInformation.ToComment("Supressed all output"));
				return;
			}

			if (process.IsAssignableFrom<SimpleProcess>())
				m_methoddef = m_typedef.Methods.Where(x => x.Name == "OnTick" && x.Parameters.Count == 0).FirstOrDefault();


			m_globalInformation.AddTypeDefinition(m_typedef);

			if (m_methoddef == null)
			{
				m_sb.AppendLine(m_globalInformation.ToComment($"Unable to find method OnTick in {m_typedef.FullName}"));
				return;
			}
				
			DoParse();
		}

		private Converter(MethodDefinition m, GlobalInformation<TTypeClass> globalInformation, int indentation = 0)
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
				m_localVariables[globalInformation.ToValidName(n.Name)] = new Tuple<TypeReference, TTypeClass>(n.ParameterType, m_globalInformation.GetOutputType(n));

			DoParse();

			// Unregister parameters
			foreach (var n in m_methoddef.Parameters)
				m_localVariables.Remove(globalInformation.ToValidName(n.Name));
			
			m_sb.Indentation -= 4;
		}

		private IEnumerable<string> Method
		{
			get
			{
				return RenderMethod(m_sb.Indentation);
			}
		}

		public IEnumerable<string> Body { get { return m_sb.Lines; } }

		public bool IsClockedProcess { get { return m_typedef.GetAttribute<ClockedProcessAttribute>() != null; } }

		public IEnumerable<MemberItem> WrittenProperties(TypeDefinition t)
		{
			var lst = t.GetBusProperties();
			if (t.IsBusType() && m_typedef.GetAttribute<SuppressBodyAttribute>() == null && m_typedef.GetAttribute<SuppressOutputAttribute>() == null && m_typedef.GetAttribute<IgnoreAttribute>() == null)
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


		public IEnumerable<string> Variables 
		{ 
			get 
			{ 
				return  
					(from n in m_classVariables
					 	let member = ResolveIdentifierItem(n.Key)
						where member.GetAttribute<IgnoreAttribute>() == null
							select string.Format("variable {0} : {1}", 
					      	m_globalInformation.ToValidName(n.Key),
                            member.ItemType.IsArray 
	                            ? m_globalInformation.ToValidName(member.Name + "_type")
								: VHDLType(member.Item)))
					
					.Union(
					from n in m_localVariables
					  select string.Format("variable {0} : {1}", 
						    m_globalInformation.ToValidName(n.Key), 
			                ToSafeName(n.Value.Item2))
				);
			} 
		}
		public IEnumerable<KeyValuePair<string, MemberItem>> Signals 
		{ 
			get 
			{ 
				return 
					from n in m_signals
					let member = ResolveIdentifierItem(n.Key)
				 	where member.GetAttribute<IgnoreAttribute>() == null
						select new KeyValuePair<string, MemberItem>(n.Key, member);
			} 
		}

		public IEnumerable<string> ProcessResetStaments
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
						yield return ResolveMemberReset(ResolveIdentifierItem(s), null);

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
			
		public IEnumerable<string> TypeDefinitions { get { return GetTypeDefinitions(); } }

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

		public virtual object GetInitialValueForReset(MemberItem signal, string varname)
		{
			var itemtype = signal.ItemType;
			var vhdltype = m_globalInformation.GetOutputType(signal.Item, signal.ItemType);
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

			return initialvalue;
		}

		public string ResolveMemberReset(MemberItem signal, string varname)
		{
			var initialvalue = GetInitialValueForReset(signal, varname);

			var target = string.IsNullOrWhiteSpace(varname) ?
				new MemberReferenceExpression(new ThisReferenceExpression(), signal.Name)
				:
				new MemberReferenceExpression(new MemberReferenceExpression(new ThisReferenceExpression(), varname), signal.Name);


			return ResolveResetStatement(target, initialvalue);
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

							m_sb.AppendLine(m_globalInformation.ToComment(string.Format("Failed to parse constructor statement: {0}", s.ToString().Replace(Environment.NewLine, " "))));
						}
						else if (s is InvocationExpression)
						{
							if (s.ToString() != "base..ctor ()")
								m_sb.AppendLine(m_globalInformation.ToComment(string.Format("-- Unparsed to parse constructor statement: {0}", s)));
						}
						else
						{
							m_sb.AppendLine(m_globalInformation.ToComment(string.Format("-- Unparsed constructor statement: {0}", s)));
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
					else if (f.GetAttribute<SignalAttribute>() == null)
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
				m_sb.AppendLine(m_globalInformation.ToComment(string.Format("-- Failed to locate body in {0}", m_typedef.FullName)));
				return;
			}
				
			//Console.WriteLine(methodnode);

			if (m_typedef.GetAttribute<SuppressBodyAttribute>() != null)
			{
				m_sb.AppendLine(m_globalInformation.ToComment("-- Supressed VHDL Body content"));
			}
			else
			{
				ReturnVariable = m_methoddef.ReturnType.FullName == "System.Void" ? null : RegisterTemporaryVariable(m_methoddef.ReturnType);

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
						var compiled = CreateNewConverter(c);
						m_compiledMethods[c] = new Tuple<int, string[]>(0, compiled.Method.ToArray());

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
			return CreateNewConverter(mdef).Method;
		}


		public string RegisterTemporaryVariable(TypeReference vartype, TTypeClass vhdltype = null)
		{
			var varname = m_globalInformation.ToValidName("tmpvar_" + (m_varcount++).ToString());
			m_localVariables.Add(varname, new Tuple<TypeReference, TTypeClass>(vartype, vhdltype ?? m_globalInformation.GetOutputType(vartype)));

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

		public void RegisterSignalWrite(MemberItem member, bool isSimulation)
		{
			// Register as written
			if (member.Type == MemberItem.MemberType.Property && member.DeclaringType.IsBusType())
			{
				if (isSimulation)
					m_simulationWrittenSignals[member.Item as PropertyDefinition] = string.Empty;
				else
					m_writtenSignals[member.Item as PropertyDefinition] = string.Empty;
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

		public Tuple<MemberItem, TTypeClass, string> ResolveLocalOrClassIdentifier(string name, Expression exp)
		{
			MemberItem res = null;
			TTypeClass vhdl = null;

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
				vhdl = m_globalInformation.GetOutputType(res.Item, res.ItemType);

			return new Tuple<MemberItem, TTypeClass, string>(res, vhdl, name);
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
			if (processname.StartsWith(asmname, StringComparison.Ordinal))
				processname = processname.Substring(asmname.Length);

			return m_globalInformation.ToValidName(processname);
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


		/// <summary>
		/// Renders a method in the target language.
		/// </summary>
		/// <returns>The lines in the method.</returns>
		/// <param name="indentation">The indentation to use.</param>
		public abstract IEnumerable<string> RenderMethod(int indentation);

		public abstract TExpression WrapConverted(TExpression s, TTypeClass target, bool fromCast = false);

		public abstract TExpression WrapIfComposite(TExpression s);

		public abstract TExpression ResolveExpression(Expression s);

		public abstract MemberItem ResolveIdentifierItem(string name);

		public abstract string ToSafeName(TTypeClass type);

		public abstract string GetDefaultInitializer(TypeReference vartype, IMemberDefinition member);

		public abstract string ResolveResetStatement(Expression target, object initialvalue = null);

		public virtual Converter<TExpression, TTypeClass> CreateNewConverter(MethodDefinition method)
		{
			return (Converter<TExpression, TTypeClass>)Activator.CreateInstance(this.GetType(), method, m_globalInformation);
		}

		public abstract bool IsVariableExpression(TExpression s);

		public abstract bool IsConstantReference(TExpression m);

		public abstract IEnumerable<string> GetTypeDefinitions();

	}
}

