using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;

namespace SME.AST
{
	// This partial part deals with the IL side of the parsing
	public partial class ParseProcesses
	{
		/// <summary>
		/// Processes a single method and extracts all the statements in it
		/// </summary>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method to decompile.</param>
		protected virtual Statement[] Decompile(NetworkState network, ProcessState proc, MethodState method)
		{
			var astbuilder = new AstBuilder(proc.DecompilerContext);
			astbuilder.AddMethod(method.SourceMethod);
			astbuilder.RunTransformations();
			var sx = astbuilder.SyntaxTree;

			foreach (var s in sx.Members.Where(x => x is UsingDeclaration).Cast<UsingDeclaration>())
				proc.Imports.Add(s.Import.ToString());

			var methodnode = sx.Members.Where(x => x is MethodDeclaration).FirstOrDefault() as MethodDeclaration;
			if (methodnode == null)
				return null;

			method.ReturnVariable = method.SourceMethod.ReturnType.FullName == "System.Void" ? null : RegisterTemporaryVariable(network, proc, method, method.SourceMethod.ReturnType, method.SourceMethod);
			if (method.ReturnVariable != null)
				method.ReturnVariable.Parent = method;
				      
			var statements = new List<Statement>();

			foreach (var n in methodnode.Body.Children)
				if (n.NodeType == NodeType.Statement)
				{
					try
					{
						statements.Add(Decompile(network, proc, method, n));
					}
					catch (Exception ex)
					{
						Console.WriteLine($"Failed to process statement: {n} -> {ex}");
						statements.Add(new CommentStatement($"Failed to process statement: {n} -> {ex}"));
					}
				}
				else
					throw new Exception(string.Format("Unsupported construct: {0}", n));

			return statements.ToArray();
		}

		/// <summary>
		/// Decompile the specified process, using the specified entry method
		/// </summary>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The entry method to start decompilation from.</param>
		protected virtual void Decompile(NetworkState network, ProcessState proc, System.Reflection.MethodInfo method)
		{
			var statements = new List<Statement>();
			if (proc.CecilType == null)
				proc.CecilType = LoadType(proc.SourceType);

			var proctype = proc.CecilType.Resolve();
			proc.DecompilerContext = new DecompilerContext(proc.CecilType.Module) { CurrentType = proctype };

			var m = proctype.Methods.FirstOrDefault(x => x.Name == method.Name && x.Parameters.Count == method.GetParameters().Length);
			if (m == null)
				throw new Exception($"Unable to find a method with the name {method.Name} in type {proc.CecilType.FullName}");

			proc.MainMethod = Decompile(network, proc, m);

			// If we have comments from the constructors, add them here
			if (statements.Count > 0)
			{
				statements.AddRange(proc.MainMethod.Statements);
				proc.MainMethod.Statements = statements.ToArray();
			}

			var methods = new List<MethodState>();

			while(proc.MethodTargets.Count > 0)
			{
				var tp = proc.MethodTargets.Dequeue();
				var ix = tp.Item3;

				var ic = (ix.SourceExpression as ICSharpCode.NRefactory.CSharp.InvocationExpression);
				var r = ic.Target as ICSharpCode.NRefactory.CSharp.MemberReferenceExpression;

				// TODO: Maybe we can support overloads here as well
				var dm = methods.FirstOrDefault(x => x.Name == r.MemberName);
				if (dm == null)
				{
					var mr = proctype.Methods.FirstOrDefault(x => x.Name == r.MemberName);
					if (mr == null)
						throw new Exception($"Unable to resolve method call to {r}");
					dm = Decompile(network, proc, mr);
					methods.Add(dm);
				}

				proc.Methods = methods.ToArray();
				ix.Target = dm;
				ix.TargetExpression = new MethodReferenceExpression()
				{
					Parent = ix,
					SourceExpression = ix.SourceExpression,
					SourceResultType = dm.ReturnVariable.CecilType,
					Target = dm
				};
				ix.SourceResultType = ix.TargetExpression.SourceResultType;
			}

			methods.Reverse();
			proc.Methods = methods.ToArray();
			proc.SharedSignals = proc.Signals.Values.ToArray();
			proc.SharedVariables = proc.Variables.Values.ToArray();
		}

		/// <summary>
		/// Decompiles the specified method
		/// </summary>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method to decompile.</param>
		protected virtual MethodState Decompile(NetworkState network, ProcessState proc, MethodDefinition method)
		{
			var res = new MethodState()
			{
				Name = method.Name,
				SourceMethod = method,
				Parent = proc,
				Ignore = method.GetAttribute<IgnoreAttribute>() != null
			};

			res.Parameters = method.Parameters.Select(x => ParseParameter(network, proc, res, x)).ToArray();

			if (res.Ignore)
			{
				res.Statements = new Statement[0];
				res.AllVariables = new Variable[0];
                res.Variables = new Variable[0];
			}
			else
			{
				res.Statements = Decompile(network, proc, res);
                res.AllVariables = res.CollectedVariables.ToArray();
                res.Variables = res.Scopes.First().Value.Values.ToArray();
			}

			if (res.ReturnVariable == null)
			{
				res.ReturnVariable = new Variable()
				{
					CecilType = method.ReturnType,
					Parent = res,
					Source = method
				};
			}

			return res;
		}
	}
}
