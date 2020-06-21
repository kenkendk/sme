﻿using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SME.AST
{
	/// <summary>
	/// A collection of static extension methods
	/// </summary>
	public static class HelperMethods
	{
		/// <summary>
		/// Returns <c>true</c> if the type reference implements the given interface
		/// </summary>
		/// <returns><c>true</c>, if interface was implemented, <c>false</c> otherwise.</returns>
		/// <param name="tr">The type reference to evaluate.</param>
		/// <typeparam name="T">The interface to test for.</typeparam>
		public static bool HasInterface<T>(this TypeReference tr)
		{
			return tr.Resolve().HasInterface(typeof(T));
		}

		/// <summary>
		/// Returns <c>true</c> if the type definition implements the given interface
		/// </summary>
		/// <returns><c>true</c>, if interface was implemented, <c>false</c> otherwise.</returns>
		/// <param name="td">The type definition to evaluate.</param>
		/// <typeparam name="T">The interface to test for.</typeparam>
		public static bool HasInterface<T>(this TypeDefinition td)
		{
			return td.HasInterface(typeof(T));
		}

		/// <summary>
		/// Returns <c>true</c> if the type reference implements the given interface
		/// </summary>
		/// <returns><c>true</c>, if interface was implemented, <c>false</c> otherwise.</returns>
		/// <param name="tr">The type reference to evaluate.</param>
		/// <param name="t">The interface to test for.</param>
		public static bool HasInterface(this TypeReference tr, Type t)
		{
			return tr.Resolve().HasInterface(t);
		}

		/// <summary>
		/// Returns <c>true</c> if the type definition implements the given interface
		/// </summary>
		/// <returns><c>true</c>, if interface was implemented, <c>false</c> otherwise.</returns>
		/// <param name="td">The type definition to evaluate.</param>
		/// <param name="t">The interface to test for.</param>
		public static bool HasInterface(this TypeDefinition td, Type t)
		{
			return HasInterface(td, td.Module.ImportReference(t).Resolve());
		}

        /// <summary>
        /// Returns <c>true</c> if the type definition implements the given interface
        /// </summary>
        /// <returns><c>true</c>, if interface was implemented, <c>false</c> otherwise.</returns>
        /// <param name="td">The type definition to evaluate.</param>
        /// <param name="b">The interface to test for.</param>
        public static bool HasInterface(this TypeDefinition td, TypeDefinition b)
        {
        	return td.Interfaces.Any(x => x.InterfaceType.Resolve() == b);
        }

        /// <summary>
        /// Returns a value indicating if the type is a Bus type
        /// </summary>
        /// <returns><c>true</c>, if the type reference is a bus type, <c>false</c> otherwise.</returns>
        /// <param name="tr">The type to evaluate.</param>
        public static bool IsBusType(this TypeReference tr)
		{
			return tr.HasInterface<IBus>();
		}

		/// <summary>
		/// Returns a value indicating if the type is a Bus type
		/// </summary>
		/// <returns><c>true</c>, if the type definition is a bus type, <c>false</c> otherwise.</returns>
		/// <param name="td">The type to evaluate.</param>
		public static bool IsBusType(this INamedTypeSymbol td)
		{
			return td.Interfaces.Any(x => Type.GetType(x.ToDisplayString()) == typeof(IBus));
			//return td.HasInterface<IBus>();
		}

		public static bool IsBusType(this ITypeSymbol its)
		{
			return ((INamedTypeSymbol)its).IsBusType();
		}

		/// <summary>
		/// Returns <c>true</c> if the type has an attribute of the given type
		/// </summary>
		/// <returns><c>true</c>, if the type has an attribute of the given type, <c>false</c> otherwise.</returns>
		/// <param name="bt">The type to evaluate.</param>
		/// <typeparam name="T">The attribute type to check for.</typeparam>
		public static bool HasAttribute<T>(this Type bt)
		{
			return HasAttribute(bt, typeof(T));
		}

		/// <summary>
		/// Returns <c>true</c> if the type has an attribute of the given type
		/// </summary>
		/// <returns><c>true</c>, if the type has an attribute of the given type, <c>false</c> otherwise.</returns>
		/// <param name="bt">The type to evaluate.</param>
		/// <param name="attrtype">The attribute type to check for.</param>
		public static bool HasAttribute(this Type bt, Type attrtype)
		{
			return bt.GetCustomAttributes(attrtype, true).Any();
		}

		/// <summary>
		/// Returns <c>true</c> if the member has an attribute of the given type
		/// </summary>
		/// <returns><c>true</c>, if the member has an attribute of the given type, <c>false</c> otherwise.</returns>
		/// <param name="mr">The member to evaluate.</param>
		/// <typeparam name="T">The attribute type to check for.</typeparam>
		public static bool HasAttribute<T>(this IMemberDefinition mr)
		{
			return mr.HasAttribute(typeof(T));
		}

		/// <summary>
		/// Returns <c>true</c> if the member has an attribute of the given type
		/// </summary>
		/// <returns><c>true</c>, if the member has an attribute of the given type, <c>false</c> otherwise.</returns>
		/// <param name="mr">The member to evaluate.</param>
		/// <param name="t">The attribute type to check for.</param>
		public static bool HasAttribute(this IMemberDefinition mr, Type t)
		{
			return GetAttribute(mr, t) != null;
		}

		public static bool HasAttribute(this ISymbol its, Type t)
		{
			return its.GetAttribute(t) != null;
		}

		public static bool HasAttribute<T>(this ISymbol its)
		{
			return its.GetAttribute<T>() != null;
		}

		public static AttributeData GetAttribute(this ISymbol its, Type t)
		{
			return its.GetAttributes(t).FirstOrDefault();
		}

		public static AttributeData GetAttribute<T>(this ISymbol its)
		{
			return its.GetAttributes<T>().FirstOrDefault();
		}

		public static IEnumerable<AttributeData> GetAttributes(this ISymbol its, Type t)
		{
			return its.GetAttributes()
				.Where(x => Type.GetType(x.AttributeClass.ToDisplayString()) == t);
		}

		public static IEnumerable<AttributeData> GetAttributes<T>(this ISymbol its)
		{
			return its.GetAttributes()
				.Where(x => Type.GetType(x.AttributeClass.ToDisplayString()) == typeof(T));
		}

		public static ISymbol LoadSymbol(this SyntaxNode sn, IEnumerable<SemanticModel> m_semantics)
		{
			return m_semantics
				.Select(x => x.GetDeclaredSymbol(sn))
				.First(x => x != null);
		}

		public static SyntaxNode GetSyntax(this ISymbol isy)
		{
			return isy.DeclaringSyntaxReferences.First().GetSyntax();
		}

		public static ClassDeclarationSyntax GetClassDecl(this ISymbol isy)
		{
			return isy.GetSyntax() as ClassDeclarationSyntax;
		}

		/// <summary>
		/// Gets the custom attribute instance from the member, or null.
		/// </summary>
		/// <returns>The custom attribute.</returns>
		/// <param name="mr">The member to examine.</param>
		/// <typeparam name="T">The type of attribute to find.</typeparam>
		public static CustomAttribute GetAttribute<T>(this IMemberDefinition mr)
		{
			return mr.GetAttribute(typeof(T));
		}

		/// <summary>
		/// Gets the custom attribute instance from the member, or null.
		/// </summary>
		/// <returns>The custom attribute.</returns>
		/// <param name="mr">The member to examine.</param>
		/// <param name="t">The type of attribute to find parameter.</param>
		public static CustomAttribute GetAttribute(this IMemberDefinition mr, Type t)
		{
			return GetAttributes(mr, t).FirstOrDefault();
		}

		/// <summary>
		/// Gets all the attributes of a specific type from the member
		/// </summary>
		/// <returns>The attributes.</returns>
		/// <param name="mr">The member to examine.</param>
		/// <typeparam name="T">The type of attributes to find.</typeparam>
		public static IEnumerable<CustomAttribute> GetAttributes<T>(this IMemberDefinition mr)
		{
			return GetAttributes(mr, typeof(T));
		}

		/// <summary>
		/// Gets all the attributes of a specific type from the member
		/// </summary>
		/// <returns>The attributes.</returns>
		/// <param name="mr">The member to examine.</param>
		/// <param name="t">The type of attributes to find.</param>
		public static IEnumerable<CustomAttribute> GetAttributes(this IMemberDefinition mr, Type t)
		{
			var n = mr.DeclaringType.Module.ImportReference(t).Resolve();
			return mr.CustomAttributes.Where(x => x.AttributeType.Resolve() == n);
		}

		/// <summary>
		/// Returns a value indicating if the member has the <see cref="InputBusAttribute"/> attribute
		/// </summary>
		/// <returns><c>true</c>, if the member has the InputBusAttribute, <c>false</c> otherwise.</returns>
		/// <param name="mr">The member to examine.</param>
		public static bool HasInputBusAttribute(this IMemberDefinition mr)
		{
			return mr.GetAttribute(typeof(InputBusAttribute)) != null;
		}

		/// <summary>
		/// Returns a value indicating if the member has the <see cref="OutputBusAttribute"/> attribute
		/// </summary>
		/// <returns><c>true</c>, if the member has the OutputBusAttribute, <c>false</c> otherwise.</returns>
		/// <param name="mr">The member to examine.</param>
		public static bool HasOutputBusAttribute(this IMemberDefinition mr)
		{
			return mr.GetAttribute(typeof(OutputBusAttribute)) != null;
		}

		/// <summary>
		/// Returns a value indicating if the member has the <see cref="ClockedBusAttribute"/> attribute
		/// </summary>
		/// <returns><c>true</c>, if the member has the ClockedBusAttribute, <c>false</c> otherwise.</returns>
		/// <param name="mr">The member to examine.</param>
		public static bool HasClockedBusAttribute(this IMemberDefinition mr)
		{
			return mr.GetAttribute(typeof(ClockedBusAttribute)) != null;
		}

		/// <summary>
		/// Returns a value indicating if the member has the <see cref="InternalBusAttribute"/> attribute
		/// </summary>
		/// <returns><c>true</c>, if the member has the InternalBusAttribute, <c>false</c> otherwise.</returns>
		/// <param name="mr">The member to examine.</param>
		public static bool HasInternalBusAttribute(this IMemberDefinition mr)
		{
			return mr.GetAttribute(typeof(InternalBusAttribute)) != null;
		}

		/// <summary>
		/// Gets the custom attribute instance from the type reference, or null.
		/// </summary>
		/// <returns>The custom attribute.</returns>
		/// <param name="tr">The type reference to examine.</param>
		/// <param name="t">The type of attribute to find.</param>
		public static CustomAttribute GetAttribute(this TypeReference tr, Type t)
		{
			return tr.Resolve().GetAttribute(t);
		}

		/// <summary>
		/// Gets the custom attribute instance from the type reference, or null.
		/// </summary>
		/// <returns>The custom attribute.</returns>
		/// <param name="tr">The type reference to examine.</param>
		/// <typeparam name="T">The type of attribute to find.</typeparam>
		public static CustomAttribute GetAttribute<T>(this TypeReference tr)
		{
			var trs = tr.Resolve();
			if (trs == null)
				throw new Exception(string.Format("Unable to resolve: {0}", tr.FullName));
			return trs.GetAttribute(typeof(T));
		}

		/// <summary>
		/// Gets the custom attribute instance from the type definition, or null.
		/// </summary>
		/// <returns>The custom attribute.</returns>
		/// <param name="td">The type definition to examine.</param>
		/// <typeparam name="T">The type of attribute to find.</typeparam>
		public static CustomAttribute GetAttribute<T>(this TypeDefinition td)
		{
			return td.GetAttribute(typeof(T));
		}

		/// <summary>
		/// Gets the custom attribute instance from the type definition, or null.
		/// </summary>
		/// <returns>The custom attribute.</returns>
		/// <param name="td">The type definition to examine.</param>
		/// <param name="t">The type of attribute to find.</param>
		public static CustomAttribute GetAttribute(this TypeDefinition td, Type t)
		{
			var n = td.Module.ImportReference(t);
			return SelfAndBases(td).SelectMany(x => x.CustomAttributes).FirstOrDefault(x => x.AttributeType.IsSameTypeReference(n));
		}

		/// <summary>
		/// Gets the custom attribute instance from the parameter definition, or null.
		/// </summary>
		/// <returns>The custom attribute.</returns>
		/// <param name="td">The type parameter to examine.</param>
		/// <typeparam name="T">The type of attribute to find.</typeparam>
		public static CustomAttribute GetAttribute<T>(this ParameterDefinition td)
		{
			return td.GetAttribute(typeof(T));
		}

		/// <summary>
		/// Gets the custom attribute instance from the parameter definition, or null.
		/// </summary>
		/// <returns>The custom attribute.</returns>
		/// <param name="td">The parameter definition to examine.</param>
		/// <param name="t">The type of attribute to find.</param>
		public static CustomAttribute GetAttribute(this ParameterDefinition td, Type t)
		{
			return td.CustomAttributes.FirstOrDefault(x => x.AttributeType.Resolve() == x.AttributeType.Module.ImportReference(t).Resolve());
		}

		/// <summary>
		/// Gets the type of the element and all its base types
		/// </summary>
		/// <returns>The type and its and bases.</returns>
		/// <param name="td">The type to get all base types for.</param>
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

		/// <summary>
		/// Returns a value indicating if the type reference can be assigned from the given type.
		/// </summary>
		/// <returns><c>true</c>, if <paramref name="tr"/> is assignable from <typeparamref name="T"/>, <c>false</c> otherwise.</returns>
		/// <param name="tr">The type to tests.</param>
		/// <typeparam name="T">The type to attempt to assign to.</typeparam>
		public static bool IsAssignableFrom<T>(this TypeReference tr)
		{
			return tr.Resolve().IsAssignableFrom(typeof(T));
		}

		/// <summary>
		/// Returns a value indicating if the type reference can be assigned from the given type.
		/// </summary>
		/// <returns><c>true</c>, if <paramref name="tr"/> is assignable from <paramref name="t"/>, <c>false</c> otherwise.</returns>
		/// <param name="tr">The type to tests.</param>
		/// <param name="t">The type to attempt to assign to.</param>
		public static bool IsAssignableFrom(this TypeReference tr, Type t)
		{
			return tr.Resolve().IsAssignableFrom(t);
		}

		/// <summary>
		/// Returns a value indicating if the type can be assigned from the given type.
		/// </summary>
		/// <returns><c>true</c>, if <paramref name="td"/> is assignable from <typeparamref name="T"/>, <c>false</c> otherwise.</returns>
		/// <param name="td">The type to tests.</param>
		/// <typeparam name="T">The type to attempt to assign to.</typeparam>
		public static bool IsAssignableFrom<T>(this TypeDefinition td)
		{
			return td.IsAssignableFrom(typeof(T));
		}

		/// <summary>
		/// Returns a value indicating if the type can be assigned from the given type.
		/// </summary>
		/// <returns><c>true</c>, if <paramref name="td"/> is assignable from <paramref name="t"/>, <c>false</c> otherwise.</returns>
		/// <param name="td">The type to tests.</param>
		/// <param name="t">The type to attempt to assign to.</param>
		public static bool IsAssignableFrom(this TypeDefinition td, Type t)
		{
			return IsAssignableFrom(td, td.Module.ImportReference(t));
		}

		/// <summary>
		/// Returns a value indicating if the type can be assigned from the given type.
		/// </summary>
		/// <returns><c>true</c>, if <paramref name="td"/> is assignable from <paramref name="n"/>, <c>false</c> otherwise.</returns>
		/// <param name="td">The type to tests.</param>
		/// <param name="n">The type to attempt to assign to.</param>
		public static bool IsAssignableFrom(this TypeDefinition td, TypeReference n)
		{
			return IsAssignableFrom(td, n.Resolve());
		}

		/// <summary>
		/// Returns a value indicating if the type can be assigned from the given type.
		/// </summary>
		/// <returns><c>true</c>, if <paramref name="td"/> is assignable from <paramref name="n"/>, <c>false</c> otherwise.</returns>
		/// <param name="td">The type to tests.</param>
		/// <param name="n">The type to attempt to assign to.</param>
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

		/// <summary>
		/// Returns a value indicating if the type reference is the given type.
		/// </summary>
		/// <returns><c>true</c>, if <paramref name="tr"/> is the same type as <typeparamref name="T"/>, <c>false</c> otherwise.</returns>
		/// <param name="tr">The type to tests.</param>
		/// <typeparam name="T">The type to test for equality with.</typeparam>
		public static bool IsType<T>(this TypeReference tr)
		{
			return tr.Resolve().IsType<T>();
		}

		/// <summary>
		/// Returns a value indicating if the type definition is the given type.
		/// </summary>
		/// <returns><c>true</c>, if <paramref name="td"/> is the same type as <typeparamref name="T"/>, <c>false</c> otherwise.</returns>
		/// <param name="td">The type to tests.</param>
		/// <typeparam name="T">The type to test for equality with.</typeparam>
		public static bool IsType<T>(this TypeDefinition td)
		{
			return IsType(td, typeof(T));
		}

		/// <summary>
		/// Returns a value indicating if the type reference is the given type.
		/// </summary>
		/// <returns><c>true</c>, if <paramref name="tr"/> is the same type as <paramref name="t"/>, <c>false</c> otherwise.</returns>
		/// <param name="tr">The type to tests.</param>
		/// <param name="t">The type to test for equality with.</param>
		public static bool IsType(this TypeReference tr, Type t)
		{
			return tr.Module.ImportReference(t).FullName == tr.FullName;
		}

		/// <summary>
		/// Returns a value indicating if the type definition is the given type.
		/// </summary>
		/// <returns><c>true</c>, if <paramref name="td"/> is the same type as <paramref name="t"/>, <c>false</c> otherwise.</returns>
		/// <param name="td">The type to tests.</param>
		/// <param name="t">The type to test for equality with.</param>
		public static bool IsType(this TypeDefinition td, Type t)
		{
			return td.Module.ImportReference(t).Resolve().FullName == td.FullName;
		}

		/// <summary>
		/// Returns the types that the given type reference can be implicitly converted to
		/// </summary>
		/// <returns>The implicit conversion types.</returns>
		/// <param name="tr">The type reference to examine.</param>
		public static IEnumerable<TypeReference> ImplicitConversions(this TypeReference tr)
		{
			return tr.Resolve().ImplicitConversions();
		}

		/// <summary>
		/// Returns the types that the given type definition can be implicitly converted to
		/// </summary>
		/// <returns>The implicit conversion types.</returns>
		/// <param name="td">The type definition to examine.</param>
		public static IEnumerable<TypeReference> ImplicitConversions(this TypeDefinition td)
		{
			return
				from m in td.Methods
				where m.Name == "op_Implicit" && m.IsSpecialName && m.IsHideBySig && m.HasParameters && m.Parameters.Count() == 1
				select m.Parameters.First().ParameterType.Resolve() == td ? m.ReturnType : m.Parameters.First().ParameterType;
		}

		public static bool IsSameTypeReference(this ITypeSymbol a, ITypeSymbol b)
		{
			return a.Equals(b);
		}

		public static bool IsSameTypeReference(this ITypeSymbol a, Type b)
		{
			var itb = a.ContainingAssembly.GetTypeByMetadataName(b.FullName);
			return a.IsSameTypeReference(itb);
		}

		public static bool IsSameTypeReference<T>(this ITypeSymbol a)
		{
			return a.IsSameTypeReference(typeof(T));
		}

		/*/// <summary>
		/// Returns a value indicating if the types are the same
		/// </summary>
		/// <returns><c>true</c>, if the types are the same, <c>false</c> otherwise.</returns>
		/// <param name="a">The type reference to examine.</param>
		/// <param name="b">The type to test for equality with.</param>
		public static bool IsSameTypeReference(this ISymbol a, Type b)
		{
			var ta = Type.GetType(a.ToDisplayString());
			return ta == b;
		}

		/// <summary>
		/// Returns a value indicating if the types are the same
		/// </summary>
		/// <returns><c>true</c>, if the types are the same, <c>false</c> otherwise.</returns>
		/// <param name="a">The type reference to examine.</param>
		/// <typeparam name="T">The type to test for equality with.</typeparam>
		public static bool IsSameTypeReference<T>(this ISymbol a)
		{
			return IsSameTypeReference(a, typeof(T));
		}

		/// <summary>
		/// Returns a value indicating if the types are the same
		/// </summary>
		/// <returns><c>true</c>, if the types are the same, <c>false</c> otherwise.</returns>
		/// <param name="a">The type reference to examine.</param>
		/// <param name="b">The type to test for equality with.</param>
		public static bool IsSameTypeReference(this ISymbol a, ISymbol b)
		{
			var tb = Type.GetType(b.ToDisplayString());
			return IsSameTypeReference(a, tb);
		}*/

		/// <summary>
		/// Gets a field in the given type or its base types, with the given name
		/// </summary>
		/// <returns>The field definition or null.</returns>
		/// <param name="self">The type to examine.</param>
		/// <param name="name">The name of the field to find.</param>
		public static FieldDefinition GetFieldRecursive(this TypeDefinition self, string name)
		{
			return GetFieldsRecursive(self).FirstOrDefault(x => x.Name == name);
		}

		/// <summary>
		/// Returns all fields found in the type and its base types
		/// </summary>
		/// <returns>The fields found.</returns>
		/// <param name="self">The type to get the fields from.</param>
		public static IEnumerable<FieldDefinition> GetFieldsRecursive(this TypeDefinition self)
		{
			var x = self;
			while (x != null)
			{
				foreach (var f in x.Fields)
					yield return f;

				if (x.BaseType == null)
					yield break;

				x = x.BaseType.Resolve();
			}
		}

		/// <summary>
		/// Returns all properties found in the type and its base types
		/// </summary>
		/// <returns>The properties found.</returns>
		/// <param name="self">The type to get the fields from.</param>
		public static IEnumerable<PropertyDefinition> GetPropertiesRecursive(this TypeDefinition self)
		{
			var x = self;
			while (x != null)
			{
				foreach (var f in x.Properties)
					yield return f;

                if (x.HasInterfaces)
					foreach (var n in x.Interfaces)
                        foreach (var f in n.InterfaceType.Resolve().Properties)
							yield return f;

				if (x.BaseType == null)
					yield break;
				x = x.BaseType.Resolve();
			}
		}

		/// <summary>
		/// Returns all properties found in the type and its base types
		/// </summary>
		/// <returns>The properties found.</returns>
		/// <param name="self">The type to get the fields from.</param>
		public static IEnumerable<System.Reflection.PropertyInfo> GetPropertiesRecursive(this Type self, System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly | System.Reflection.BindingFlags.Public)
		{
			var x = self;
			while (x != null)
			{
				foreach (var f in x.GetProperties(flags))
					yield return f;

				var interfaces = x.GetInterfaces();

				if (interfaces != null)
					foreach (var n in interfaces)
						foreach (var f in n.GetProperties(flags))
							yield return f;

				x = x.BaseType;
			}
		}

		/// <summary>
		/// Argument input/output types
		/// </summary>
		public enum ArgumentInOut
		{
			/// <summary>
			/// The argument is an exclusive input argument
			/// </summary>
			In,
			/// <summary>
			/// The argument is an exclusive output argument
			/// </summary>
			Out,
			/// <summary>
			/// The argument is both an input and an output argument
			/// </summary>
			InOut
		}

		/// <summary>
		/// Returns a value indicating what directions an argument has
		/// </summary>
		/// <returns>The argument directions.</returns>
		/// <param name="n">The argument to examine.</param>
		public static ArgumentInOut GetArgumentInOut(this ParameterDefinition n)
		{
			var inarg = (n.Attributes & ParameterAttributes.Out) != ParameterAttributes.Out;
			//var outarg = (n.Attributes & ParameterAttributes.Out) == ParameterAttributes.Out && (n.Attributes & ParameterAttributes.In) != ParameterAttributes.In;
			var inoutarg = (n.Attributes & ParameterAttributes.In) == ParameterAttributes.In && (n.Attributes & ParameterAttributes.Out) == ParameterAttributes.Out;
			var inoutoverride = (n.Attributes & ParameterAttributes.Out) == ParameterAttributes.Out || (n.Attributes & ParameterAttributes.In) == ParameterAttributes.In;
			var isarray = n.ParameterType.IsArray;
			//var argrange = n.GetAttribute<RangeAttribute>();
			return inoutarg || (isarray && !inoutoverride) ? ArgumentInOut.InOut : (inarg ? ArgumentInOut.In : ArgumentInOut.Out);
		}

		/// <summary>
		/// Returns a value indicating if the supplied member is a fixed array
		/// </summary>
		/// <returns><c>true</c>, the member is a fixed array, <c>false</c> otherwise.</returns>
		/// <param name="member">The member to examine.</param>
		public static bool IsFixedArrayType(this IMemberDefinition member)
		{
			return IsFixedArrayType(GetMemberType(member));
		}

		/// <summary>
		/// Returns a value indicating if the supplied member is an array
		/// </summary>
		/// <returns><c>true</c>, the member is an array, <c>false</c> otherwise.</returns>
		/// <param name="member">The member to examine.</param>
		public static bool IsArrayType(this IMemberDefinition member)
		{
			return IsArrayType(GetMemberType(member));
		}

		/// <summary>
		/// Returns a value indicating if the supplied type reference is an array
		/// </summary>
		/// <returns><c>true</c>, the type reference is an array, <c>false</c> otherwise.</returns>
		/// <param name="tr">The type reference to examine.</param>
		public static bool IsArrayType(this TypeReference tr)
		{
			return tr.IsArray || tr.IsFixedArrayType();
		}

		/// <summary>
		/// Returns a value indicating if the supplied type is an array
		/// </summary>
		/// <returns><c>true</c>, the type is an array, <c>false</c> otherwise.</returns>
		/// <param name="t">The type to examine.</param>
		public static bool IsArrayType(this Type t)
		{
			return t.IsArray || t.IsFixedArrayType();
		}

		/// <summary>
		/// Returns a value indicating if the supplied type reference is a fixed array
		/// </summary>
		/// <returns><c>true</c>, the type reference is a fixed array, <c>false</c> otherwise.</returns>
		/// <param name="tr">The type reference to examine.</param>
		public static bool IsFixedArrayType(this ITypeSymbol tr)
		{
			var it = tr as INamedTypeSymbol;
			return it.IsGenericType && tr.IsSameTypeReference(typeof(IFixedArray<>));
		}

		/// <summary>
		/// Returns a value indicating if the supplied type is a fixed array
		/// </summary>
		/// <returns><c>true</c>, the type is a fixed array, <c>false</c> otherwise.</returns>
		/// <param name="t">The type to examine.</param>
		public static bool IsFixedArrayType(this Type t)
		{
			return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IFixedArray<>);
		}

		/// <summary>
		/// Gets the type of the supplied property/field or method
		/// </summary>
		/// <returns>The member type.</returns>
		/// <param name="member">The member to examine.</param>
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

		/// <summary>
		/// Gets the type of the array elements.
		/// </summary>
		/// <returns>The array element type.</returns>
		/// <param name="member">The member to examine.</param>
		public static TypeReference GetArrayElementType(this IMemberDefinition member)
		{
			return GetArrayElementType(GetMemberType(member));
		}

		/// <summary>
		/// Gets the type of the array elements.
		/// </summary>
		/// <returns>The array element type.</returns>
		/// <param name="tr">The type reference to examine.</param>
		public static ITypeSymbol GetArrayElementType(this ITypeSymbol tr)
		{
			if (tr is IArrayTypeSymbol)
				return (tr as IArrayTypeSymbol).ElementType;
			else if (tr.IsFixedArrayType())
				return (tr as INamedTypeSymbol).TypeParameters.First();
			else
				throw new Exception($"GetArrayElementType called on non-array: {tr.Name}");
		}

		/// <summary>
		/// Gets the type of the array elements.
		/// </summary>
		/// <returns>The array element type.</returns>
		/// <param name="t">The type to examine.</param>
		public static Type GetArrayElementType(this Type t)
		{
			if (t.IsArray)
				return t.GetElementType();
			else if (t.IsFixedArrayType())
				return t.GetGenericArguments().First();
			else
				throw new Exception($"GetArrayElementType called on non-array: {t.FullName}");
		}

		/// <summary>
		/// Gets the length of the fixed array.
		/// </summary>
		/// <returns>The fixed array length.</returns>
		/// <param name="member">The member to examine.</param>
		public static int GetFixedArrayLength(this MemberDeclarationSyntax member, IEnumerable<SemanticModel> m_semantics)
		{
			var attr = member.LoadSymbol(m_semantics).GetAttribute<FixedArrayLengthAttribute>();
			var arg = attr.ConstructorArguments.First().Value;
			return (int)Convert.ChangeType(arg, typeof(int));
		}

		/// <summary>
		/// Gets the length of the fixed array.
		/// </summary>
		/// <returns>The fixed array length.</returns>
		/// <param name="member">The member to examine.</param>
		public static int GetFixedArrayLength(this System.Reflection.MemberInfo member)
		{
			var attr = member.GetCustomAttributes(typeof(FixedArrayLengthAttribute), false).Cast<FixedArrayLengthAttribute>().First();
			return attr.Length;
		}

        /// <summary>
        /// Gets the length of a fixed-length array
        /// </summary>
        /// <returns>The fixed array length.</returns>
        /// <param name="element">The element to get the length for.</param>
        public static int GetArrayLength(DataElement element)
        {
            if (element is Constant)
                element = ((Constant)element).ArrayLengthSource ?? element;

            if (element.DefaultValue is Array)
                return ((Array)element.DefaultValue).Length;
            else if (element.DefaultValue is EmptyArrayCreateExpression)
                return ResolveIntegerValue(((EmptyArrayCreateExpression)element.DefaultValue).SizeExpression);
            else if (element.DefaultValue is ArrayCreateExpression)
                return ((ArrayCreateExpression)element.DefaultValue).ElementExpressions.Length;
            else if (element.Source is System.Reflection.MemberInfo)
                return GetFixedArrayLength((System.Reflection.MemberInfo)element.Source);
            else if (element.Source is IMemberDefinition)
                return GetFixedArrayLength((IMemberDefinition)element.Source);

            throw new Exception($"Unable to get size of array: {element.Name}");
        }

		/// <summary>
		/// Loads the specified reflection Type and returns the equivalent CeCil TypeDefinition
		/// </summary>
		/// <returns>The loaded type.</returns>
		/// <param name="source">The source that provides the context</param>
		/// <param name="t">The type to load.</param>
		public static TypeReference LoadType(this TypeReference source, Type t)
		{
			return source.Module.ImportReference(t);
		}

		/// <summary>
		/// Returns the target variable or signal, or null
		/// </summary>
		/// <returns>The target variable or signal.</returns>
		/// <param name="self">The item to examine.</param>
		public static DataElement GetTarget(this ASTItem self)
		{
			if (self == null)
				return null;
			if (self is DataElement)
				return (DataElement)self;
			if (self is IdentifierExpression)
				return ((IdentifierExpression)self).Target;
			if (self is MemberReferenceExpression)
				return ((MemberReferenceExpression)self).Target;
            if (self is WrappingExpression)
                return GetTarget(((WrappingExpression)self).Expression);
            if (self is CustomExpression)
                return ((CustomExpression)self).GetTarget();

			return null;
		}

        /// <summary>
        /// Removes parenthesis and type casts to get the underlying item
        /// </summary>
        /// <returns>The unwrapped expression.</returns>
        /// <param name="self">The expression to unwrap.</param>
        public static Expression GetUnwrapped(this Expression self)
        {
            var cur = self;
            while (cur != null)
            {
                if (cur is WrappingExpression)
                    cur = ((WrappingExpression)cur).Expression;
                else if (cur is CustomExpression && ((CustomExpression)cur).Children.Length == 1)
                    cur = ((CustomExpression)cur).Children[0];
                else
                    break;
            }

            return cur ?? self;
        }

		/// <summary>
		/// Sets the target value
		/// </summary>
		/// <returns>The target variable or signal.</returns>
		/// <param name="self">The item to set the element on.</param>
		/// <param name="target">The value to set</param>
		public static void SetTarget(this ASTItem self, DataElement target)
		{
            if (self is IdentifierExpression)
                ((IdentifierExpression)self).Target = target;
            else if (self is MemberReferenceExpression)
                ((MemberReferenceExpression)self).Target = target;
            else if (self is WrappingExpression)
                SetTarget(((WrappingExpression)self).Expression, target);
            else if (self is Expression && self != ((Expression)self).GetUnwrapped())
                SetTarget(((Expression)self).GetUnwrapped(), target);
            else
                throw new Exception($"Unable to set target on item of type {self.GetType().FullName}");
		}

        /// <summary>
        /// Reverse walks the tree to find the next parent of the given type or null
        /// </summary>
        /// <returns>The nearest parent or null.</returns>
        /// <param name="self">The item ot get the parent for.</param>
        /// <typeparam name="T">The data type to look for.</typeparam>
        public static T GetNearestParent<T>(this ASTItem self)
            where T : ASTItem
        {
            return (T)GetNearestParent(self, typeof(T));
        }

        /// <summary>
        /// Reverse walks the tree to find the next parent of the given type or null
        /// </summary>
        /// <returns>The nearest parent or null.</returns>
        /// <param name="self">The item ot get the parent for.</param>
        /// <param name="parentType">The data type to look for.</param>
        public static ASTItem GetNearestParent(this ASTItem self, Type parentType)
        {
            if (self == null)
                return null;
            var p = self.Parent;
            while (p != null && !parentType.IsAssignableFrom(p.GetType()))
                p = p.Parent;

            return p;
        }

        /// <summary>
        /// Extracts an integer value from a data element
        /// </summary>
        /// <returns>The integer value.</returns>
        /// <param name="expression">The expression to extract the value from.</param>
        private static int ResolveIntegerValue(Expression expression)
        {
            expression = expression.GetUnwrapped();
            if (expression is PrimitiveExpression)
                return (int)Convert.ChangeType(((AST.PrimitiveExpression)expression).Value, typeof(int));
            var target = expression.GetTarget();
            if (target.DefaultValue != null)
                return (int)Convert.ChangeType(target.DefaultValue, typeof(int));
            if (target is Constant && ((Constant)target).ArrayLengthSource != null)
                return GetArrayLength(target);

            throw new Exception($"Cannot extract integer value from: {expression.SourceExpression}");
        }

        /// <summary>
        /// Returns the start, end and increment integer values for a statically sized for loop
        /// </summary>
        /// <returns>The static for loop start, end and increment values.</returns>
        /// <param name="self">The statement to extract the values for.</param>
        public static Tuple<int, int, int> GetStaticForLoopValues(this AST.ForStatement self)
        {
            int start, end, incr;

            if (!(self.Initializer is AST.PrimitiveExpression))
                throw new Exception($"Unable to statically expand loop initializer: {self.Initializer.SourceExpression}");
            if (!(self.Condition is AST.BinaryOperatorExpression))
                throw new Exception($"Unable to statically expand loop initializer: {self.Condition.SourceExpression}");
            if (((BinaryOperatorExpression)self.Condition).Operator != ICSharpCode.Decompiler.CSharp.Syntax.BinaryOperatorType.LessThan)
                throw new Exception($"Can only statically expand loops with a less-than operator: {self.Condition.SourceExpression}");

            start = ResolveIntegerValue(self.Initializer);

            var cond_left = ((BinaryOperatorExpression)self.Condition).Left.GetUnwrapped();
            var cond_right = ((BinaryOperatorExpression)self.Condition).Right.GetUnwrapped();
            if (cond_left.GetTarget() != self.LoopIndex)
                throw new Exception($"Can only statically expand loops where the left side of the condition is the loop variable");

            if (cond_right is PrimitiveExpression || cond_right.GetTarget() != null)
                end = ResolveIntegerValue(cond_right);
            else if (cond_right is BinaryOperatorExpression)
            {
                var boe = cond_right as BinaryOperatorExpression;
                if (boe.Operator != ICSharpCode.Decompiler.CSharp.Syntax.BinaryOperatorType.Add && boe.Operator != ICSharpCode.Decompiler.CSharp.Syntax.BinaryOperatorType.Subtract )
                    throw new Exception($"Can only statically expand loops if the condition is a simple add/subtract operation: {boe.SourceExpression}");

                var lefttarget = boe.Left.GetTarget();
                if (lefttarget == null)
                    throw new Exception($"Can only statically expand loops if the condition is a simple add/subtract operation: {boe.SourceExpression}");

                var left_opr = ResolveIntegerValue(boe.Left);
                var right_opr = ResolveIntegerValue(boe.Right);

                if (boe.Operator == ICSharpCode.Decompiler.CSharp.Syntax.BinaryOperatorType.Add)
                    end = left_opr + right_opr;
                else
                    end = left_opr - right_opr;
            }
            else
                throw new Exception($"Can only statically expand loops if the condition is a simple add/subtract operation: {self.Condition.SourceExpression}");

            if (self.Increment is UnaryOperatorExpression)
            {
                var uoe = self.Increment as UnaryOperatorExpression;
                if (uoe.Operand.GetTarget() != self.LoopIndex)
                    throw new Exception($"The item in the loop increment must be the loop variable: {self.Increment.SourceExpression}");
                if (uoe.Operator != ICSharpCode.Decompiler.CSharp.Syntax.UnaryOperatorType.PostIncrement)
                    throw new Exception($"The item in the loop increment must be the loop variable with post increment: {self.Increment.SourceExpression}");
                incr = 1;
            }
            else if (self.Increment is AssignmentExpression)
            {
                var boe = self.Increment as AssignmentExpression;
                if (boe.Left.GetTarget() != self.LoopIndex)
                    throw new Exception($"The item in the loop increment must be the loop variable: {self.Increment.SourceExpression}");
                if (boe.Operator == ICSharpCode.Decompiler.CSharp.Syntax.AssignmentOperatorType.Assign && boe.Right.GetUnwrapped() is BinaryOperatorExpression)
                {
                    var boee = boe.Right.GetUnwrapped() as BinaryOperatorExpression;
                    if (boee.Operator != ICSharpCode.Decompiler.CSharp.Syntax.BinaryOperatorType.Add)
                        throw new Exception($"The item in the loop increment must be a simple addition: {self.Increment.SourceExpression}");
                    if (boee.Left.GetTarget() != self.LoopIndex)
                        throw new Exception($"The item in the loop increment must be the loop variable: {self.Increment.SourceExpression}");

                    incr = ResolveIntegerValue(boee.Right);
                }
                else
                {
                    if (boe.Operator != ICSharpCode.Decompiler.CSharp.Syntax.AssignmentOperatorType.Add)
                        throw new Exception($"The item in the loop increment must be a simple addition: {self.Increment.SourceExpression}");
                    incr = ResolveIntegerValue(boe.Right);
                }
            }
            else
                throw new Exception($"Can only statically expand loops if the increment is a simple constant: {self.Condition.SourceExpression}");

            return new Tuple<int, int, int>(start, end, incr);
        }
	}
}

