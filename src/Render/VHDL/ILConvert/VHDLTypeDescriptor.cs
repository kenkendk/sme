using System;
using Mono.Cecil;
using System.Linq;
using System.Collections.Generic;

namespace SME.Render.VHDL.ILConvert
{
	public class VHDLTypeDescriptor
	{
		public string Alias { get; set; }
		public bool IsArray { get; set; }
		public string Name { get; set; }
		public string ElementName { get; set; }
		public int LowerBound { get; set; }
		public int UpperBound { get; set; }
		public TypeReference SourceType { get; set; }

		public int Length
		{
			get
			{
				return Math.Max(UpperBound, LowerBound) - Math.Min(UpperBound, LowerBound) + 1;
			}
		}

		public bool IsNumeric
		{
			get
			{
				return new [] { 
					VHDLTypes.NUMERIC_INT8,
					VHDLTypes.NUMERIC_INT16,
					VHDLTypes.NUMERIC_INT32,
					VHDLTypes.NUMERIC_INT64,
					VHDLTypes.NUMERIC_UINT8,
					VHDLTypes.NUMERIC_UINT16,
					VHDLTypes.NUMERIC_UINT32,
					VHDLTypes.NUMERIC_UINT64,
				}.Contains(this);
			}
		}

		public bool IsSystemType
		{
			get
			{
				return new [] { 
					VHDLTypes.SYSTEM_BOOL,
					VHDLTypes.SYSTEM_INT8,
					VHDLTypes.SYSTEM_INT16,
					VHDLTypes.SYSTEM_INT32,
					VHDLTypes.SYSTEM_INT64,
					VHDLTypes.SYSTEM_UINT8,
					VHDLTypes.SYSTEM_UINT16,
					VHDLTypes.SYSTEM_UINT32,
					VHDLTypes.SYSTEM_UINT64,
				}.Contains(this);
			}
		}

		public bool IsVHDLSigned
		{
			get
			{
				return Name.StartsWith("SIGNED", StringComparison.OrdinalIgnoreCase);
			}
		}

		public bool IsVHDLUnsigned
		{
			get
			{
				return Name.StartsWith("UNSIGNED", StringComparison.OrdinalIgnoreCase);
			}
		}

		public bool IsSystemSigned
		{
			get
			{
				return new [] { 
					VHDLTypes.SYSTEM_INT8,
					VHDLTypes.SYSTEM_INT16,
					VHDLTypes.SYSTEM_INT32,
					VHDLTypes.SYSTEM_INT64,
				}.Contains(this);
			}
		}

		public bool IsSystemUnsigned
		{
			get
			{
				return new [] { 
					VHDLTypes.SYSTEM_UINT8,
					VHDLTypes.SYSTEM_UINT16,
					VHDLTypes.SYSTEM_UINT32,
					VHDLTypes.SYSTEM_UINT64,
				}.Contains(this);
			}
		}

		public bool IsNumericSigned
		{
			get
			{
				return new[] {
					VHDLTypes.NUMERIC_INT8,
					VHDLTypes.NUMERIC_INT16,
					VHDLTypes.NUMERIC_INT32,
					VHDLTypes.NUMERIC_INT64,
				}.Contains(this);
			}
		}

		public bool IsNumericUnsigned
		{
			get
			{
				return new[] {
					VHDLTypes.NUMERIC_UINT8,
					VHDLTypes.NUMERIC_UINT16,
					VHDLTypes.NUMERIC_UINT32,
					VHDLTypes.NUMERIC_UINT64,
				}.Contains(this);
			}
		}

		public bool IsSigned
		{
			get
			{
				return IsSystemSigned || IsVHDLSigned;
			}
		}

		public bool IsUnsigned
		{
			get
			{
				return IsSystemUnsigned || IsVHDLUnsigned;
			}
		}
		public bool IsStdLogicVector
		{
			get
			{
				return Name.StartsWith("STD_LOGIC_VECTOR", StringComparison.OrdinalIgnoreCase);
			}
		}

		public bool IsStdLogic
		{
			get
			{
				return Name.Equals("STD_LOGIC", StringComparison.OrdinalIgnoreCase);
			}
		}

		public override string ToString()
		{
			return string.IsNullOrWhiteSpace(Alias) ? Name : Alias;
		}

		public string ToSafeVHDLName()
		{
			var str = this.ToString();
			if (IsUnsigned || IsStdLogicVector)
				return str;
			else
				return Renderer.ConvertToValidVHDLName(str);
		}
	}

	public class VHDLTypeScope
	{
		private static Type[] SIGNED_NUMERIC_TYPES = new Type[] { typeof(sbyte), typeof(short), typeof(int), typeof(long) };
		private static Type[] UNSIGNED_NUMERIC_TYPES = new Type[] { typeof(byte), typeof(ushort), typeof(uint), typeof(ulong) };

		private static Type[] NUMERIC_TYPES = new Type[] { typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong) };
		private readonly TypeReference[] m_numericTypes;
		private readonly TypeReference[] m_signedNumericTypes;
		private readonly TypeReference[] m_unsignedNumericTypes;

		private readonly TypeDefinition[] m_resolvedNumericTypes;
		private readonly TypeDefinition[] m_resolvedSignedNumericTypes;
		private readonly TypeDefinition[] m_resolvedUnsignedNumericTypes;

		private readonly System.Text.RegularExpressions.Regex _re = new System.Text.RegularExpressions.Regex(@"STD_LOGIC_VECTOR\((?<from>\d+) +(?<direction>(to)|(downto)) +(?<to>\d+)\)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
		private readonly Dictionary<string, VHDLTypeDescriptor> m_stringTypes;

		private readonly Dictionary<string, VHDLTypeDescriptor> m_builtins = new Dictionary<string, VHDLTypeDescriptor>(StringComparer.OrdinalIgnoreCase);

		private readonly Dictionary<string, VHDLTypeDescriptor> m_arrays = new Dictionary<string, VHDLTypeDescriptor>(StringComparer.OrdinalIgnoreCase);

		private readonly Dictionary<int, VHDLTypeDescriptor> m_vectorTypes = new Dictionary<int, VHDLTypeDescriptor>();

		private readonly ModuleDefinition m_resolveModule;

		public VHDLTypeScope(ModuleDefinition resolveModule)
		{
			m_resolveModule = resolveModule;

			foreach (var p in typeof(VHDLTypes).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Where(x => x.FieldType == typeof(VHDLTypeDescriptor)).Select(x => (VHDLTypeDescriptor)x.GetValue(null)))
			{
				m_builtins[p.Name] = p;
				if (!string.IsNullOrWhiteSpace(p.Alias))
					m_builtins[p.Alias] = p;

				if (p.IsSystemType)
					m_arrays[p.Name + "_ARRAY"] = new VHDLTypeDescriptor() {
						Name = p.Alias + "_ARRAY",
						IsArray = true,
						ElementName = p.Name
					};
			}

			m_stringTypes = new Dictionary<string, VHDLTypeDescriptor>(m_builtins, StringComparer.OrdinalIgnoreCase);
			m_numericTypes = NUMERIC_TYPES.Select(x => resolveModule.Import(x)).ToArray();
			m_signedNumericTypes = SIGNED_NUMERIC_TYPES.Select(x => resolveModule.Import(x)).ToArray();
			m_unsignedNumericTypes = UNSIGNED_NUMERIC_TYPES.Select(x => resolveModule.Import(x)).ToArray();

			m_resolvedNumericTypes = m_numericTypes.Select(x => x.Resolve()).ToArray();
			m_resolvedSignedNumericTypes = m_signedNumericTypes.Select(x => x.Resolve()).ToArray();
			m_resolvedUnsignedNumericTypes = m_unsignedNumericTypes.Select(x => x.Resolve()).ToArray();
		}


		public IEnumerable<string> BuiltinNames
		{
			get { return m_builtins.Keys; }
		}


		public VHDLTypeDescriptor GetVHDLType(string typename, string alias, TypeReference type)
		{
			if (!string.IsNullOrWhiteSpace(alias) && m_stringTypes.ContainsKey(alias))
				return m_stringTypes[alias];
			
			if (m_stringTypes.ContainsKey(typename) && m_stringTypes[typename].Alias == alias)
				return m_stringTypes[typename];


			var m = _re.Match(typename);
			VHDLTypeDescriptor res;
			if (m.Success && m.Length == typename.Length)
			{
				var fr = int.Parse(m.Groups["from"].Value);
				var to = int.Parse(m.Groups["to"].Value);

				if (alias == null && string.Equals(m.Groups["direction"].Value, "downto", StringComparison.OrdinalIgnoreCase))
					return GetStdLogicVector(Math.Max(fr, to) - Math.Min(fr, to) + 1);


				res = new VHDLTypeDescriptor() {
					Name = typename,
					Alias = alias,
					IsArray = true,
					ElementName = "STD_LOGIC",
					SourceType = type,
					UpperBound = 
						string.Equals(m.Groups["direction"].Value, "downto", StringComparison.OrdinalIgnoreCase)
						? Math.Max(fr, to) : Math.Min(fr, to),
					LowerBound =
						string.Equals(m.Groups["direction"].Value, "downto", StringComparison.OrdinalIgnoreCase)
						? Math.Min(fr, to) : Math.Max(fr, to),
				};

			}
			else
			{
				res = new VHDLTypeDescriptor() {
					Name = typename,
					Alias = alias,
					SourceType = type,
					IsArray = false
				};
			}

			if (!m_stringTypes.ContainsKey(res.Name))
				m_stringTypes.Add(res.Name, res);
			if (!string.IsNullOrWhiteSpace(res.Alias))
				m_stringTypes.Add(res.Alias, res);
			return res;

		}

		public VHDLTypeDescriptor NumericEquivalent(VHDLTypeDescriptor type, bool throwIfNotFound = true)
		{
			if (type.IsNumeric)
				return type;

			if (type == VHDLTypes.SYSTEM_UINT8 || type.IsStdLogicVector && type.Length == 8)
				return VHDLTypes.NUMERIC_UINT8;
			else if (type == VHDLTypes.SYSTEM_UINT16 || type.IsStdLogicVector && type.Length == 16)
				return VHDLTypes.NUMERIC_UINT16;
			else if (type == VHDLTypes.SYSTEM_UINT32 || type.IsStdLogicVector && type.Length == 32)
				return VHDLTypes.NUMERIC_UINT32;
			else if (type == VHDLTypes.SYSTEM_UINT64 || type.IsStdLogicVector && type.Length == 64)
				return VHDLTypes.NUMERIC_UINT64;
			else if (type == VHDLTypes.SYSTEM_INT8 || type.IsStdLogicVector && type.Length == 8)
				return VHDLTypes.NUMERIC_INT8;
			else if (type == VHDLTypes.SYSTEM_INT16 || type.IsStdLogicVector && type.Length == 16)
				return VHDLTypes.NUMERIC_INT16;
			else if (type == VHDLTypes.SYSTEM_INT32 || type.IsStdLogicVector && type.Length == 32)
				return VHDLTypes.NUMERIC_INT32;
			else if (type == VHDLTypes.SYSTEM_INT64 || type.IsStdLogicVector && type.Length == 64)
				return VHDLTypes.NUMERIC_INT64;
			else if (type.SourceType != null && type.IsStdLogicVector)
			{
				/*foreach (var n in type.SourceType.Resolve().Methods)
				{
					if (n.IsSpecialName && n.IsStatic && n.Name == "op_Implicit")
						Console.WriteLine();
					if (n.MethodReturnType.ReturnType.IsPrimitive)
						Console.WriteLine();
				}*/

				var targets = type.SourceType.Resolve().Methods.Where(x => x.IsSpecialName && x.IsStatic && x.Name == "op_Implicit" && x.MethodReturnType.ReturnType.IsPrimitive && m_resolvedNumericTypes.Where(y => x.MethodReturnType.ReturnType.IsSameTypeReference(y)).Any()).Select(y => y.MethodReturnType.ReturnType).ToArray();
				if (targets.Count() == 0)
					throw new Exception(string.Format("Unable to get numeric equivalent for {0}", type.SourceType.FullName));
				else if (targets.Count() == 1)
				{
					var t = targets.First();
					var vt = GetVHDLType(t);

					if (vt.IsStdLogicVector && vt.Length == type.Length)
						return NumericEquivalent(vt);

					if (m_signedNumericTypes.Where(x => x.IsSameTypeReference(t)).Any())
					{
						var name = string.Format("SIGNED({0} downto {1})", type.UpperBound, type.LowerBound);

						if (m_stringTypes.ContainsKey(name))
							return m_stringTypes[name];
						
						return m_stringTypes[name] = new VHDLTypeDescriptor() {
							Name = name,
							IsArray = true,
							ElementName = "STD_LOGIC",
							UpperBound = type.UpperBound,
							LowerBound = type.LowerBound
						};
					}
					else if (m_unsignedNumericTypes.Where(x => x.IsSameTypeReference(t)).Any())
					{
						var name = string.Format("UNSIGNED({0} downto {1})", type.UpperBound, type.LowerBound);

						if (m_stringTypes.ContainsKey(name))
							return m_stringTypes[name];

						return m_stringTypes[name] = new VHDLTypeDescriptor() {
							Name = name,
							IsArray = true,
							ElementName = "STD_LOGIC",
							UpperBound = type.UpperBound,
							LowerBound = type.LowerBound
						};

					}
					else
						throw new Exception("Unexpected case");

				}
				else
					return type; // Defer decision until later so the typecast can choose the right variant
			}
			else
			{
				if (throwIfNotFound)
					throw new Exception(string.Format("Unable to find suitable numeric type for {0}", type.Alias ?? type.Name));
				else
					return null;
			}
		}

		public VHDLTypeDescriptor SystemEquivalent(VHDLTypeDescriptor type)
		{
			if (type.IsSystemType)
				return type;

			if (type == VHDLTypes.NUMERIC_UINT8 || type.IsStdLogicVector && type.Length == 8)
				return VHDLTypes.SYSTEM_UINT8;
			else if (type == VHDLTypes.NUMERIC_UINT16 || type.IsStdLogicVector && type.Length == 16)
				return VHDLTypes.SYSTEM_UINT16;
			else if (type == VHDLTypes.NUMERIC_UINT32 || type.IsStdLogicVector && type.Length == 32)
				return VHDLTypes.SYSTEM_UINT32;
			else if (type == VHDLTypes.NUMERIC_UINT64 || type.IsStdLogicVector && type.Length == 64)
				return VHDLTypes.SYSTEM_UINT64;
			else if (type == VHDLTypes.NUMERIC_INT8 || type.IsStdLogicVector && type.Length == 8)
				return VHDLTypes.SYSTEM_INT8;
			else if (type == VHDLTypes.NUMERIC_INT16 || type.IsStdLogicVector && type.Length == 16)
				return VHDLTypes.SYSTEM_INT16;
			else if (type == VHDLTypes.NUMERIC_INT32 || type.IsStdLogicVector && type.Length == 32)
				return VHDLTypes.SYSTEM_INT32;
			else if (type == VHDLTypes.NUMERIC_INT64 || type.IsStdLogicVector && type.Length == 64)
				return VHDLTypes.SYSTEM_INT64;
			else
				throw new Exception(string.Format("Unable to find suitable system type for {0}", type.Alias ?? type.Name));
		}

		public VHDLTypeDescriptor StdLogicVectorEquivalent(VHDLTypeDescriptor type)
		{
			if (!type.IsStdLogicVector)
				throw new Exception(string.Format("Unable to find suitable std_logic_vector type for {0}", type.Alias ?? type.Name));

			return GetStdLogicVector(type.Length);
		}

		public VHDLTypeDescriptor GetVHDLType(ParameterDefinition type)
		{
			var tr = type.ParameterType;

			if (!tr.IsArrayType())
				return GetVHDLType(tr);

			var eltype = GetVHDLType(tr.GetArrayElementType());

			var name = 
				string.IsNullOrWhiteSpace(eltype.Alias)
				? ((MethodDefinition)type.Method).Name + "_" + type.Name + "_ARRAY"
				: eltype.ToString() + "_ARRAY";

			if (!m_arrays.ContainsKey(name))
			{
				m_arrays[name] = new VHDLTypeDescriptor() {
					IsArray = true,
					Name = name,
					ElementName = eltype.ToString()
				};
			}

			return m_arrays[name];
		}

		public VHDLTypeDescriptor GetVHDLType(System.Reflection.PropertyInfo pi)
		{
			var customtype = pi.GetCustomAttributes(typeof(VHDLTypeAttribute), true).FirstOrDefault() as VHDLTypeAttribute;
			if (customtype != null)
				return GetVHDLType(customtype, m_resolveModule.Import(pi.PropertyType));

			// Try on-type declaration
			customtype = pi.PropertyType.GetCustomAttributes(typeof(VHDLTypeAttribute), true).FirstOrDefault() as VHDLTypeAttribute;
			if (customtype != null)
				return GetVHDLType(customtype, m_resolveModule.Import(pi.PropertyType));

			if (!pi.PropertyType.IsArray)
				return GetVHDLType(pi.PropertyType);


			if (!pi.PropertyType.IsArrayType())
				return GetVHDLType(pi.PropertyType);

			var eltype = GetVHDLType(pi.PropertyType.GetArrayElementType());

			var name = 
				string.IsNullOrWhiteSpace(eltype.Alias)
				? pi.Name + "_ARRAY"
				: eltype.ToString() + "_ARRAY";
			
			if (!m_arrays.ContainsKey(name))
			{
				m_arrays[name] = new VHDLTypeDescriptor() {
					IsArray = true,
					Name = name,
					ElementName = eltype.ToString()
				};
			}

			return m_arrays[name];
		}

		public VHDLTypeDescriptor GetVHDLType(VHDLTypeAttribute attr, TypeReference type)
		{
			return GetVHDLType(attr.Type, attr.Alias, type);
		}

		public VHDLTypeDescriptor GetVHDLType(IMemberDefinition type, TypeReference membertype)
		{
			TypeReference tr = membertype;
			var customtype = type.GetAttribute<VHDLTypeAttribute>();

			if (tr == null)
			{
				if (type is FieldDefinition)
					tr = (type as FieldDefinition).FieldType;
				else if (type is PropertyDefinition)
					tr = (type as PropertyDefinition).PropertyType;
				else if (type is MethodDefinition)
					tr = (type as MethodDefinition).ReturnType;
				else
					throw new Exception(string.Format("Not supported member type: {0}", type.GetType().FullName));
			}

			if (customtype == null)
				customtype = tr.GetAttribute<VHDLTypeAttribute>();

			VHDLTypeDescriptor customvhdl = null;
			if (customtype != null)
			{
				var argname = customtype.ConstructorArguments.First().Value as string;
				var argalias = customtype.ConstructorArguments.Count > 1 ? customtype.ConstructorArguments.Last().Value as string : null;
				customvhdl = GetVHDLType(new VHDLTypeAttribute(argname, argalias), tr);
			}

			if (!tr.IsArrayType())
			{				
				if (tr.IsSameTypeReference(typeof(IntPtr)))
				{
					if (IntPtr.Size == 4)
						return VHDLTypes.SYSTEM_INT32;
					else if (IntPtr.Size == 8)
						return VHDLTypes.SYSTEM_INT64;
				}
				else if (tr.IsSameTypeReference(typeof(UIntPtr)))
				{
					if (IntPtr.Size == 4)
						return VHDLTypes.SYSTEM_UINT32;
					else if (IntPtr.Size == 8)
						return VHDLTypes.SYSTEM_UINT64;
				}
				return customvhdl == null ? GetVHDLType(tr) : customvhdl;
			}

			var eltype = GetVHDLType(tr.GetArrayElementType());

			var name = 
				string.IsNullOrWhiteSpace(eltype.Alias)
				? type.Name + "_ARRAY"
				: eltype.ToString() + "_ARRAY";
			
			if (!m_arrays.ContainsKey(name))
			{
				var rt = customvhdl == null ? eltype : customvhdl;

				m_arrays[name] = new VHDLTypeDescriptor() {
					IsArray = true,
					Name = name,
					ElementName = rt.ToString()
				};
			}

			return m_arrays[name];
		}

		public VHDLTypeDescriptor GetByName(string name)
		{
			var res = TryGetByName(name);
			if (res == null)
				throw new Exception(string.Format("Unable to find type {0}", name));
			return res;
			
		}

		public VHDLTypeDescriptor TryGetByName(string name)
		{
			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException($"{name}");

			if (m_builtins.ContainsKey(name))
				return m_builtins[name];
			if (m_stringTypes.ContainsKey(name))
				return m_stringTypes[name];
			if (m_arrays.ContainsKey(name))
				return m_arrays[name];

			return m_builtins.Values.Union(m_stringTypes.Values).Union(m_arrays.Values).Union(m_vectorTypes.Values).Where(x => string.Equals(x.Alias, name, StringComparison.OrdinalIgnoreCase) || string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
		}

		public VHDLTypeDescriptor GetVHDLType(TypeReference type)
		{
			if (type.IsArrayType())
				throw new Exception("Should call with a member reference");

			var customtype = type.GetAttribute<VHDLTypeAttribute>();
			if (customtype != null)
			{
				var argname = customtype.ConstructorArguments.First().Value as string;
				var argalias = customtype.ConstructorArguments.Count > 1 ? customtype.ConstructorArguments.Last().Value as string : null;
				return GetVHDLType(new VHDLTypeAttribute(argname, argalias), type);
			}

			if (type.IsType<IntPtr>())
			{
				if (IntPtr.Size == 4)
					return VHDLTypes.SYSTEM_INT32;
				else if (IntPtr.Size == 8)
					return VHDLTypes.SYSTEM_INT64;
			}

			if (type.IsType<UIntPtr>())
			{
				if (IntPtr.Size == 4)
					return VHDLTypes.SYSTEM_UINT32;
				else if (IntPtr.Size == 8)
					return VHDLTypes.SYSTEM_UINT64;
			}

			if (type.IsType<byte>())
				return VHDLTypes.SYSTEM_UINT8;
			else if (type.IsType<sbyte>())
				return VHDLTypes.SYSTEM_INT8;
			else if (type.IsType<short>())
				return VHDLTypes.SYSTEM_INT16;
			else if (type.IsType<ushort>())
				return VHDLTypes.SYSTEM_UINT16;
			else if (type.IsType<int>())
				return VHDLTypes.SYSTEM_INT32;
			else if (type.IsType<uint>())
				return VHDLTypes.SYSTEM_UINT32;
			else if (type.IsType<long>())
				return VHDLTypes.SYSTEM_INT64;
			else if (type.IsType<ulong>())
				return VHDLTypes.SYSTEM_UINT64;
			else if (type.IsType<bool>())
				return VHDLTypes.SYSTEM_BOOL;
			else if (type.Resolve().IsEnum)
				return GetVHDLType(type.FullName, null, type);
			else
			{
				return GetVHDLType(type.FullName, null, type);
				//throw new Exception(string.Format("Unsupported type: {0}", type.FullName));
			}
		}

		public VHDLTypeDescriptor GetVHDLType(Type type)
		{
			if (type == typeof(byte))
				return VHDLTypes.SYSTEM_UINT8;
			else if (type == typeof(ushort))
				return VHDLTypes.SYSTEM_UINT16;
			else if (type == typeof(uint))
				return VHDLTypes.SYSTEM_UINT32;
			else if (type == typeof(ulong))
				return VHDLTypes.SYSTEM_UINT64;
			else if (type == typeof(sbyte))
				return VHDLTypes.SYSTEM_INT8;
			else if (type == typeof(short))
				return VHDLTypes.SYSTEM_INT16;
			else if (type == typeof(int))
				return VHDLTypes.SYSTEM_INT32;
			else if (type == typeof(long))
				return VHDLTypes.SYSTEM_INT64;
			else if (type == typeof(bool))
				return VHDLTypes.SYSTEM_BOOL;
			else
				return GetVHDLType(type.FullName, null, m_resolveModule.Import(type));
				//throw new Exception(string.Format("Unsupported type: {0}", type.FullName));
		}

		public VHDLTypeDescriptor GetStdLogicVector(int length)
		{
			if (!m_vectorTypes.ContainsKey(length))
				m_vectorTypes[length] = new VHDLTypeDescriptor() 
				{
					Name = string.Format("STD_LOGIC_VECTOR({0} downto 0)", length - 1),
					IsArray = true,
					ElementName = "STD_LOGIC",
					LowerBound = 0,
					UpperBound = length - 1,
				};

			return m_vectorTypes[length];
		}
	}

	public static class VHDLTypes
	{
		public static readonly VHDLTypeDescriptor INTEGER = new VHDLTypeDescriptor() {
			Name = "INTEGER",
			IsArray = false
		};

		public static readonly VHDLTypeDescriptor NUMERIC_UINT8 = new VHDLTypeDescriptor() {
			Name = "UNSIGNED(7 downto 0)",
			IsArray = true,
			ElementName ="STD_LOGIC",
			LowerBound = 0,
			UpperBound = 7
		};

		public static readonly VHDLTypeDescriptor NUMERIC_UINT16 = new VHDLTypeDescriptor() {
			Name = "UNSIGNED(15 downto 0)",
			IsArray = true,
			ElementName ="STD_LOGIC",
			LowerBound = 0,
			UpperBound = 15
		};

		public static readonly VHDLTypeDescriptor NUMERIC_UINT32 = new VHDLTypeDescriptor() {
			Name = "UNSIGNED(31 downto 0)",
			IsArray = true,
			ElementName ="STD_LOGIC",
			LowerBound = 0,
			UpperBound = 31
		};

		public static readonly VHDLTypeDescriptor NUMERIC_UINT64 = new VHDLTypeDescriptor() {
			Name = "UNSIGNED(63 downto 0)",
			IsArray = true,
			ElementName ="STD_LOGIC",
			LowerBound = 0,
			UpperBound = 63
		};

		public static readonly VHDLTypeDescriptor NUMERIC_INT8 = new VHDLTypeDescriptor() {
			Name = "SIGNED(7 downto 0)",
			IsArray = true,
			ElementName ="STD_LOGIC",
			LowerBound = 0,
			UpperBound = 7
		};

		public static readonly VHDLTypeDescriptor NUMERIC_INT16 = new VHDLTypeDescriptor() {
			Name = "SIGNED(15 downto 0)",
			IsArray = true,
			ElementName ="STD_LOGIC",
			LowerBound = 0,
			UpperBound = 15
		};

		public static readonly VHDLTypeDescriptor NUMERIC_INT32 = new VHDLTypeDescriptor() {
			Name = "SIGNED(31 downto 0)",
			IsArray = true,
			ElementName ="STD_LOGIC",
			LowerBound = 0,
			UpperBound = 31
		};

		public static readonly VHDLTypeDescriptor NUMERIC_INT64 = new VHDLTypeDescriptor() {
			Name = "SIGNED(63 downto 0)",
			IsArray = true,
			ElementName ="STD_LOGIC",
			LowerBound = 0,
			UpperBound = 53
		};

		public static readonly VHDLTypeDescriptor BOOL = new VHDLTypeDescriptor() {
			Name = "BOOLEAN",
			IsArray = false
		};

		public static readonly VHDLTypeDescriptor SYSTEM_BOOL = new VHDLTypeDescriptor() {
			Name = "STD_LOGIC",
			Alias = "T_SYSTEM_BOOL",
			//SourceType = typeof(bool),
			IsArray = false,
		};

		public static readonly VHDLTypeDescriptor SYSTEM_UINT8 = new VHDLTypeDescriptor() {
			Name = "STD_LOGIC_VECTOR(7 downto 0)",
			Alias = "T_SYSTEM_UINT8",
			//SourceType = typeof(byte),
			IsArray = true,
			ElementName ="STD_LOGIC",
			LowerBound = 0,
			UpperBound = 7,
		};

		public static readonly VHDLTypeDescriptor SYSTEM_INT8 = new VHDLTypeDescriptor() {
			Name = "STD_LOGIC_VECTOR(7 downto 0)",
			Alias = "T_SYSTEM_INT8",
			//SourceType = typeof(sbyte),
			IsArray = true,
			ElementName ="STD_LOGIC",
			LowerBound = 0,
			UpperBound = 7
		};

		public static readonly VHDLTypeDescriptor SYSTEM_UINT16 = new VHDLTypeDescriptor() {
			Name = "STD_LOGIC_VECTOR(15 downto 0)",
			Alias = "T_SYSTEM_UINT16",
			//SourceType = typeof(ushort),
			IsArray = true,
			ElementName ="STD_LOGIC",
			LowerBound = 0,
			UpperBound = 15
		};

		public static readonly VHDLTypeDescriptor SYSTEM_INT16 = new VHDLTypeDescriptor() {
			Name = "STD_LOGIC_VECTOR(15 downto 0)",
			Alias = "T_SYSTEM_INT16",
			//SourceType = typeof(short),
			IsArray = true,
			ElementName ="STD_LOGIC",
			LowerBound = 0,
			UpperBound = 15
		};

		public static readonly VHDLTypeDescriptor SYSTEM_UINT32 = new VHDLTypeDescriptor() {
			Name = "STD_LOGIC_VECTOR(31 downto 0)",
			Alias = "T_SYSTEM_UINT32",
			//SourceType = typeof(uint),
			IsArray = true,
			ElementName ="STD_LOGIC",
			LowerBound = 0,
			UpperBound = 31
		};

		public static readonly VHDLTypeDescriptor SYSTEM_INT32 = new VHDLTypeDescriptor() {
			Name = "STD_LOGIC_VECTOR(31 downto 0)",
			Alias = "T_SYSTEM_INT32",
			//SourceType = typeof(int),
			IsArray = true,
			ElementName ="std_logic",
			LowerBound = 0,
			UpperBound = 31
		};

		public static readonly VHDLTypeDescriptor SYSTEM_UINT64 = new VHDLTypeDescriptor() {
			Name = "STD_LOGIC_VECTOR(63 downto 0)",
			Alias = "T_SYSTEM_UINT64",
			//SourceType = typeof(ulong),
			IsArray = true,
			ElementName ="STD_LOGIC",
			LowerBound = 0,
			UpperBound = 63
		};

		public static readonly VHDLTypeDescriptor SYSTEM_INT64 = new VHDLTypeDescriptor() {
			Name = "STD_LOGIC_VECTOR(63 downto 0)",
			Alias = "T_SYSTEM_INT64",
			//SourceType = typeof(long),
			IsArray = true,
			ElementName ="STD_LOGIC",
			LowerBound = 0,
			UpperBound = 63
		};
	}
}

