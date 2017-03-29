using System;
using Mono.Cecil;
using System.Linq;
using System.Collections.Generic;
using SME.AST;

namespace SME.VHDL
{
	/// <summary>
	/// Representation of a VHDL type
	/// </summary>
	public class VHDLType
	{
		/// <summary>
		/// Gets or sets a type alias name
		/// </summary>
		public string Alias { get; set; }
		/// <summary>
		/// Gets or sets a value indicating if the type is an array
		/// </summary>
		public bool IsArray { get; set; }
		/// <summary>
		/// Gets or sets the primary VHDL name
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// Gets or sets the name of the elements in the array
		/// </summary>
		public string ElementName { get; set; }
		/// <summary>
		/// Gets or sets the lower bound of the array.
		/// </summary>
		public int LowerBound { get; set; }
		/// <summary>
		/// Gets or sets the upper bound of the array.
		/// </summary>
		public int UpperBound { get; set; }
		/// <summary>
		/// Gets or sets the source CeCil type
		/// </summary>
		public TypeReference SourceType { get; set; }

		/// <summary>
		/// Gets or sets the length of the array
		/// </summary>
		public int Length
		{
			get
			{
				return Math.Max(UpperBound, LowerBound) - Math.Min(UpperBound, LowerBound) + 1;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:SME.VHDL.VHDLType"/> is a numeric type.
		/// </summary>
		public bool IsNumeric
		{
			get
			{
				return new[] {
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

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:SME.VHDL.VHDLType"/> is a system type.
		/// </summary>
		public bool IsSystemType
		{
			get
			{
				return new[] {
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

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:SME.VHDL.VHDLType"/> is a VHDL signed type.
		/// </summary>
		/// <value><c>true</c> if is VHDLS igned; otherwise, <c>false</c>.</value>
		public bool IsVHDLSigned
		{
			get
			{
				return Name.StartsWith("SIGNED", StringComparison.OrdinalIgnoreCase);
			}
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:SME.VHDL.VHDLType"/> is a VHDL unsigned type.
		/// </summary>
		public bool IsVHDLUnsigned
		{
			get
			{
				return Name.StartsWith("UNSIGNED", StringComparison.OrdinalIgnoreCase);
			}
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:SME.VHDL.VHDLType"/> is a system signed type.
		/// </summary>
		public bool IsSystemSigned
		{
			get
			{
				return new[] {
					VHDLTypes.SYSTEM_INT8,
					VHDLTypes.SYSTEM_INT16,
					VHDLTypes.SYSTEM_INT32,
					VHDLTypes.SYSTEM_INT64,
				}.Contains(this);
			}
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:SME.VHDL.VHDLType"/> is a system unsigned type.
		/// </summary>
		public bool IsSystemUnsigned
		{
			get
			{
				return new[] {
					VHDLTypes.SYSTEM_UINT8,
					VHDLTypes.SYSTEM_UINT16,
					VHDLTypes.SYSTEM_UINT32,
					VHDLTypes.SYSTEM_UINT64,
				}.Contains(this);
			}
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:SME.VHDL.VHDLType"/> is a numeric signed type.
		/// </summary>
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

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:SME.VHDL.VHDLType"/> is a numeric unsigned type.
		/// </summary>
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

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:SME.VHDL.VHDLType"/> is a signed type.
		/// </summary>
		public bool IsSigned
		{
			get
			{
				return IsSystemSigned || IsVHDLSigned;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:SME.VHDL.VHDLType"/> is an unsigned type.
		/// </summary>
		public bool IsUnsigned
		{
			get
			{
				return IsSystemUnsigned || IsVHDLUnsigned;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:SME.VHDL.VHDLType"/> is a std_logic_vector type.
		/// </summary>
		public bool IsStdLogicVector
		{
			get
			{
				return Name.StartsWith("STD_LOGIC_VECTOR", StringComparison.OrdinalIgnoreCase);
			}
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:SME.VHDL.VHDLType"/> is a std_logic type.
		/// </summary>
		public bool IsStdLogic
		{
			get
			{
				return Name.Equals("STD_LOGIC", StringComparison.OrdinalIgnoreCase);
			}
		}

		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:SME.VHDL.VHDLType"/>.
		/// </summary>
		/// <returns>A <see cref="T:System.String"/> that represents the current <see cref="T:SME.VHDL.VHDLType"/>.</returns>
		public override string ToString()
		{
			return string.IsNullOrWhiteSpace(Alias) ? Name : Alias;
		}

		/// <summary>
		/// Returns a valid VHDL name for this type
		/// </summary>
		/// <returns>The safe VHDLN ame.</returns>
		public string ToSafeVHDLName()
		{
			var str = this.ToString();
			if (IsUnsigned || IsSigned || IsStdLogicVector || IsSystemType)
				return str;
			else
				return Naming.ToValidName(str);
		}
	}

	/// <summary>
	/// A new VHDL type scope
	/// </summary>
	public class VHDLTypeScope
	{
		/// <summary>
		/// The signed numeric types in System.Reflection.
		/// </summary>
		private static Type[] SIGNED_NUMERIC_TYPES = new Type[] { typeof(sbyte), typeof(short), typeof(int), typeof(long) };
		/// <summary>
		/// The unsigned numeric types in System.Reflection.
		/// </summary>
		private static Type[] UNSIGNED_NUMERIC_TYPES = new Type[] { typeof(byte), typeof(ushort), typeof(uint), typeof(ulong) };

		/// <summary>
		/// The numeric types in System.Reflection.
		/// </summary>
		private static Type[] NUMERIC_TYPES = new Type[] { typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong) };

		/// <summary>
		/// The numeric types in Mono.Cecil.
		/// </summary>
		private readonly TypeReference[] m_numericTypes;
		/// <summary>
		/// The signed numeric types in Mono.Cecil.
		/// </summary>
		private readonly TypeReference[] m_signedNumericTypes;
		/// <summary>
		/// The unsigned numeric types in Mono.Cecil.
		/// </summary>
		private readonly TypeReference[] m_unsignedNumericTypes;

		/// <summary>
		/// The resolved numeric types in Mono.Cecil.
		/// </summary>
		private readonly TypeDefinition[] m_resolvedNumericTypes;

		/// <summary>
		/// Regulaer expression used to parse a STD_LOGIC_VECTOR definition
		/// </summary>
		private readonly System.Text.RegularExpressions.Regex _re = new System.Text.RegularExpressions.Regex(@"STD_LOGIC_VECTOR\((?<from>\d+) +(?<direction>(to)|(downto)) +(?<to>\d+)\)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

		/// <summary>
		/// The string types lookup.
		/// </summary>
		private readonly Dictionary<string, VHDLType> m_stringTypes;

		/// <summary>
		/// The builtin types lookup.
		/// </summary>
		private readonly Dictionary<string, VHDLType> m_builtins = new Dictionary<string, VHDLType>(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// The array types lookup
		/// </summary>
		private readonly Dictionary<string, VHDLType> m_arrays = new Dictionary<string, VHDLType>(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// The vector types lookup
		/// </summary>
		private readonly Dictionary<int, VHDLType> m_vectorTypes = new Dictionary<int, VHDLType>();

		/// <summary>
		/// The Mono.Cecil module definition
		/// </summary>
		private readonly ModuleDefinition m_resolveModule;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SME.VHDL.VHDLTypeScope"/> class.
		/// </summary>
		/// <param name="resolveModule">The module used to resolve types.</param>
		public VHDLTypeScope(ModuleDefinition resolveModule)
		{
			m_resolveModule = resolveModule;

			foreach (var p in typeof(VHDLTypes).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Where(x => x.FieldType == typeof(VHDLType)).Select(x => (VHDLType)x.GetValue(null)))
			{
				m_builtins[p.Name] = p;
				if (!string.IsNullOrWhiteSpace(p.Alias))
					m_builtins[p.Alias] = p;

				if (p.IsSystemType)
					m_arrays[p.Name + "_ARRAY"] = new VHDLType()
					{
						Name = p.Alias + "_ARRAY",
						IsArray = true,
						ElementName = p.Name
					};
			}

			m_stringTypes = new Dictionary<string, VHDLType>(m_builtins, StringComparer.OrdinalIgnoreCase);
			m_numericTypes = NUMERIC_TYPES.Select(x => resolveModule.Import(x)).ToArray();
			m_signedNumericTypes = SIGNED_NUMERIC_TYPES.Select(x => resolveModule.Import(x)).ToArray();
			m_unsignedNumericTypes = UNSIGNED_NUMERIC_TYPES.Select(x => resolveModule.Import(x)).ToArray();

			m_resolvedNumericTypes = m_numericTypes.Select(x => x.Resolve()).ToArray();
		}

		/// <summary>
		/// Gets all the builtin names
		/// </summary>
		public IEnumerable<string> BuiltinNames
		{
			get { return m_builtins.Keys; }
		}


		/// <summary>
		/// Gets a VHDL type from values
		/// </summary>
		/// <returns>The VHDL type.</returns>
		/// <param name="typename">The name of the type to get.</param>
		/// <param name="alias">The type alias to use.</param>
		/// <param name="type">The type in Mono.Cecil.</param>
		public VHDLType GetVHDLType(string typename, string alias, TypeReference type)
		{
			if (!string.IsNullOrWhiteSpace(alias) && m_stringTypes.ContainsKey(alias))
				return m_stringTypes[alias];

			if (m_stringTypes.ContainsKey(typename) && m_stringTypes[typename].Alias == alias)
				return m_stringTypes[typename];


			var m = _re.Match(typename);
			VHDLType res;
			if (m.Success && m.Length == typename.Length)
			{
				var fr = int.Parse(m.Groups["from"].Value);
				var to = int.Parse(m.Groups["to"].Value);

				if (alias == null && string.Equals(m.Groups["direction"].Value, "downto", StringComparison.OrdinalIgnoreCase))
					return GetStdLogicVector(Math.Max(fr, to) - Math.Min(fr, to) + 1);


				res = new VHDLType()
				{
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
				res = new VHDLType()
				{
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

		/// <summary>
		/// Gets a numeric equivalent type for a VHDL type
		/// </summary>
		/// <returns>The equivalent VHDL type.</returns>
		/// <param name="type">The type to find a numeric equivalent for.</param>
		/// <param name="throwIfNotFound">If set to <c>true</c> throws and exception if not found.</param>
		public VHDLType NumericEquivalent(VHDLType type, bool throwIfNotFound = true)
		{
			if (type.IsNumeric)
				return type;


			if (type == VHDLTypes.SYSTEM_UINT8)
				return VHDLTypes.NUMERIC_UINT8;
			else if (type == VHDLTypes.SYSTEM_UINT16)
				return VHDLTypes.NUMERIC_UINT16;
			else if (type == VHDLTypes.SYSTEM_UINT32)
				return VHDLTypes.NUMERIC_UINT32;
			else if (type == VHDLTypes.SYSTEM_UINT64)
				return VHDLTypes.NUMERIC_UINT64;
			else if (type == VHDLTypes.SYSTEM_INT8)
				return VHDLTypes.NUMERIC_INT8;
			else if (type == VHDLTypes.SYSTEM_INT16)
				return VHDLTypes.NUMERIC_INT16;
			else if (type == VHDLTypes.SYSTEM_INT32)
				return VHDLTypes.NUMERIC_INT32;
			else if (type == VHDLTypes.SYSTEM_INT64)
				return VHDLTypes.NUMERIC_INT64;
			else if (type.IsStdLogicVector && type.Length == 8)
				return VHDLTypes.NUMERIC_UINT8;
			else if (type.IsStdLogicVector && type.Length == 16)
				return VHDLTypes.NUMERIC_UINT16;
			else if (type.IsStdLogicVector && type.Length == 32)
				return VHDLTypes.NUMERIC_UINT32;
			else if (type.IsStdLogicVector && type.Length == 64)
				return VHDLTypes.NUMERIC_UINT64;
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

					if (m_signedNumericTypes.Any(x => x.IsSameTypeReference(t)))
					{
						var name = string.Format("SIGNED({0} downto {1})", type.UpperBound, type.LowerBound);

						if (m_stringTypes.ContainsKey(name))
							return m_stringTypes[name];

						return m_stringTypes[name] = new VHDLType()
						{
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

						return m_stringTypes[name] = new VHDLType()
						{
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

		/// <summary>
		/// Gets a system equivalent type for the given VHDL type
		/// </summary>
		/// <returns>The system type equivalent.</returns>
		/// <param name="type">The VHDL type to get a system equivalent.</param>
		public VHDLType SystemEquivalent(VHDLType type)
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

		/// <summary>
		/// Gets a STD_LOGIC_VECTOR of a size matching the given type
		/// </summary>
		/// <returns>The std_logic_vector equivalent.</returns>
		/// <param name="type">The type to get an equivalent for.</param>
		public VHDLType StdLogicVectorEquivalent(VHDLType type)
		{
			if (!type.IsStdLogicVector)
				throw new Exception(string.Format("Unable to find suitable std_logic_vector type for {0}", type.Alias ?? type.Name));

			return GetStdLogicVector(type.Length);
		}

		/// <summary>
		/// Gets the VHDL type for a parameter.
		/// </summary>
		/// <returns>The VHDL type.</returns>
		/// <param name="type">The parameter definition.</param>
		public VHDLType GetVHDLType(ParameterDefinition type)
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
				m_arrays[name] = new VHDLType()
				{
					IsArray = true,
					Name = name,
					ElementName = eltype.ToString()
				};
			}

			return m_arrays[name];
		}

		/// <summary>
		/// Gets the VHDL type for a property.
		/// </summary>
		/// <returns>The VHDL type.</returns>
		/// <param name="pi">The property definition.</param>
		public VHDLType GetVHDLType(System.Reflection.PropertyInfo pi)
		{
			var customtype = pi.GetCustomAttributes(typeof(VHDLTypeAttribute), true).FirstOrDefault() as VHDLTypeAttribute;
			if (customtype != null)
				return GetVHDLType(customtype, m_resolveModule.Import(pi.PropertyType));

			// Try on-type declaration
			customtype = pi.PropertyType.GetCustomAttributes(typeof(VHDLTypeAttribute), true).FirstOrDefault() as VHDLTypeAttribute;
			if (customtype != null)
				return GetVHDLType(customtype, m_resolveModule.Import(pi.PropertyType));

			if (!pi.PropertyType.IsArrayType())
				return GetVHDLType(pi.PropertyType);

			var eltype = GetVHDLType(pi.PropertyType.GetArrayElementType());

			var name =
				string.IsNullOrWhiteSpace(eltype.Alias)
				? pi.Name + "_ARRAY"
				: eltype.ToString() + "_ARRAY";

			if (!m_arrays.ContainsKey(name))
			{
				m_arrays[name] = new VHDLType()
				{
					IsArray = true,
					Name = name,
					ElementName = eltype.ToString()
				};
			}

			return m_arrays[name];
		}

		/// <summary>
		/// Gets the VHDL type from a type attribute
		/// </summary>
		/// <returns>The VHDL type.</returns>
		/// <param name="attr">The attribute.</param>
		/// <param name="type">The underlying type.</param>
		public VHDLType GetVHDLType(VHDLTypeAttribute attr, TypeReference type)
		{
			return GetVHDLType(attr.Type, attr.Alias, type);
		}


		/// <summary>
		/// Gets the vhdl type from a member definition
		/// </summary>
		/// <returns>The VHDL type.</returns>
		/// <param name="type">The member definition.</param>
		/// <param name="membertype">The forced type.</param>
		public VHDLType GetVHDLType(IMemberDefinition type, TypeReference membertype = null)
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

			VHDLType customvhdl = null;
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

				m_arrays[name] = new VHDLType()
				{
					IsArray = true,
					Name = name,
					ElementName = rt.ToString()
				};
			}

			return m_arrays[name];
		}

		/// <summary>
		/// Gets a VHDL type from its name
		/// </summary>
		/// <returns>The VHDL type.</returns>
		/// <param name="name">The name to find the type for.</param>
		public VHDLType GetByName(string name)
		{
			var res = TryGetByName(name);
			if (res == null)
				throw new Exception(string.Format("Unable to find type {0}", name));
			return res;

		}

		/// <summary>
		/// Tries to get a VHDL type from its name
		/// </summary>
		/// <returns>The VHDL type.</returns>
		/// <param name="name">The name to find the type for.</param>
		public VHDLType TryGetByName(string name)
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

		/// <summary>
		/// Gets the VHDL type from a type reference
		/// </summary>
		/// <returns>The VHDL type.</returns>
		/// <param name="type">The ty[e reference to get the VHDL type for.</param>
		public VHDLType GetVHDLType(TypeReference type)
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

		/// <summary>
		/// Gets the VHDL type from a System.Type.
		/// </summary>
		/// <returns>The VHDL type.</returns>
		/// <param name="type">The type to get the VHDL type for.</param>
		public VHDLType GetVHDLType(Type type)
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

		/// <summary>
		/// Gets a std_logic_vector of the given length
		/// </summary>
		/// <returns>The std_logic_vector type.</returns>
		/// <param name="length">The length of the vector.</param>
		public VHDLType GetStdLogicVector(int length)
		{
			if (!m_vectorTypes.ContainsKey(length))
				m_vectorTypes[length] = new VHDLType()
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

	/// <summary>
	/// Basic VHDL types
	/// </summary>
	public static class VHDLTypes
	{
		/// <summary>
		/// The INTEGER type
		/// </summary>
		public static readonly VHDLType INTEGER = new VHDLType()
		{
			Name = "INTEGER",
			IsArray = false
		};

		/// <summary>
		/// The UINT 8 UNSIGNED type
		/// </summary>
		public static readonly VHDLType NUMERIC_UINT8 = new VHDLType()
		{
			Name = "UNSIGNED(7 downto 0)",
			IsArray = true,
			ElementName = "STD_LOGIC",
			LowerBound = 0,
			UpperBound = 7
		};

		/// <summary>
		/// The UINT 16 UNSIGNED type
		/// </summary>
		public static readonly VHDLType NUMERIC_UINT16 = new VHDLType()
		{
			Name = "UNSIGNED(15 downto 0)",
			IsArray = true,
			ElementName = "STD_LOGIC",
			LowerBound = 0,
			UpperBound = 15
		};

		/// <summary>
		/// The UINT 32 UNSIGNED type
		/// </summary>
		public static readonly VHDLType NUMERIC_UINT32 = new VHDLType()
		{
			Name = "UNSIGNED(31 downto 0)",
			IsArray = true,
			ElementName = "STD_LOGIC",
			LowerBound = 0,
			UpperBound = 31
		};

		/// <summary>
		/// The UINT 64 UNSIGNED type
		/// </summary>
		public static readonly VHDLType NUMERIC_UINT64 = new VHDLType()
		{
			Name = "UNSIGNED(63 downto 0)",
			IsArray = true,
			ElementName = "STD_LOGIC",
			LowerBound = 0,
			UpperBound = 63
		};

		/// <summary>
		/// The INT 8 SIGNED type
		/// </summary>
		public static readonly VHDLType NUMERIC_INT8 = new VHDLType()
		{
			Name = "SIGNED(7 downto 0)",
			IsArray = true,
			ElementName = "STD_LOGIC",
			LowerBound = 0,
			UpperBound = 7
		};

		/// <summary>
		/// The INT 16 SIGNED type
		/// </summary>
		public static readonly VHDLType NUMERIC_INT16 = new VHDLType()
		{
			Name = "SIGNED(15 downto 0)",
			IsArray = true,
			ElementName = "STD_LOGIC",
			LowerBound = 0,
			UpperBound = 15
		};

		/// <summary>
		/// The INT 32 SIGNED type
		/// </summary>
		public static readonly VHDLType NUMERIC_INT32 = new VHDLType()
		{
			Name = "SIGNED(31 downto 0)",
			IsArray = true,
			ElementName = "STD_LOGIC",
			LowerBound = 0,
			UpperBound = 31
		};

		/// <summary>
		/// The INT 64 SIGNED type
		/// </summary>
		public static readonly VHDLType NUMERIC_INT64 = new VHDLType()
		{
			Name = "SIGNED(63 downto 0)",
			IsArray = true,
			ElementName = "STD_LOGIC",
			LowerBound = 0,
			UpperBound = 63
		};

		/// <summary>
		/// The BOOLEAN type
		/// </summary>
		public static readonly VHDLType BOOL = new VHDLType()
		{
			Name = "BOOLEAN",
			IsArray = false
		};

		/// <summary>
		/// The T_SYSTEM_BOOL type
		/// </summary>
		public static readonly VHDLType SYSTEM_BOOL = new VHDLType()
		{
			Name = "STD_LOGIC",
			Alias = "T_SYSTEM_BOOL",
			//SourceType = typeof(bool),
			IsArray = false,
		};

		/// <summary>
		/// The T_SYSTEM_UINT8 type
		/// </summary>
		public static readonly VHDLType SYSTEM_UINT8 = new VHDLType()
		{
			Name = "STD_LOGIC_VECTOR(7 downto 0)",
			Alias = "T_SYSTEM_UINT8",
			//SourceType = typeof(byte),
			IsArray = true,
			ElementName = "STD_LOGIC",
			LowerBound = 0,
			UpperBound = 7,
		};

		/// <summary>
		/// The T_SYSTEM_INT8 type
		/// </summary>
		public static readonly VHDLType SYSTEM_INT8 = new VHDLType()
		{
			Name = "STD_LOGIC_VECTOR(7 downto 0)",
			Alias = "T_SYSTEM_INT8",
			//SourceType = typeof(sbyte),
			IsArray = true,
			ElementName = "STD_LOGIC",
			LowerBound = 0,
			UpperBound = 7
		};

		/// <summary>
		/// The T_SYSTEM_UINT16 type
		/// </summary>
		public static readonly VHDLType SYSTEM_UINT16 = new VHDLType()
		{
			Name = "STD_LOGIC_VECTOR(15 downto 0)",
			Alias = "T_SYSTEM_UINT16",
			//SourceType = typeof(ushort),
			IsArray = true,
			ElementName = "STD_LOGIC",
			LowerBound = 0,
			UpperBound = 15
		};

		/// <summary>
		/// The T_SYSTEM_INT16 type
		/// </summary>
		public static readonly VHDLType SYSTEM_INT16 = new VHDLType()
		{
			Name = "STD_LOGIC_VECTOR(15 downto 0)",
			Alias = "T_SYSTEM_INT16",
			//SourceType = typeof(short),
			IsArray = true,
			ElementName = "STD_LOGIC",
			LowerBound = 0,
			UpperBound = 15
		};

		/// <summary>
		/// The T_SYSTEM_UINT32 type
		/// </summary>
		public static readonly VHDLType SYSTEM_UINT32 = new VHDLType()
		{
			Name = "STD_LOGIC_VECTOR(31 downto 0)",
			Alias = "T_SYSTEM_UINT32",
			//SourceType = typeof(uint),
			IsArray = true,
			ElementName = "STD_LOGIC",
			LowerBound = 0,
			UpperBound = 31
		};

		/// <summary>
		/// The T_SYSTEM_INT32 type
		/// </summary>
		public static readonly VHDLType SYSTEM_INT32 = new VHDLType()
		{
			Name = "STD_LOGIC_VECTOR(31 downto 0)",
			Alias = "T_SYSTEM_INT32",
			//SourceType = typeof(int),
			IsArray = true,
			ElementName = "std_logic",
			LowerBound = 0,
			UpperBound = 31
		};

		/// <summary>
		/// The T_SYSTEM_UINT64 type
		/// </summary>
		public static readonly VHDLType SYSTEM_UINT64 = new VHDLType()
		{
			Name = "STD_LOGIC_VECTOR(63 downto 0)",
			Alias = "T_SYSTEM_UINT64",
			//SourceType = typeof(ulong),
			IsArray = true,
			ElementName = "STD_LOGIC",
			LowerBound = 0,
			UpperBound = 63
		};

		/// <summary>
		/// The T_SYSTEM_INT64 type
		/// </summary>
		public static readonly VHDLType SYSTEM_INT64 = new VHDLType()
		{
			Name = "STD_LOGIC_VECTOR(63 downto 0)",
			Alias = "T_SYSTEM_INT64",
			//SourceType = typeof(long),
			IsArray = true,
			ElementName = "STD_LOGIC",
			LowerBound = 0,
			UpperBound = 63
		};
	}
}

