using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Mono.Cecil;
using SME.Render.Transpiler.ILConvert;
using SME.Render.VHDL.ILConvert.AugmentedExpression;

namespace SME.Render.VHDL.ILConvert
{
	public class VHDLGlobalInformation : SME.Render.Transpiler.ILConvert.GlobalInformation<VHDLTypeDescriptor>
	{
		public VHDLTypeScope VHDLTypes { get; private set; }

		public VHDLGlobalInformation()
		{
		}

		public VHDLGlobalInformation(ModuleDefinition resolveModule)
			: base()
		{
			VHDLTypes = new VHDLTypeScope(resolveModule);

		}

		public override IMemberDefinition StoreType(IMemberDefinition member)
		{
			if (member == null)
				return null;

			TypeReference decl;
			if (member is PropertyDefinition)
				decl = (member as PropertyDefinition).PropertyType;
			else if (member is FieldDefinition)
				decl = (member as FieldDefinition).FieldType;
			else if (member is MethodDefinition)
				return member;
			else
				throw new Exception($"Unable to store type {member}");

			if (decl.IsBusType())
			{
				foreach (var nx in decl.Resolve().Properties)
				{
					var key = nx.PropertyType.FullName.ToString();
					if (!ExternalTypes.ContainsKey(key))
						ExternalTypes[key] = new Tuple<TypeReference, VHDLTypeDescriptor>(nx.PropertyType, VHDLTypes.GetVHDLType(nx, nx.PropertyType));

					if (nx.IsFixedArrayType())
						BusArrays[nx] = VHDLTypes.GetVHDLType(nx, nx.PropertyType);
				}
			}
			else if (!decl.IsAssignableFrom<IProcess>())
			{
				var key = decl.FullName;
				if (!ExternalTypes.ContainsKey(key))
					ExternalTypes[key] = new Tuple<TypeReference, VHDLTypeDescriptor>(decl, VHDLTypes.GetVHDLType(member, decl));

				if (member is PropertyDefinition && decl.IsFixedArrayType() && member.DeclaringType != null && member.DeclaringType.IsBusType())
				{
					var pdef = member as PropertyDefinition;
					BusArrays[pdef] = VHDLTypes.GetVHDLType(pdef, decl);
				}

			}

			return member;
		}

		public override string VHDLType(TypeReference tr)
		{
			return VHDLTypes.GetVHDLType(tr).ToString();
		}

		public override string VHDLType(IMemberDefinition m)
		{
			StoreType(m);

			var attr = m.GetAttribute<VHDLTypeAttribute>();
			if (attr != null)
				return attr.ConstructorArguments.First().Value.ToString();

			if (m.IsArrayType())
				return m.DeclaringType.FullName + "." + m.Name + "_type";

			if (m is PropertyReference)
				return VHDLTypes.GetVHDLType(m, (m as PropertyReference).PropertyType).ToString();
			else if (m is FieldReference)
				return VHDLTypes.GetVHDLType(m, (m as FieldReference).FieldType).ToString();
			else
				throw new Exception($"Unable to determine VHDL type for {m}");
		}

		public override VHDLTypeDescriptor GetOutputType(ParameterDefinition m)
		{
			return VHDLTypes.GetVHDLType(m);
		}

		public override VHDLTypeDescriptor GetOutputType(IMemberDefinition m, TypeReference tr)
		{
			return VHDLTypes.GetVHDLType(m, tr);
		}

		public override VHDLTypeDescriptor GetOutputType(TypeReference tr)
		{
			return VHDLTypes.GetVHDLType(tr);
		}

		public override string AssemblyNameToFileName(IEnumerable<IProcess> processes)
		{
			return processes.First().GetType().Assembly.GetName().Name + ".vhdl";
		}

		public override string ProcessNameToFileName(IProcess process)
		{
			return ProcessNameToValidName(process) + ".vhdl";
		}

		public override string ProcessNameToValidName(IProcess process)
		{
			var processname = process.GetType().FullName;
			var asmname = process.GetType().Assembly.GetName().Name + '.';
			if (processname.StartsWith(asmname))
				processname = processname.Substring(asmname.Length);

			return ToValidName(processname);
		}

		public override string BusSignalToValidName(IProcess process, System.Reflection.PropertyInfo pi)
		{
			if (process != null && pi.DeclaringType.DeclaringType == process.GetType())
				return ToValidName(pi.DeclaringType.Name + '_' + pi.Name);

			var busname = pi.DeclaringType.FullName + '_' + pi.Name;
			var asmname = (process == null ? pi.DeclaringType : process.GetType()).Assembly.GetName().Name + '.';
			if (busname.StartsWith(asmname))
				busname = busname.Substring(asmname.Length);

			return ToValidName(busname);
		}

		public override string AssemblyToValidName(IEnumerable<IProcess> processes)
		{
			return ToValidName(processes.First().GetType().Assembly.GetName().Name);
		}

		private static Regex RX_ALPHANUMERIC = new Regex(@"[^\u0030-\u0039|\u0041-\u005A|\u0061-\u007A]");

		public override string ToValidName(string name)
		{
			var r = RX_ALPHANUMERIC.Replace(name, "_");
			if (new string[] { "register", "record", "variable", "process", "if", "then", "else", "begin", "end", "architecture", "of", "is" }.Contains(r.ToLowerInvariant()))
				r = "vhdl_" + r;
			return r;
		}

		public override string ToComment(string message)
		{
			return "-- " + message;
		}

		public override VHDLTypeDescriptor GetNativeIntegerType() { return VHDL.ILConvert.VHDLTypes.INTEGER; }
	}
}
