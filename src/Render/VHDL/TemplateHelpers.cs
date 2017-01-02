using System;
using System.Linq;
using System.Collections.Generic;
using SME.Render.VHDL.ILConvert;
using Mono.Cecil;
using ICSharpCode.NRefactory.CSharp;

namespace SME.Render.VHDL
{
	public partial class Entity
	{
		public Converter il;
		public IProcess proc;
		public Entity(Converter il, IProcess proc) 
		{ 
			this.il = il; 
			this.proc = proc;
		}

		public bool IsComponent { get { return proc is IVHDLComponent; } }

		private string ComponentName
		{
			get
			{
				var ca = proc.GetType().GetCustomAttributes(typeof(VHDLComponentAttribute), false).FirstOrDefault() as VHDLComponentAttribute;
				return ca == null ? Renderer.ConvertToValidVHDLName(proc.GetType().FullName) : ca.Name;
			}
		}

		public string ComponentSignals
		{
			get
			{
				if (!IsComponent)
					return null;

				return (proc as IVHDLComponent).SignalRegion(this.ComponentName, 2);
			}
		}

		public string ComponentProcesses
		{
			get
			{
				if (!IsComponent)
					return null;

				return (proc as IVHDLComponent).ProcessRegion(this.ComponentName, 4);
			}
		}
	}

	public partial class TopLevel
	{
		private readonly IEnumerable<Converter> Processes;
		private readonly GlobalInformation Information;

		public string AssemblyNameToVHDL { get { return Renderer.ConvertToValidVHDLName(Processes.First().ProcType.Module.Assembly.Name.Name); } }
		public TypeDefinition TopAssembly { get; private set; }

		public TopLevel(GlobalInformation information, IEnumerable<Converter> processes) 
		{ 
			Processes = processes;
			Information = information;
			TopAssembly = new TypeDefinition(Processes.First().ProcType.Namespace, "Dummy", TypeAttributes.Class);
			Processes.First().ProcType.Module.Types.Add(TopAssembly);
		}

		private class PortMapEntry
		{
			public string Type;
			public TypeDefinition Bus;
			public MemberItem Signal;
		}

		private IEnumerable<PortMapEntry> ListSignals(Converter p)
		{
			return (
				from bus in p.InputOnlyBusses
					from signal in bus.GetBusProperties()
					select new PortMapEntry() {Type = "Input", Bus = bus, Signal = signal}
	          ).Union(
					from bus in p.OutputOnlyBusses
					from signal in p.WrittenProperties(bus)
					select new PortMapEntry() {Type = "Output", Bus = bus, Signal = signal}
              ).Union(
					from bus in p.InputOutputBusses
					from signal in bus.GetBusProperties()
					select new PortMapEntry() {Type = "Input/output", Bus = bus, Signal = signal}
          	);
		}

		private bool IsClockedBus(TypeDefinition t)
		{
			return t.GetAttribute<ClockedBusAttribute>() != null;
		}

		public string DefaultValue(PropertyDefinition pd)
		{
			var init = pd.GetAttribute<InitialValueAttribute>();
			object def = null;
			if (init != null && init.HasConstructorArguments)
			{
				def = init.ConstructorArguments.First().Value;
				if (def is CustomAttributeArgument)
					def = ((CustomAttributeArgument)def).Value;
			}

			if (pd.PropertyType.IsType<bool>())
				return ((object)true).Equals(def) ? "'1'" : "'0'";
			else if (pd.PropertyType.Resolve().IsEnum)
			{
				if (def == null)
					return Renderer.ConvertToValidVHDLName(pd.PropertyType.FullName + "." + pd.PropertyType.Resolve().Fields.Skip(1).First().Name);
				else
					return Renderer.ConvertToValidVHDLName(pd.PropertyType.FullName + "." + pd.PropertyType.Resolve().Fields.Where(x => def.Equals(x.Constant)).First().Name);
			}

			if (def == null)
			{
				def = Converter.GetDefaultValue(pd.PropertyType);
				if (def != null)
				{
					var proptype = Information.VHDLType(pd); 
					if (proptype.StartsWith("T_", StringComparison.InvariantCultureIgnoreCase))
						proptype = proptype.Substring(2);
					return string.Format("{0}({1})", proptype, def);
				}
			}

			if (def == null)
				def = Converter.GetDefaultInitializer(Information, pd.PropertyType, pd);

			if (def == null)
				return "???";
			else
				return def.ToString();
		}

		private class TypeDefComp : IEqualityComparer<TypeDefinition>
		{
			#region IEqualityComparer implementation
			public bool Equals(TypeDefinition x, TypeDefinition y)
			{
				return x.FullName == y.FullName;
			}
			public int GetHashCode(TypeDefinition obj)
			{
				return obj.FullName.GetHashCode();
			}
			#endregion
		}

		public IEnumerable<TypeDefinition> ClockedBusses
		{
			get
			{
				return Processes.SelectMany(x => x.ClockedBusses).Distinct(new TypeDefComp());
			}
		}
	}

	public partial class TracefileTester
	{
		private class Signal 
		{
			public string Name;
			public Type Type;
			public VHDLTypeDescriptor VHDLType;
			public string VHDLTypeName;
			public bool IsDriver;
		}

		private IEnumerable<Signal> AllSignals { get; set; }
		private IEnumerable<Signal> AllSignalSplit { get; set; }

		private IEnumerable<Signal> VerifySignals { get { return AllSignalSplit.Where(x => !x.IsDriver); } }
		private IEnumerable<Signal> DriverSignals { get { return AllSignalSplit.Where(x => x.IsDriver); } }


		private string VHDLName { get; set; }
		private string Tracefile { get; set; }
		private int ClockPulseLength { get { return ClockLength / 2; } }
		private int ClockLength { get; set; }

		public TracefileTester(GlobalInformation info, IEnumerable<IProcess> processes, IEnumerable<CSVTracer.SignalEntry> props, string tracefilename = "../filename.csv", int clocklength = 10)
		{
			var all_split = new List<Signal>();
			var all = new List<Signal>();
			foreach (var s in props)
			{
				var name = Renderer.BusSignalNameToVHDLName(null, s.Property);
				
				var tn =
					s.Property.PropertyType.IsFixedArrayType()
					 ? Renderer.ConvertToValidVHDLName(s.Property.DeclaringType.FullName + "." + s.Property.Name + "_type")
					 : info.VHDLTypes.GetVHDLType(s.Property).ToSafeVHDLName();

				var bs = new Signal()
				{
					Name = name,
					Type = s.Property.PropertyType,
					VHDLType = info.VHDLTypes.GetVHDLType(s.Property),
					VHDLTypeName = tn,
					IsDriver = s.IsDriver
				};
				all.Add(bs);

				if (s.Property.PropertyType.IsFixedArrayType())
				{
					var eltype = s.Property.PropertyType.GetArrayElementType();

					foreach (var i in Enumerable.Range(0, s.Property.GetFixedArrayLength()))
						all_split.Add(new Signal()
						{
						Name = string.Format("{0}({1})", name, i),
							Type = eltype,
							VHDLType = info.VHDLTypes.GetVHDLType(eltype),
							VHDLTypeName = info.VHDLTypes.GetVHDLType(eltype).ToSafeVHDLName(),
							IsDriver = s.IsDriver
						});

				}
				else
				{
					all_split.Add(bs);
				}
			}

			AllSignals = all;
			AllSignalSplit = all_split;

			VHDLName = Renderer.AssemblyNameToVHDLName(processes);
			Tracefile = tracefilename;
			ClockLength = clocklength;
		}
	}

	public partial class CustomTypes
	{
		private GlobalInformation Information { get; set; }

		public class CustomType
		{
			public readonly TypeDefinition m_typedef;
			private readonly GlobalInformation m_info;

			public CustomType(TypeDefinition typedef, TypeReference tr, VHDLTypeDescriptor vhdltype, GlobalInformation info)
			{
				m_typedef = typedef;
				m_info = info;

				Name = vhdltype.ToString();
				Type = "type";
			}

			public string Name { get; private set; }
			public string Type { get; private set; }

			public IEnumerable<string> Members 
			{ 
				get 
				{
					if (m_typedef.IsEnum)
					{
						yield return string.Format(
							"({0});",
							string.Join("," + Environment.NewLine + "     ", 
								m_typedef.Fields.Where(x => x.Name != "value__").Select(m => Renderer.ConvertToValidVHDLName(m_typedef.FullName + "_" + m.Name)))
						);
					}
					else if (m_typedef.IsValueType && !m_typedef.IsPrimitive)
					{
						yield return "record";

						foreach (var m in m_typedef.Fields)
							if (!m.IsStatic)
								yield return string.Format("    {0}: {1};", Renderer.ConvertToValidVHDLName(m.Name), Renderer.ConvertToValidVHDLName(m_info.VHDLType(m)));


						yield return "end record;";
					}
				}
			}


		}

		public CustomTypes(GlobalInformation info)
		{
			Information = info;
		}

		public IEnumerable<CustomType> Types
		{
			get
			{
				
				var ignores = Information.VHDLTypes.BuiltinNames.ToDictionary(x => x, y => String.Empty);
				ignores["System.Void"] = string.Empty;


				return
					from n in Information.ExternalTypes
					let nr = n.Value.Item1.Resolve()
					where 
						!ignores.ContainsKey(n.Key)
						&&
						(nr.IsEnum || (nr.IsValueType && !nr.IsPrimitive))
						&&
						nr.GetAttribute<VHDLTypeAttribute>() == null
					select new CustomType(nr, n.Value.Item1, n.Value.Item2, Information);
			}
		}

		public IEnumerable<Tuple<string, VHDLTypeDescriptor, int>> BusArrays
		{
			get
			{
				return
					Information
						.BusArrays
						.Select(n =>
						        new Tuple<string, VHDLTypeDescriptor, int>(
							        n.Key.DeclaringType.FullName + "." + n.Key.Name, 
							        Information.VHDLTypes.GetByName(n.Value.ElementName), 
							        n.Key.GetFixedArrayLength())
						       )
						.Distinct();
			}
		}

		public IEnumerable<string> EnumTypes
		{
			get
			{
				return
					from n in Types
				 	where n.m_typedef.IsEnum
				 	select Renderer.ConvertToValidVHDLName(n.m_typedef.FullName);
					
			}
		}

		public IEnumerable<string> Constants
		{
			get
			{
				var lst = 
					from n in Types
					where n.m_typedef.IsValueType && !n.m_typedef.IsPrimitive
					from f in n.m_typedef.Fields
					where f.IsStatic && f.IsInitOnly
					select f;

				lst = lst.Union(Information.Constants.Keys).Distinct();

				foreach (var n in lst)
				{
					object nx = null;
					Exception nex = null;
					string convm = null;
					try
					{
						if (Information.Constants.ContainsKey(n))
							nx = Information.Constants[n];
						else
						{
							var lk = new ILConvert.Converter(n.DeclaringType, Information).ParseStaticConstructor();
							nx = lk[n.Name];
						}

						if (nx is ArrayCreateExpression)
						{
							var eltype = n.FieldType.GetElementType();
							convm = Information.VHDLType(eltype).Substring("T_".Length);
						}
						else
						{
							convm = Information.VHDLType(n).Substring("T_".Length);
						}
							
					}
					catch(Exception ex)
					{
						nex = ex;
					}

					if (nex != null)
					{
						foreach(var m in nex.Message.Split(new string[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries))
							yield return string.Format("-- {0}", m);
					}
					else
					{
						if (nx != null && convm != null)
						{
							if (nx is ArrayCreateExpression)
							{
								var arc = nx as ArrayCreateExpression;

								var varname = n.ToVHDLName(n.DeclaringType, null);
								var eltype = n.FieldType.GetElementType();
								var vhdl_eltype = Information.VHDLType(eltype);


								yield return string.Format("type {0}_type is array (0 to {1} - 1) of {2}", varname, arc.Initializer.Elements.Count, vhdl_eltype);

								string values;
								if (new [] { typeof(sbyte), typeof(byte), typeof(ushort), typeof(short), typeof(int) }.Select(x => eltype.Module.Import(x).Resolve()).Contains(eltype.Resolve()))
									values = string.Join(", ", arc.Initializer.Elements.Select(x => string.Format("{0}({1})", convm, x)));
								else
								{
									if (eltype.Resolve() == eltype.Module.Import(typeof(uint)).Resolve())
										values = string.Join(", ", arc.Initializer.Elements.Select(x => string.Format("\"{1}\"", convm, Convert.ToString((uint)(x as PrimitiveExpression).Value , 2).PadLeft(32, '0'))));
									else if (eltype.Resolve() == eltype.Module.Import(typeof(long)).Resolve())
										values = string.Join(", ", arc.Initializer.Elements.Select(x => string.Format("\"{1}\"", convm, Convert.ToString((long)(x as PrimitiveExpression).Value, 2).PadLeft(64, '0'))));
									/*else if (eltype.Resolve() == eltype.Module.Import(typeof(ulong)).Resolve())
										values = string.Join(", ", arc.Initializer.Elements.Select(x => string.Format("{0}({1})", convm, Convert.ToString((ulong)(x as PrimitiveExpression).Value, 2).PadLeft(64, '0'))));*/
									else
										values = " ??? unsupported type ??? ";
								}
								
								yield return string.Format("constant {0}: {0}_type := ({1})", varname, values);

							}
							else
							{
								yield return string.Format("constant {0}: {1} := {2}({3})", n.ToVHDLName(n.DeclaringType, null), Information.VHDLType(n), convm, nx);
							}
						}
						else
							yield return string.Format("-- constant {0}: {1} := ???", n.ToVHDLName(n.DeclaringType, null), Information.VHDLType(n));
					}

				}

			}
		}
	}

}


