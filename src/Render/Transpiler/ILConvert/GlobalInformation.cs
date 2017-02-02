using System;
using System.Collections.Generic;
using Mono.Cecil;
using System.Linq;
using ICSharpCode.NRefactory.CSharp;

namespace SME.Render.Transpiler.ILConvert
{
	public abstract class GlobalInformation<TTypeClass> : IGlobalInformation
	{
		public Dictionary<string, Tuple<TypeReference, TTypeClass>> ExternalTypes { get; private set; }

		public Dictionary<PropertyDefinition, TTypeClass> BusArrays { get; private set; }

		public Dictionary<FieldDefinition, object> Constants { get; private set; }

		private List<TypeDefinition> m_types;

		public GlobalInformation()
		{
			ExternalTypes = new  Dictionary<string, Tuple<TypeReference, TTypeClass>>();
			Constants = new Dictionary<FieldDefinition, object>();
			BusArrays = new Dictionary<PropertyDefinition, TTypeClass>();
			m_types = new List<TypeDefinition>();

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

		public abstract TTypeClass GetOutputType(ParameterDefinition m);
		public abstract TTypeClass GetOutputType(IMemberDefinition m, TypeReference tr);
		public abstract TTypeClass GetOutputType(TypeReference tr);

		public abstract IMemberDefinition StoreType(IMemberDefinition member);

		public abstract string VHDLType(TypeReference tr);

		public abstract string VHDLType(IMemberDefinition m);

		public string VHDLType(MemberItem m)
		{
			return VHDLType(m.Item);
		}

		public abstract string ToValidName(string name);
		public abstract string ToComment(string comment);

		public abstract string AssemblyNameToFileName(IEnumerable<IProcess> processes);
		public abstract string ProcessNameToFileName(IProcess process);
		public abstract string ProcessNameToValidName(IProcess process);
		public abstract string BusSignalToValidName(IProcess process, System.Reflection.PropertyInfo pi);
		public abstract string AssemblyToValidName(IEnumerable<IProcess> processes);
		public abstract TTypeClass GetNativeIntegerType();
	}
}

