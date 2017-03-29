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
		/// Decompiles the static initializer for a non-process type
		/// </summary>
		/// <param name="network">The top-level network.</param>
		/// <param name="tr">The type to decompile the initializer for.</param>
		protected void DecompileStaticInitializer(NetworkState network, Mono.Cecil.TypeReference tr)
		{
			var proctype = tr.Resolve();
			var pcs = new ProcessState()
			{
				DecompilerContext = new DecompilerContext(proctype.Module) { CurrentType = proctype },
				CecilType = tr,
				Parent = network
			};

			var static_constructor = proctype.Methods.Where(x => x.IsConstructor && !x.HasParameters && x.IsStatic).FirstOrDefault();

			if (static_constructor != null)
				DecompileConstructor(network, pcs, static_constructor, true);
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

			var static_constructor = proctype.Methods.Where(x => x.IsConstructor && !x.HasParameters && x.IsStatic).FirstOrDefault();

			if (static_constructor != null)
				statements.AddRange(DecompileConstructor(network, proc, static_constructor, true));

			var constructor = proctype.Methods.Where(x => x.IsConstructor && !x.HasParameters && !x.IsStatic).FirstOrDefault();
			if (constructor != null)
				statements.AddRange(DecompileConstructor(network, proc, constructor, false));

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
				res.Variables = new Variable[0];
			}
			else
			{
				res.Statements = Decompile(network, proc, res);
				res.Variables = res.LocalVariables.Values.ToArray();
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


		/// <summary>
		/// Performs decompilation on a constructor method, and finds initializer values
		/// </summary>
		/// <returns>The constructor.</returns>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="condef">The constructor being parsed.</param>
		/// <param name="statics">A value indicating if this is a parse of the static constructor.</param>
		protected virtual Statement[] DecompileConstructor(NetworkState network, ProcessState proc, MethodDefinition condef, bool statics)
		{
			var statements = new List<Statement>();
			var astbuilder = new AstBuilder(proc.DecompilerContext);
			if (condef != null)
			{
				astbuilder.AddMethod(condef);
				astbuilder.RunTransformations();

				var connode = astbuilder.SyntaxTree.Children.Where(x => x.NodeType == NodeType.Member).FirstOrDefault() as ConstructorDeclaration;
				if (connode != null)
				{
					foreach (var s in from n in connode.Body.Statements where n is ICSharpCode.NRefactory.CSharp.ExpressionStatement select (n as ICSharpCode.NRefactory.CSharp.ExpressionStatement).Expression)
					{
						if (s is ICSharpCode.NRefactory.CSharp.AssignmentExpression)
						{
							try
							{
								var ae = s as ICSharpCode.NRefactory.CSharp.AssignmentExpression;
								var lhs = LocateDataElement(network, proc, null, null, ae.Left);

								if (lhs.Source is FieldDefinition && (lhs.Source as FieldDefinition).IsStatic == statics)
								{
									if (lhs.CecilType.IsArray && ae.Right is ICSharpCode.NRefactory.CSharp.ArrayCreateExpression)
									{
										var arc = ae.Right as ICSharpCode.NRefactory.CSharp.ArrayCreateExpression;
										var count = arc.Arguments.FirstOrDefault();
										while (count is ICSharpCode.NRefactory.CSharp.CastExpression)
											count = (count as ICSharpCode.NRefactory.CSharp.CastExpression).Expression;

										if (count == null)
										{
											var items = new List<ICSharpCode.NRefactory.CSharp.PrimitiveExpression>();
											foreach (var el in arc.Initializer.Elements)
												if (el is ICSharpCode.NRefactory.CSharp.PrimitiveExpression)
													items.Add(el as ICSharpCode.NRefactory.CSharp.PrimitiveExpression);
												else
													throw new Exception("Unexpected item in array initializer: " + el);

											var arexp = new ArrayCreateExpression()
											{
												SourceExpression = ae,
												Parent = proc,
												SourceResultType = lhs.CecilType,
												ElementExpressions = items.Select(x => new PrimitiveExpression()
												{
													SourceExpression = x,
													SourceResultType = lhs.CecilType.GetArrayElementType(),
													Value = x.Value
												}).Cast<Expression>().ToArray()
											};

											SetDataElementDefaultValue(network, proc, lhs, arexp, statics);
											continue;
										}
										else if (count is ICSharpCode.NRefactory.CSharp.PrimitiveExpression)
										{
											var ace = new EmptyArrayCreateExpression()
											{
												SourceExpression = count,
												SourceResultType = lhs.CecilType,
												SizeExpression = new PrimitiveExpression()
												{
													Value = (count as ICSharpCode.NRefactory.CSharp.PrimitiveExpression).Value,
													SourceExpression = count,
													SourceResultType = LoadType(typeof(int)),
												}
											};
											ace.SizeExpression.Parent = ace;
											ace.Parent = lhs;

											SetDataElementDefaultValue(network, proc, lhs, ace, statics);
											continue;
										}
										else if (count is ICSharpCode.NRefactory.CSharp.IdentifierExpression)
										{
											var c = LocateDataElement(network, proc, null, null, count);
											var ace = new EmptyArrayCreateExpression()
											{
												SourceExpression = count,
												SourceResultType = lhs.CecilType,
												SizeExpression = new IdentifierExpression()
												{
													Name = c.Name,
													SourceExpression = count,
													SourceResultType = c.CecilType,
													Target = c
												}
											};

											ace.SizeExpression.Parent = ace;
											ace.Parent = lhs;

											SetDataElementDefaultValue(network, proc, lhs, ace, statics);
											continue;
										}
										else if (count is ICSharpCode.NRefactory.CSharp.MemberReferenceExpression)
										{
											var c = LocateDataElement(network, proc, null, null, count);
											var ace = new EmptyArrayCreateExpression()
											{
												SourceExpression = count,
												SourceResultType = lhs.CecilType,
												SizeExpression = new MemberReferenceExpression()
												{
													Name = c.Name,
													SourceExpression = count,
													SourceResultType = c.CecilType,
													Target = c
												}
											};

											ace.SizeExpression.Parent = ace;
											ace.Parent = lhs;

											SetDataElementDefaultValue(network, proc, lhs, ace, statics);
											continue;
										}
									}
									else if (ae.Right is ICSharpCode.NRefactory.CSharp.PrimitiveExpression)
									{
										SetDataElementDefaultValue(network, proc, lhs, (ae.Right as ICSharpCode.NRefactory.CSharp.PrimitiveExpression).Value, statics);
										continue;
									}
								}
							}
							catch
							{
							}

							statements.Add(new CommentStatement($"Failed to parse constructor statement: {s}"));
						}
						else if (s is ICSharpCode.NRefactory.CSharp.InvocationExpression)
						{
							if (s.ToString() != "base..ctor ()")								
								statements.Add(new CommentStatement($"Unparsed to parse constructor statement: {s}"));
						}
						else
						{
							statements.Add(new CommentStatement($"Unparsed to parse constructor statement: {s}"));
						}
					}
				}
			}

			return statements.ToArray();
		}


	}
}
