using System;
using Mono.Cecil;
using System.Linq;
using ICSharpCode.NRefactory.CSharp;
using System.Collections.Generic;

namespace SME.Render.VHDL.ILConvert
{
	public static class HelperMethods
	{
		public static bool HasInterface<T>(this TypeReference tr)
		{
			return tr.Resolve().HasInterface(typeof(T));
		}

		public static bool HasInterface<T>(this TypeDefinition td)
		{
			return td.HasInterface(typeof(T));
		}

		public static bool HasInterface(this TypeReference tr, Type t)
		{
			return tr.Resolve().HasInterface(t);
		}

		public static bool HasInterface(this TypeDefinition td, Type t)
		{
			return HasInterface(td, td.Module.Import(t).Resolve());
		}

		public static bool HasInterface(this TypeDefinition td, TypeDefinition b)
		{
			return td.Interfaces.Any(x => x.Resolve() == b);
		}

		public static bool IsBusType(this TypeReference tr)
		{
			return tr.HasInterface<IBus>();
		}

		public static bool IsBusType(this TypeDefinition td)
		{
			return td.HasInterface<IBus>();
		}

		public static CustomAttribute GetAttribute<T>(this IMemberDefinition mr)
		{
			return mr.GetAttribute(typeof(T));
		}

		public static CustomAttribute GetAttribute(this IMemberDefinition mr, Type t)
		{
			var n = mr.DeclaringType.Module.Import(t).Resolve();
			return mr.CustomAttributes.Where(x => x.AttributeType.Resolve() == n).FirstOrDefault();
		}

		public static IEnumerable<CustomAttribute> GetAttributes<T>(this IMemberDefinition mr)
		{
			return GetAttributes(mr, typeof(T));
		}

		public static IEnumerable<CustomAttribute> GetAttributes(this IMemberDefinition mr, Type t)
		{
			var n = mr.DeclaringType.Module.Import(t).Resolve();
			return mr.CustomAttributes.Where(x => x.AttributeType.Resolve() == n);
		}

		public static bool HasInputBusAttribute(this IMemberDefinition mr)
		{
			return mr.GetAttribute(typeof(InputBusAttribute)) != null;
		}

		public static bool HasOutputBusAttribute(this IMemberDefinition mr)
		{
			return mr.GetAttribute(typeof(OutputBusAttribute)) != null;
		}

		public static bool HasClockedBusAttribute(this IMemberDefinition mr)
		{
			return mr.GetAttribute(typeof(ClockedBusAttribute)) != null;
		}

		public static bool HasInternalBusAttribute(this IMemberDefinition mr)
		{
			return mr.GetAttribute(typeof(InternalBusAttribute)) != null;
		}

		public static CustomAttribute GetAttribute<T>(this TypeReference tr, Type t)
		{
			return tr.Resolve().GetAttribute(t);
		}

		public static CustomAttribute GetAttribute<T>(this TypeReference tr)
		{
			var trs = tr.Resolve();
			if (trs == null)
				throw new Exception(string.Format("Unable to resolve: {0}", tr.FullName));
			return trs.GetAttribute(typeof(T));
		}

		public static CustomAttribute GetAttribute<T>(this TypeDefinition td)
		{
			return td.GetAttribute(typeof(T));
		}

		public static CustomAttribute GetAttribute(this TypeDefinition td, Type t)
		{
			var n = td.Module.Import(t).Resolve();
			return SelfAndBases(td).SelectMany(x => x.CustomAttributes).Where(x => x.AttributeType.Resolve() == n).FirstOrDefault();
		}

		public static CustomAttribute GetAttribute<T>(this ParameterDefinition td)
		{
			return td.GetAttribute(typeof(T));
		}

		public static CustomAttribute GetAttribute(this ParameterDefinition td, Type t)
		{
			return td.CustomAttributes.Where(x => x.AttributeType.Resolve() == x.AttributeType.Module.Import(t).Resolve()).FirstOrDefault();
		}



		public static IEnumerable<TypeDefinition> SelfAndBases(this TypeDefinition td)
		{
			while (td != null)
			{
				yield return td;

				if (td.BaseType == null)
					yield break;
				
				td = td.BaseType.Resolve();
			}
		}

		public static bool IsAssignableFrom<T>(this TypeReference tr)
		{
			return tr.Resolve().IsAssignableFrom(typeof(T));
		}

		public static bool IsAssignableFrom(this TypeReference tr, Type t)
		{
			return tr.Resolve().IsAssignableFrom(t);
		}

		public static bool IsAssignableFrom<T>(this TypeDefinition td)
		{
			return td.IsAssignableFrom(typeof(T));
		}

		public static bool IsAssignableFrom(this TypeDefinition td, Type t)
		{
			return IsAssignableFrom(td, td.Module.Import(t));
		}

		public static bool IsAssignableFrom(this TypeDefinition td, TypeReference n)
		{
			return IsAssignableFrom(td, n.Resolve());
		}

		public static bool IsAssignableFrom(this TypeDefinition td, TypeDefinition n)
		{
			TypeReference p = td;
			while (p != null)
			{
				var x = p.Resolve();
				if (x == n)
					return true;
				if (n.IsInterface && x.HasInterface(n))
					return true;
				p = x.BaseType;
			}

			return false;
		}

		public static bool IsType<T>(this TypeReference tr)
		{
			return tr.Resolve().IsType<T>();
		}

		public static bool IsType<T>(this TypeDefinition td)
		{
			return IsType(td, typeof(T));
		}

		public static bool IsType(this TypeReference tr, Type t)
		{
			return tr.Resolve().IsType(t);
		}

		public static bool IsType(this TypeDefinition td, Type t)
		{
			return td == td.Module.Import(t).Resolve();
		}

		public static IEnumerable<TypeReference> ImplicitConversions(this TypeReference tr)
		{
			return tr.Resolve().ImplicitConversions();
		}

		public static IEnumerable<TypeReference> ImplicitConversions(this TypeDefinition td)
		{
			return 
				from m in td.Methods
				where m.Name == "op_Implicit" && m.IsSpecialName && m.IsHideBySig && m.HasParameters && m.Parameters.Count() == 1
				select m.Parameters.First().ParameterType.Resolve() == td ? m.ReturnType : m.Parameters.First().ParameterType;
		}

		public static bool IsSameTypeReference(this TypeReference a, Type b)
		{
			return IsSameTypeReference(a, a.Module.Import(b));
		}

		public static bool IsSameTypeReference<T>(this TypeReference a)
		{
			return IsSameTypeReference(a, typeof(T));
		}

		public static bool IsSameTypeReference(this TypeReference a, TypeReference b)
		{
			return a.FullName == b.FullName;
		}

		public static FieldDefinition GetFieldRecursive(this TypeDefinition self, string name)
		{
			return GetFieldsRecursive(self).Where(x => x.Name == name).FirstOrDefault();
		}

		public static IEnumerable<FieldDefinition> GetFieldsRecursive(this TypeDefinition self)
		{
			var x = self;
			while (x != null)
			{
				foreach(var f in x.Fields)
					yield return f;

				if (x.BaseType == null)
					yield break;
				
				x = x.BaseType.Resolve();
			}
		}

		public static IEnumerable<MemberItem> GetBusProperties(this TypeDefinition self)
		{
			var bustype = self.Module.Import(typeof(IBus));
			return GetPropertiesRecursive(self).Where(x => !x.DeclaringType.IsSameTypeReference(bustype)).Select(x => new MemberItem(null, x, self));
		}

		public static IEnumerable<PropertyDefinition> GetPropertiesRecursive(this TypeDefinition self)
		{
			var x = self;
			while (x != null)
			{
				foreach(var f in x.Properties)
					yield return f;

				if (x.HasInterfaces)
					foreach(var n in x.Interfaces)
						foreach(var f in n.Resolve().Properties)
							yield return f;

				if (x.BaseType == null)
					yield break;
				x = x.BaseType.Resolve();
			}
		}

		public static string GetVHDLInOut(this ParameterDefinition n)
		{
			var inarg = (n.Attributes & ParameterAttributes.Out) != ParameterAttributes.Out;
			var outarg = (n.Attributes & ParameterAttributes.Out) == ParameterAttributes.Out && (n.Attributes & ParameterAttributes.In) != ParameterAttributes.In;
			var inoutarg = (n.Attributes & ParameterAttributes.In) == ParameterAttributes.In && (n.Attributes & ParameterAttributes.Out) == ParameterAttributes.Out;
			var inoutoverride = (n.Attributes & ParameterAttributes.Out) == ParameterAttributes.Out || (n.Attributes & ParameterAttributes.In) == ParameterAttributes.In;
			var isarray = n.ParameterType.IsArray;
			var argrange = n.GetAttribute<VHDLRangeAttribute>();
			return inoutarg || (isarray && !inoutoverride) ? "inout" : (inarg ? "in" : "out");
		}

		public static bool IsFixedArrayType(this MemberItem member)
		{
			return IsFixedArrayType(member.Item);
		}

		public static bool IsFixedArrayType(this IMemberDefinition member)
		{
			return IsFixedArrayType(GetMemberType(member));
		}

		public static bool IsArrayType(this MemberItem member)
		{
			return IsArrayType(member.Item);
		}

		public static bool IsArrayType(this IMemberDefinition member)
		{
			return IsArrayType(GetMemberType(member));
		}

		public static bool IsArrayType(this TypeReference tr)
		{
			return tr.IsArray || tr.IsFixedArrayType();
		}

		public static bool IsArrayType(this Type t)
		{
			return t.IsArray || t.IsFixedArrayType();
		}

		public static bool IsFixedArrayType(this TypeReference tr)
		{
			return tr.IsGenericInstance && tr.GetElementType().IsSameTypeReference(typeof(IFixedArray<>));
		}

		public static bool IsFixedArrayType(this Type t)
		{
			return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IFixedArray<>);
		}

		public static TypeReference GetMemberType(MemberItem member)
		{
			return GetMemberType(member.Item);
		}

		public static TypeReference GetMemberType(IMemberDefinition member)
		{
			if (member is PropertyDefinition)
				return (member as PropertyDefinition).PropertyType;
			else if (member is FieldDefinition)
				return (member as FieldDefinition).FieldType;
			else if (member is MethodDefinition)
				return (member as MethodDefinition).ReturnType;
			else
				throw new Exception($"Usupported IMemberDefinition {member.GetType().FullName}");
		}

		public static TypeReference GetArrayElementType(this MemberItem member)
		{
			return GetArrayElementType(member.Item);
		}

		public static TypeReference GetArrayElementType(this IMemberDefinition member)
		{
			return GetArrayElementType(GetMemberType(member));
		}

		public static TypeReference GetArrayElementType(this TypeReference tr)
		{
			if (tr.IsArray)
				return tr.GetElementType();
			else if (tr.IsFixedArrayType())
				return (tr as GenericInstanceType).GenericArguments.First();
			else
				throw new Exception($"GetArrayElementType called on non-array: {tr.FullName}");
		}

		public static Type GetArrayElementType(this Type t)
		{
			if (t.IsArray)
				return t.GetElementType();
			else if (t.IsFixedArrayType())
				return t.GetGenericArguments().First();
			else
				throw new Exception($"GetArrayElementType called on non-array: {t.FullName}");
		}

		public static int GetFixedArrayLength(this MemberItem member)
		{
			return GetFixedArrayLength(member.Item);
		}

		public static int GetFixedArrayLength(this IMemberDefinition member)
		{
			var attr = member.GetAttribute<FixedArrayLengthAttribute>();
			var arg = attr.ConstructorArguments.First().Value;
			return (int)Convert.ChangeType(arg, typeof(int));			
		}

		public static int GetFixedArrayLength(this System.Reflection.MemberInfo member)
		{
			var attr = member.GetCustomAttributes(typeof(FixedArrayLengthAttribute), false).Cast<FixedArrayLengthAttribute>().First();
			return attr.Length;
		}


	}
}

