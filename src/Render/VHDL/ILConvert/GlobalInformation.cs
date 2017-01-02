using System;
using System.Collections.Generic;
using Mono.Cecil;
using System.Linq;

namespace SME.Render.VHDL.ILConvert
{
	public class GlobalInformation
	{
		public VHDLTypeScope VHDLTypes { get; private set; }

		public Dictionary<string, Tuple<TypeReference, VHDLTypeDescriptor>> ExternalTypes { get; private set; }

		public Dictionary<PropertyDefinition, VHDLTypeDescriptor> BusArrays { get; private set; }

		public Dictionary<FieldDefinition, object> Constants { get; private set; }

		private List<TypeDefinition> m_types;


		public GlobalInformation(ModuleDefinition resolveModule)
		{
			ExternalTypes = new  Dictionary<string, Tuple<TypeReference, VHDLTypeDescriptor>>();
			Constants = new Dictionary<FieldDefinition, object>();
			BusArrays = new Dictionary<PropertyDefinition, VHDLTypeDescriptor>();
			m_types = new List<TypeDefinition>();
			VHDLTypes = new VHDLTypeScope(resolveModule);

		}

		public void AddTypeDefinition(TypeDefinition td)
		{
			m_types.Add(td);
		}

		public MemberItem StoreType(MemberItem item)
		{
			StoreType(item.Item);
			return item;
		}

		public IMemberDefinition StoreType(IMemberDefinition member)
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

		public string VHDLType(TypeReference tr)
		{
			return VHDLTypes.GetVHDLType(tr).ToString();
		}

		public string VHDLType(IMemberDefinition m)
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

		public string VHDLType(MemberItem m)
		{
			return VHDLType(m.Item);
		}
	}
}

