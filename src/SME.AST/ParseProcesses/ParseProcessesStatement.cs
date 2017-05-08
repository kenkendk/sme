using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;

namespace SME.AST
{
	// This partial part deals with statements
	public partial class ParseProcesses
	{
		/// <summary>
		/// Processes a single statement from the decompiler and returns an AST entry for it
		/// </summary>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method where the statement is found.</param>
		/// <param name="statement">The decompiler statement to process.</param>
		protected virtual Statement Decompile(NetworkState network, ProcessState proc, MethodState method, AstNode statement)
		{
			if (statement is ICSharpCode.NRefactory.CSharp.ExpressionStatement)
				return Decompile(network, proc, method, statement as ICSharpCode.NRefactory.CSharp.ExpressionStatement);
			else if (statement is ICSharpCode.NRefactory.CSharp.IfElseStatement)
				return Decompile(network, proc, method, statement as ICSharpCode.NRefactory.CSharp.IfElseStatement);
			else if (statement is ICSharpCode.NRefactory.CSharp.BlockStatement)
				return Decompile(network, proc, method, statement as ICSharpCode.NRefactory.CSharp.BlockStatement);
			else if (statement is ICSharpCode.NRefactory.CSharp.VariableDeclarationStatement)
				return Decompile(network, proc, method, statement as ICSharpCode.NRefactory.CSharp.VariableDeclarationStatement);
			else if (statement is ICSharpCode.NRefactory.CSharp.SwitchStatement)
				return Decompile(network, proc, method, statement as ICSharpCode.NRefactory.CSharp.SwitchStatement);
			else if (statement is ICSharpCode.NRefactory.CSharp.ReturnStatement)
				return Decompile(network, proc, method, statement as ICSharpCode.NRefactory.CSharp.ReturnStatement);
			else if (statement is ICSharpCode.NRefactory.CSharp.ForStatement)
				return Decompile(network, proc, method, statement as ICSharpCode.NRefactory.CSharp.ForStatement);
			else if (statement is ICSharpCode.NRefactory.CSharp.BreakStatement)
				return Decompile(network, proc, method, statement as ICSharpCode.NRefactory.CSharp.BreakStatement);
			else if (statement is ICSharpCode.NRefactory.CSharp.CheckedStatement)
			{
				Console.WriteLine("Warning: \"checked\" is not supported and will be ignored for statement: {0}", statement);
				return Decompile(network, proc, method, (statement as ICSharpCode.NRefactory.CSharp.CheckedStatement).Body);
			}
			else if (statement is ICSharpCode.NRefactory.CSharp.UncheckedStatement)
				return Decompile(network, proc, method, (statement as ICSharpCode.NRefactory.CSharp.UncheckedStatement).Body);
			else if (statement.IsNull)
				return new EmptyStatement() { Parent = method };
			else
				throw new Exception(string.Format("Unsupported statement: {0} ({1})", statement, statement.GetType().FullName));
		}

		/// <summary>
		/// Processes a single statement from the decompiler and returns an AST entry for it
		/// </summary>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method where the statement is found.</param>
		/// <param name="statement">The decompiler statement to process.</param>
		protected virtual ExpressionStatement Decompile(NetworkState network, ProcessState proc, MethodState method, ICSharpCode.NRefactory.CSharp.ExpressionStatement statement)
		{
			if (statement.GetType() != typeof(ICSharpCode.NRefactory.CSharp.ExpressionStatement))
				throw new Exception(string.Format("Unsupported expression statement: {0} ({1})", statement, statement.GetType().FullName));

			var s = new ExpressionStatement()
			{
				SourceStatement = statement,
				Parent = method
			};

			s.Expression = Decompile(network, proc, method, s, statement.Expression);
			return s;
		}

		/// <summary>
		/// Processes a single statement from the decompiler and returns an AST entry for it
		/// </summary>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method where the statement is found.</param>
		/// <param name="statement">The decompiler statement to process.</param>
		protected virtual IfElseStatement Decompile(NetworkState network, ProcessState proc, MethodState method, ICSharpCode.NRefactory.CSharp.IfElseStatement statement)
		{
			var s = new IfElseStatement()
			{
				SourceStatement = statement,
				TrueStatement = Decompile(network, proc, method, statement.TrueStatement),
				FalseStatement = Decompile(network, proc, method, statement.FalseStatement),
				Parent = method
			};

			s.Condition = Decompile(network, proc, method, s, statement.Condition);
			s.TrueStatement.Parent = s;
			s.FalseStatement.Parent = s;

			return s;
		}

		/// <summary>
		/// Processes a single statement from the decompiler and returns an AST entry for it
		/// </summary>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method where the statement is found.</param>
		/// <param name="statement">The decompiler statement to process.</param>
		protected virtual BlockStatement Decompile(NetworkState network, ProcessState proc, MethodState method, ICSharpCode.NRefactory.CSharp.BlockStatement statement)
		{
			var s = new BlockStatement
			{
				SourceStatement = statement,
				Statements = statement.Statements.Select(x => Decompile(network, proc, method, x)).ToArray(),
				Parent = method
			};

			foreach (var x in s.Statements)
				x.Parent = s;

			return s;
		}

		/// <summary>
		/// Processes a single variable declaration statement from the decompiler and returns an AST entry for it
		/// </summary>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method where the statement is found.</param>
		/// <param name="statement">The decompiler statement to process.</param>
		protected virtual Statement Decompile(NetworkState network, ProcessState proc, MethodState method, ICSharpCode.NRefactory.CSharp.VariableDeclarationStatement statement)
		{
			TypeReference vartype = null;

			var init = statement.Variables.FirstOrDefault(x => x.Initializer is ICSharpCode.NRefactory.CSharp.MemberReferenceExpression);
			if (init != null)
			{
				var mt = TryLocateElement(network, proc, method, null, init.Initializer);
				if (mt != null && mt is AST.Bus)
					vartype = LoadType(((AST.Bus)mt).SourceType);
			}

			if (vartype == null)
				vartype = LoadType(statement.Type, method);
			
			if (vartype.IsBusType())
			{
				foreach (var n in statement.Variables)
				{
					if (n.Initializer is ICSharpCode.NRefactory.CSharp.MemberReferenceExpression)
					{
						proc.BusInstances[n.Name] = LocateBus(network, proc, method, n.Initializer);
					}
					else
					{
						var match = proc.CecilType.Resolve().Fields.Where(x => x.FieldType.IsSameTypeReference(vartype)).FirstOrDefault();
						if (match != null)
							proc.BusInstances[n.Name] = LocateBus(network, proc, method, n.Initializer);
						else
							Console.WriteLine("Unable to determine what bus is assigned to variable {0}", n.Name);
					}
				}

				return new EmptyStatement()
				{
					SourceStatement = statement,
					Parent = method
				};
			}
			else
			{
				var statements = new List<Statement>();

				foreach (var n in statement.Variables)
				{
					RegisterVariable(network, proc, method, vartype, n);
					if (!n.Initializer.IsNull)
						statements.Add(Decompile(network, proc, method, new ICSharpCode.NRefactory.CSharp.ExpressionStatement(new ICSharpCode.NRefactory.CSharp.AssignmentExpression(new ICSharpCode.NRefactory.CSharp.IdentifierExpression(n.Name), n.Initializer.Clone()))));
				}

				if (statements.Count == 0)
				{
					return new EmptyStatement()
					{
						SourceStatement = statement,
						Parent = method
					};
				}
				else if (statements.Count == 1)
				{
					statements[0].Parent = method;
					return statements[0];
				}
				else
				{
					var s = new BlockStatement()
					{
						SourceStatement = statement,
						Statements = statements.ToArray(),
						Parent = method						                       
					};

					foreach (var x in s.Statements)
						x.Parent = s;

					return s;
				}
			}
		}

		/// <summary>
		/// Processes a single statement from the decompiler and returns an AST entry for it
		/// </summary>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method where the statement is found.</param>
		/// <param name="statement">The decompiler statement to process.</param>
		protected virtual SwitchStatement Decompile(NetworkState network, ProcessState proc, MethodState method, ICSharpCode.NRefactory.CSharp.SwitchStatement statement)
		{
			var s = new SwitchStatement()
			{
				SourceStatement = statement,
				Parent = method
			};

			s.SwitchExpression = Decompile(network, proc, method, s, statement.Expression);

			s.Cases = statement
				.SwitchSections
				.Select(x => new Tuple<Expression[], Statement[]>(
					x.CaseLabels.Select(y => Decompile(network, proc, method, s, y.Expression)).ToArray(),
					x.Statements.Select(y => Decompile(network, proc, method, y)).ToArray()
				)).ToArray();

			foreach (var c in s.Cases)
			{
				foreach (var x in c.Item1)
					x.Parent = s;
				foreach (var x in c.Item2)
					x.Parent = s;
			}

			return s;
		}

		/// <summary>
		/// Processes a single statement from the decompiler and returns an AST entry for it
		/// </summary>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method where the statement is found.</param>
		/// <param name="statement">The decompiler statement to process.</param>
		protected virtual Statement Decompile(NetworkState network, ProcessState proc, MethodState method, ICSharpCode.NRefactory.CSharp.ReturnStatement statement)
		{
			var s = new ReturnStatement()
			{
				SourceStatement = statement,
				Parent = method
			};

			s.ReturnExpression = Decompile(network, proc, method, s, statement.Expression);

			return s;
		}

		/// <summary>
		/// Finds the length of an array or a primitive value for use in loop bounds
		/// </summary>
		/// <returns>The array length or primitive.</returns>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method where the statement is found.</param>
		/// <param name="src">The expression to examine.</param>
		protected virtual Constant ResolveArrayLengthOrPrimitive(NetworkState network, ProcessState proc, MethodState method, ICSharpCode.NRefactory.CSharp.Expression src)
		{
			if (src is ICSharpCode.NRefactory.CSharp.PrimitiveExpression)
				try
				{
					return new Constant
					{
						Source = src,
						DefaultValue = Convert.ToInt32((src as ICSharpCode.NRefactory.CSharp.PrimitiveExpression).Value),
						CecilType = LoadType(typeof(int)),
						Parent = method
					};
				}
				catch (Exception ex)
				{
					throw new Exception(string.Format("Unable to resolve as a constant value: {0}", src), ex);
				}

			var ex_left = src as ICSharpCode.NRefactory.CSharp.MemberReferenceExpression;
			if (ex_left.MemberName != "Length")
				throw new Exception(string.Format("Only plain style for loops supported: {0}", src));

			var member = LocateDataElement(network, proc, method, null, ex_left.Target);
			if (member.CecilType.IsFixedArrayType())
			{
				if (member.Source is IMemberDefinition)
				{
					return new Constant
					{
						Source = member,
						DefaultValue = ((IMemberDefinition)member.Source).GetFixedArrayLength(),
						CecilType = LoadType(typeof(int))
					};
				}
				else if (member.Source is System.Reflection.MemberInfo)
				{
					return new Constant
					{
						Source = member,
						DefaultValue = ((System.Reflection.MemberInfo)member.Source).GetFixedArrayLength(),
						CecilType = LoadType(typeof(int))
					};
				}
			}

			var value = member.DefaultValue;

			if (value is AST.ArrayCreateExpression)
			{
				var ce = (value as ArrayCreateExpression);
				var target = ce.ElementExpressions.Length;
				return new Constant()
				{
					DefaultValue = target,
					Source = ce,
					CecilType = LoadType(typeof(int))
				};
				
			}
			else if (value is AST.EmptyArrayCreateExpression)
			{
				var ce = (value as EmptyArrayCreateExpression);
				var target = ce.SizeExpression.GetTarget();
				if (target == null)
				{
					return new Constant()
					{
						DefaultValue = ((PrimitiveExpression)ce.SizeExpression).Value,
						Source = ce,
						CecilType = LoadType(typeof(int))
					};
				}
				else
				{
					return new Constant()
					{
						DefaultValue = target.DefaultValue,
						Source = target.Source,
						CecilType = LoadType(typeof(int))
					};
				}
			}
					

			if (value is ICSharpCode.NRefactory.CSharp.ArrayCreateExpression)
				return new Constant() 
				{ 
					Source = value, 
					DefaultValue = (value as ICSharpCode.NRefactory.CSharp.ArrayCreateExpression).Initializer.Children.Count(),
					CecilType = LoadType(typeof(int)),
					Parent = method
				};

			if (value is IMemberDefinition)
			{
				try
				{
					var mr = value as IMemberDefinition;
					if (mr is FieldDefinition && network.ConstantLookup.ContainsKey(mr as FieldDefinition))
						return ResolveArrayLengthOrPrimitive(network, proc, method, new ICSharpCode.NRefactory.CSharp.PrimitiveExpression(network.ConstantLookup[mr as FieldDefinition]));
				}
				catch (Exception ex)
				{
					throw new Exception(string.Format("Unable to resolve as a constant value: {0}", src), ex);
				}
			}

			try
			{
				return new Constant() { 
					Source = value, 
					DefaultValue = Convert.ToInt32(value),
					CecilType = LoadType(typeof(int)),
					Parent = method
				};
			}
			catch (Exception ex)
			{
				throw new Exception(string.Format("Unable to resolve as a constant value: {0}", src), ex);
			}
		}
		/// <summary>
		/// Processes a single statement from the decompiler and returns an AST entry for it
		/// </summary>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method where the statement is found.</param>
		/// <param name="statement">The decompiler statement to process.</param>
		protected virtual BreakStatement Decompile(NetworkState network, ProcessState proc, MethodState method, ICSharpCode.NRefactory.CSharp.BreakStatement statement)
		{
			return new BreakStatement()
			{
				Parent = method,
				SourceStatement =statement
			};
		}

		/// <summary>
		/// Processes a single statement from the decompiler and returns an AST entry for it
		/// </summary>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method where the statement is found.</param>
		/// <param name="statement">The decompiler statement to process.</param>
		protected virtual ForStatement Decompile(NetworkState network, ProcessState proc, MethodState method, ICSharpCode.NRefactory.CSharp.ForStatement statement)
		{
			if (statement.Initializers.Count != 1)
				throw new Exception(string.Format("Only plain style for loops supported: {0}", statement));

			if (statement.Iterators.Count != 1)
				throw new Exception(string.Format("Only plain style for loops supported: {0}", statement));


			var init = statement.Initializers.First() as ICSharpCode.NRefactory.CSharp.VariableDeclarationStatement;
			if (init == null)
				throw new Exception(string.Format("Only plain style for loops supported: {0}", statement));

			if (init.Variables.Count != 1)
				throw new Exception(string.Format("Only plain style for loops supported: {0}", statement));

			var name = init.Variables.First().Name;
			var initial = init.Variables.First().Initializer as ICSharpCode.NRefactory.CSharp.PrimitiveExpression;

			if (initial == null || !(initial.Value is int))
				throw new Exception(string.Format("Only plain style for loops supported: {0}", statement));

			var startvalue = new Constant() 
			{ 
				Source = initial, 
				DefaultValue = (int)initial.Value,
				CecilType = LoadType(typeof(int))
			};

			var cond = statement.Condition as ICSharpCode.NRefactory.CSharp.BinaryOperatorExpression;
			if (cond == null)
				throw new Exception(string.Format("Only plain style for loops supported: {0}", statement));

			if (cond.Operator != BinaryOperatorType.LessThan)
				throw new Exception(string.Format("Only plain style for loops supported: {0}", statement));

			var condleft = cond.Left as ICSharpCode.NRefactory.CSharp.IdentifierExpression;
			var condright = cond.Right as ICSharpCode.NRefactory.CSharp.PrimitiveExpression;

			// Handling cases where the upper limit is the length of an array
			if (condright == null)
			{
				// Some plus/minus expression
				if (cond.Right is ICSharpCode.NRefactory.CSharp.BinaryOperatorExpression)
				{
					var binop = cond.Right as ICSharpCode.NRefactory.CSharp.BinaryOperatorExpression;

					var leftval = ResolveArrayLengthOrPrimitive(network, proc, method, binop.Left);
					var righval = ResolveArrayLengthOrPrimitive(network, proc, method, binop.Right);

					if (binop.Operator == BinaryOperatorType.Add)
						condright = new ICSharpCode.NRefactory.CSharp.PrimitiveExpression((int)leftval.DefaultValue + (int)righval.DefaultValue);
					else if (binop.Operator == BinaryOperatorType.Subtract)
						condright = new ICSharpCode.NRefactory.CSharp.PrimitiveExpression((int)leftval.DefaultValue - (int)righval.DefaultValue);
					else
						throw new Exception(string.Format("Only add and subtract operations are supported in for loop bounds: {0}", statement));
				}
				// Plain limit
				else if (cond.Right is ICSharpCode.NRefactory.CSharp.IdentifierExpression || cond.Right is ICSharpCode.NRefactory.CSharp.MemberReferenceExpression)
				{
					condright = new ICSharpCode.NRefactory.CSharp.PrimitiveExpression(ResolveArrayLengthOrPrimitive(network, proc, method, cond.Right).DefaultValue);
				}
			}

			if (condleft == null || condright == null || !(condright.Value is int) || condleft.Identifier != name)
				throw new Exception(string.Format("Only plain style for loops supported: {0}", statement));

			var endvalue = new Constant() 
			{ 
				Source = condright,
				DefaultValue = (int)condright.Value,
				CecilType = LoadType(typeof(int))
			};

			var increment = new Constant() 
			{ 
				DefaultValue = 1,
				CecilType = LoadType(typeof(int)),
				Source = cond
			};

			var itr = statement.Iterators.First() as ICSharpCode.NRefactory.CSharp.ExpressionStatement;
			if (itr == null || !(itr.Expression is ICSharpCode.NRefactory.CSharp.UnaryOperatorExpression))
			{
				var ae = itr == null ? null : itr.Expression as ICSharpCode.NRefactory.CSharp.AssignmentExpression;

				if (ae != null && ae.Left is ICSharpCode.NRefactory.CSharp.IdentifierExpression && (ae.Left as ICSharpCode.NRefactory.CSharp.IdentifierExpression).Identifier == name && ae.Right is ICSharpCode.NRefactory.CSharp.PrimitiveExpression)
				{
					// Support for increments like "i += 2"
					increment = new Constant() 
					{ 
						Source = ae.Right, 
						DefaultValue = ResolveArrayLengthOrPrimitive(network, proc, method, ae.Right as ICSharpCode.NRefactory.CSharp.PrimitiveExpression),
						CecilType = LoadType(typeof(int))
					};

					itr = new ICSharpCode.NRefactory.CSharp.ExpressionStatement(
						new ICSharpCode.NRefactory.CSharp.UnaryOperatorExpression(
							UnaryOperatorType.PostIncrement,
							ae.Left.Clone()
						)
					);

					endvalue.DefaultValue = ((int)endvalue.DefaultValue) / (int)Convert.ChangeType((ae.Right as ICSharpCode.NRefactory.CSharp.PrimitiveExpression).Value, typeof(int));
				}
				else
					throw new Exception(string.Format("Only plain style for loops supported: {0}", statement));
			}

			var itre = itr.Expression as ICSharpCode.NRefactory.CSharp.UnaryOperatorExpression;
			var itro = itre.Expression as ICSharpCode.NRefactory.CSharp.IdentifierExpression;

			if (itro == null || itre.Operator != UnaryOperatorType.PostIncrement || itro.Identifier != name)
				throw new Exception(string.Format("Only plain style for loops supported: {0}", statement));


			var loopvar = new Variable()
			{
				CecilType = LoadType(typeof(int)),
				Name = name,
				Source = statement.Clone(),
			};
			Variable prevar;
			method.LocalVariables.TryGetValue(name, out prevar);
			method.LocalVariables[name] = loopvar;

			loopvar.DefaultValue = startvalue.DefaultValue;

			var res = new ForStatement()
			{
				SourceStatement = statement,
				StartValue = startvalue,
				EndValue = endvalue,
				Increment = increment,
				LoopIndex = loopvar,
				LoopBody = Decompile(network, proc, method, statement.EmbeddedStatement),
				Parent = method
			};

			loopvar.Parent = res;

			res.LoopBody.Parent = res;
			method.LocalVariables.Remove(name);

			if (prevar != null)
				method.LocalVariables[name] = prevar;

			return res;
		}
	}
}
