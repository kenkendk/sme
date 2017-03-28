using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace SME.AST
{
	// This partial part deals with finding the type from expressions
	public partial class ParseProcesses
	{
		/// <summary>
		/// Cache with typenames
		/// </summary>
		protected Dictionary<Type, TypeReference> m_typelookup = new Dictionary<Type, TypeReference>();
		/// <summary>
		/// Cache with assemblies
		/// </summary>
		protected Dictionary<string, AssemblyDefinition> m_assemblies = new Dictionary<string, AssemblyDefinition>();

		/// <summary>
		/// Examines the given expression and returns the resulting output type from the expression
		/// </summary>
		/// <returns>The expression type.</returns>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method where the statement is found.</param>
		/// <param name="statement">The statement where the expression is found.</param>
		/// <param name="expression">The expression to examine.</param>
		protected TypeReference ResolveExpressionType(NetworkState network, ProcessState proc, MethodState method, Statement statement, ICSharpCode.NRefactory.CSharp.Expression expression)
		{
			if (expression is ICSharpCode.NRefactory.CSharp.AssignmentExpression)
				return ResolveExpressionType(network, proc, method, statement, (expression as ICSharpCode.NRefactory.CSharp.AssignmentExpression).Left);
			else if (expression is ICSharpCode.NRefactory.CSharp.IdentifierExpression)
				return LocateDataElement(network, proc, method, statement, expression as ICSharpCode.NRefactory.CSharp.IdentifierExpression).CecilType;
			else if (expression is ICSharpCode.NRefactory.CSharp.MemberReferenceExpression)
			{
				var el = TryLocateElement(network, proc, method, statement, expression as ICSharpCode.NRefactory.CSharp.MemberReferenceExpression);
				if (el == null)
					throw new Exception($"Location failed for expression {expression}");
				else if (el is DataElement)
					return ((DataElement)el).CecilType;
				else if (el is Method)
				{
					var rv = ((Method)el).ReturnVariable;
					if (rv == null)
						return null;

					return rv.CecilType;
				}
				else
					throw new Exception($"Unexpected result for {expression} {el.GetType().FullName}");
			}
			else if (expression is ICSharpCode.NRefactory.CSharp.PrimitiveExpression)
				return LoadType((expression as ICSharpCode.NRefactory.CSharp.PrimitiveExpression).Value.GetType());
			else if (expression is ICSharpCode.NRefactory.CSharp.BinaryOperatorExpression)
			{
				var e = expression as ICSharpCode.NRefactory.CSharp.BinaryOperatorExpression;
				var op = e.Operator;
				if (op.IsCompareOperator() || op.IsLogicalOperator())
					return LoadType(typeof(bool));

				var lefttype = ResolveExpressionType(network, proc, method, statement, e.Left);
				//var righttype = ResolveExpressionType(network, proc, method, statement, e.Right);

				if (op == ICSharpCode.NRefactory.CSharp.BinaryOperatorType.Multiply)
				{
					//TODO: Return a larger type, double the bits?
					return lefttype;
				}
				else
				{
					// TODO: Find the largest type?
					return lefttype;
				}
			}
			else if (expression is ICSharpCode.NRefactory.CSharp.UnaryOperatorExpression)
				return ResolveExpressionType(network, proc, method, statement, (expression as ICSharpCode.NRefactory.CSharp.UnaryOperatorExpression).Expression);
			else if (expression is ICSharpCode.NRefactory.CSharp.IndexerExpression)
			{
				var arraytype = ResolveExpressionType(network, proc, method, statement, (expression as ICSharpCode.NRefactory.CSharp.IndexerExpression).Target);
				return arraytype.GetArrayElementType();
			}
			else if (expression is ICSharpCode.NRefactory.CSharp.CastExpression)
				return LoadType((expression as ICSharpCode.NRefactory.CSharp.CastExpression).Type, method);
			else if (expression is ICSharpCode.NRefactory.CSharp.ConditionalExpression)
				return ResolveExpressionType(network, proc, method, statement, (expression as ICSharpCode.NRefactory.CSharp.ConditionalExpression).TrueExpression);
			else if (expression is ICSharpCode.NRefactory.CSharp.InvocationExpression)
			{
				var si = expression as ICSharpCode.NRefactory.CSharp.InvocationExpression;
				var mt = si.Target as ICSharpCode.NRefactory.CSharp.MemberReferenceExpression;

				// Catch common translations
				if (mt != null && (expression as ICSharpCode.NRefactory.CSharp.InvocationExpression).Arguments.Count == 1)
				{
					if (mt.MemberName == "op_Implicit" || mt.MemberName == "op_Explicit")
					{
						var mtm = Decompile(network, proc, method, statement, mt);
						return ResolveExpressionType(network, proc, method, statement, new ICSharpCode.NRefactory.CSharp.CastExpression(ICSharpCode.NRefactory.CSharp.AstType.Create(mtm.SourceResultType.FullName), si.Arguments.First().Clone()));
					}
					else if (mt.MemberName == "op_Increment")
						return ResolveExpressionType(network, proc, method, statement, new ICSharpCode.NRefactory.CSharp.UnaryOperatorExpression(ICSharpCode.NRefactory.CSharp.UnaryOperatorType.Increment, si.Arguments.First().Clone()));
					else if (mt.MemberName == "op_Decrement")
						return ResolveExpressionType(network, proc, method, statement, new ICSharpCode.NRefactory.CSharp.UnaryOperatorExpression(ICSharpCode.NRefactory.CSharp.UnaryOperatorType.Decrement, si.Arguments.First().Clone()));
				}

				var m = proc.CecilType.Resolve().GetMethods().FirstOrDefault(x => x.Name == mt.MemberName);
				if (m != null)
					return m.ReturnType;

				return ResolveExpressionType(network, proc, method, statement, (expression as ICSharpCode.NRefactory.CSharp.InvocationExpression).Target);
			}
			else if (expression is ICSharpCode.NRefactory.CSharp.ParenthesizedExpression)
				return ResolveExpressionType(network, proc, method, statement, (expression as ICSharpCode.NRefactory.CSharp.ParenthesizedExpression).Expression);
			else if (expression is ICSharpCode.NRefactory.CSharp.NullReferenceExpression)
				return null;
			else if (expression is ICSharpCode.NRefactory.CSharp.ArrayCreateExpression)
				return ResolveExpressionType(network, proc, method, statement, (expression as ICSharpCode.NRefactory.CSharp.ArrayCreateExpression).Initializer.FirstChild as ICSharpCode.NRefactory.CSharp.Expression);
			else if (expression is ICSharpCode.NRefactory.CSharp.CheckedExpression)
				return ResolveExpressionType(network, proc, method, statement, (expression as ICSharpCode.NRefactory.CSharp.CheckedExpression).Expression);
			else if (expression is ICSharpCode.NRefactory.CSharp.UncheckedExpression)
				return ResolveExpressionType(network, proc, method, statement, (expression as ICSharpCode.NRefactory.CSharp.UncheckedExpression).Expression);
			else if (expression == ICSharpCode.NRefactory.CSharp.Expression.Null)
				return null;
			else
				throw new Exception(string.Format("Unsupported expression: {0} ({1})", expression, expression.GetType().FullName));
		}

		/// <summary>
		/// Loads the specified reflection Type and returns the equivalent CeCil TypeDefinition
		/// </summary>
		/// <returns>The loaded type.</returns>
		/// <param name="t">The type to load.</param>
		protected virtual TypeReference LoadType(Type t)
		{
			if (m_typelookup.ContainsKey(t))
				return m_typelookup[t];

			AssemblyDefinition asm;
			m_assemblies.TryGetValue(t.Assembly.Location, out asm);
			if (asm == null)
				asm = m_assemblies[t.Assembly.Location] = AssemblyDefinition.ReadAssembly(t.Assembly.Location);
			
			if (asm == null)
				return null;

			var res = LoadTypeByName(t.FullName, asm.Modules);

			if (res == null && t.IsGenericType)
			{
				var gt = t.GetGenericTypeDefinition();

				var gtd = LoadTypeByName(gt.FullName, asm.Modules);
				if (gtd != null)
				{
					var gtr = new GenericInstanceType(gtd);
					foreach(var ga in t.GetGenericArguments().Select(x => LoadType(x)))
						gtr.GenericArguments.Add(ga);

					res = gtr;
				}
			}

			if (res == null && t.IsArray)
			{
 				var el = t.GetElementType();
				res = new ArrayType(LoadType(el));
			}

			if (res == null)
				throw new Exception($"Failed to load {t.FullName}, the following types were found in the assembly: {string.Join(",", asm.Modules.SelectMany(x => x.GetTypes()).Select(x => x.FullName))}");

			return m_typelookup[t] = res;
		}

		/// <summary>
		/// Loads the specified reflection Type and returns the equivalent CeCil TypeDefinition
		/// </summary>
		/// <returns>The loaded type.</returns>
		/// <param name="name">The full name of the type to load.</param>
		/// <param name="modules">The modules to look in</param>
		protected virtual TypeReference LoadTypeByName(string name, params ModuleDefinition[] modules)
		{
			return LoadTypeByName(name, modules.AsEnumerable());
		}

		/// <summary>
		/// Loads the specified reflection Type and returns the equivalent CeCil TypeDefinition
		/// </summary>
		/// <returns>The loaded type.</returns>
		/// <param name="name">The full name of the type to load.</param>
		/// <param name="modules">The modules to look in</param>
		protected virtual TypeReference LoadTypeByName(string name, IEnumerable<ModuleDefinition> modules)
		{
			var parts = name.Split('.', '+', '/').Reverse().ToArray();
			foreach (var e in modules.SelectMany(m => m.GetTypes()))
			{
				var c = e;
                var lastns = string.Empty;
				var failed = false;
				for (var i = 0; i < parts.Length - 1; i++)
				{
                    if (c != null && c.Name == parts[i])
                    {
                        lastns = c.Namespace; 
                        c = c.DeclaringType;
                    }
                    else
                    {
						if (c == null && lastns == string.Join(".", parts.Skip(i).Reverse()))
							return e;
						
                        failed = true;
                        break;
                    }
				}

				if (!failed && c == null && lastns == parts[parts.Length - 1])
					return e;

			}

			return null;
		}

		/// <summary>
		/// Loads the specified AstType and returns the equivalent CeCil TypeDefinition
		/// </summary>
		/// <returns>The loaded type.</returns>
		/// <param name="t">The type to load.</param>
		protected virtual TypeReference LoadType(ICSharpCode.NRefactory.CSharp.AstType t, Method sourcemethod = null)
		{
			if (t is ICSharpCode.NRefactory.CSharp.PrimitiveType)
				switch (((ICSharpCode.NRefactory.CSharp.PrimitiveType)t).KnownTypeCode)
				{
					case ICSharpCode.NRefactory.TypeSystem.KnownTypeCode.Boolean:
						return LoadType(typeof(bool));	
					case ICSharpCode.NRefactory.TypeSystem.KnownTypeCode.Byte:
						return LoadType(typeof(byte));
					case ICSharpCode.NRefactory.TypeSystem.KnownTypeCode.SByte:
						return LoadType(typeof(sbyte));
					case ICSharpCode.NRefactory.TypeSystem.KnownTypeCode.Int16:
						return LoadType(typeof(short));
					case ICSharpCode.NRefactory.TypeSystem.KnownTypeCode.UInt16:
						return LoadType(typeof(ushort));
					case ICSharpCode.NRefactory.TypeSystem.KnownTypeCode.Int32:
						return LoadType(typeof(int));
					case ICSharpCode.NRefactory.TypeSystem.KnownTypeCode.UInt32:
						return LoadType(typeof(uint));
					case ICSharpCode.NRefactory.TypeSystem.KnownTypeCode.Int64:
						return LoadType(typeof(long));
					case ICSharpCode.NRefactory.TypeSystem.KnownTypeCode.UInt64:
						return LoadType(typeof(ulong));
					case ICSharpCode.NRefactory.TypeSystem.KnownTypeCode.Single:
						return LoadType(typeof(float));
					case ICSharpCode.NRefactory.TypeSystem.KnownTypeCode.Double:
						return LoadType(typeof(double));
				}

			if (t is ICSharpCode.NRefactory.CSharp.SimpleType)
			{
				var st = t as ICSharpCode.NRefactory.CSharp.SimpleType;
				var typename = st.Identifier;

				var t0 = typeof(int).Assembly.GetType("System." + typename);
				if (t0 != null)
					return LoadType(t0);
				if (sourcemethod != null)
				{
					var t1 = LoadTypeByName(typename, sourcemethod.SourceMethod.Module);
					if (t1 != null)
						return t1;
					t1 = LoadTypeByName(sourcemethod.SourceMethod.DeclaringType.FullName + "." + typename, sourcemethod.SourceMethod.Module);
					if (t1 != null)
						return t1;
					t1 = LoadTypeByName(sourcemethod.SourceMethod.DeclaringType.Namespace + "." + typename, sourcemethod.SourceMethod.Module);
					if (t1 != null)
						return t1;
				}
			}

			if (t is ICSharpCode.NRefactory.CSharp.MemberType)
			{
				var mt = t as ICSharpCode.NRefactory.CSharp.MemberType;
				var t0 = LoadTypeByName(mt.ToString(), sourcemethod.SourceMethod.Module);

				if (t0 != null)
					return t0;

				t0 = LoadTypeByName(sourcemethod.SourceMethod.DeclaringType.Namespace + "." + mt.ToString(), sourcemethod.SourceMethod.Module);
				if (t0 != null)
					return t0;
			}

			throw new Exception($"Failed to load {t.ToString()}");
		}


	}
}
