using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp.Syntax;
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
            if (statement is ICSharpCode.Decompiler.CSharp.Syntax.ExpressionStatement)
                return Decompile(network, proc, method, statement as ICSharpCode.Decompiler.CSharp.Syntax.ExpressionStatement);
            else if (statement is ICSharpCode.Decompiler.CSharp.Syntax.IfElseStatement)
                return Decompile(network, proc, method, statement as ICSharpCode.Decompiler.CSharp.Syntax.IfElseStatement);
            else if (statement is ICSharpCode.Decompiler.CSharp.Syntax.BlockStatement)
                return Decompile(network, proc, method, statement as ICSharpCode.Decompiler.CSharp.Syntax.BlockStatement);
            else if (statement is ICSharpCode.Decompiler.CSharp.Syntax.VariableDeclarationStatement)
                return Decompile(network, proc, method, statement as ICSharpCode.Decompiler.CSharp.Syntax.VariableDeclarationStatement);
            else if (statement is ICSharpCode.Decompiler.CSharp.Syntax.SwitchStatement)
                return Decompile(network, proc, method, statement as ICSharpCode.Decompiler.CSharp.Syntax.SwitchStatement);
            else if (statement is ICSharpCode.Decompiler.CSharp.Syntax.ReturnStatement)
                return Decompile(network, proc, method, statement as ICSharpCode.Decompiler.CSharp.Syntax.ReturnStatement);
            else if (statement is ICSharpCode.Decompiler.CSharp.Syntax.ForStatement)
                return Decompile(network, proc, method, statement as ICSharpCode.Decompiler.CSharp.Syntax.ForStatement);
            else if (statement is ICSharpCode.Decompiler.CSharp.Syntax.BreakStatement)
                return Decompile(network, proc, method, statement as ICSharpCode.Decompiler.CSharp.Syntax.BreakStatement);
            else if (statement is ICSharpCode.Decompiler.CSharp.Syntax.CheckedStatement)
            {
                Console.WriteLine("Warning: \"checked\" is not supported and will be ignored for statement: {0}", statement);
                return Decompile(network, proc, method, (statement as ICSharpCode.Decompiler.CSharp.Syntax.CheckedStatement).Body);
            }
            else if (statement is ICSharpCode.Decompiler.CSharp.Syntax.UncheckedStatement)
                return Decompile(network, proc, method, (statement as ICSharpCode.Decompiler.CSharp.Syntax.UncheckedStatement).Body);
            else if (statement.IsNull)
                return new EmptyStatement() { Parent = method };
            else if (statement is ICSharpCode.Decompiler.CSharp.Syntax.GotoStatement)
                return Decompile(network, proc, method, statement as ICSharpCode.Decompiler.CSharp.Syntax.GotoStatement);
            else if (statement is ICSharpCode.Decompiler.CSharp.Syntax.LabelStatement)
                return Decompile(network, proc, method, statement as ICSharpCode.Decompiler.CSharp.Syntax.LabelStatement);
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
		protected virtual ExpressionStatement Decompile(NetworkState network, ProcessState proc, MethodState method, ICSharpCode.Decompiler.CSharp.Syntax.ExpressionStatement statement)
		{
			if (statement.GetType() != typeof(ICSharpCode.Decompiler.CSharp.Syntax.ExpressionStatement))
				throw new Exception(string.Format("Unsupported expression statement: {0} ({1})", statement, statement.GetType().FullName));

			var s = new ExpressionStatement()
			{
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
		protected virtual IfElseStatement Decompile(NetworkState network, ProcessState proc, MethodState method, ICSharpCode.Decompiler.CSharp.Syntax.IfElseStatement statement)
		{
			var s = new IfElseStatement()
			{
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
		protected virtual BlockStatement Decompile(NetworkState network, ProcessState proc, MethodState method, ICSharpCode.Decompiler.CSharp.Syntax.BlockStatement statement)
		{
            
			var s = new BlockStatement
			{
				Parent = method
			};

            method.StartScope(s);

            s.Statements = statement.Statements.Select(x =>
            {
                var n = Decompile(network, proc, method, x);
                n.Parent = s;
                return n;
            }).ToArray();

			method.FinishScope(s);


			return s;
		}

		/// <summary>
		/// Processes a single variable declaration statement from the decompiler and returns an AST entry for it
		/// </summary>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method where the statement is found.</param>
		/// <param name="statement">The decompiler statement to process.</param>
		protected virtual Statement Decompile(NetworkState network, ProcessState proc, MethodState method, ICSharpCode.Decompiler.CSharp.Syntax.VariableDeclarationStatement statement)
		{
			TypeReference vartype = null;

			var init = statement.Variables.FirstOrDefault(x => x.Initializer is ICSharpCode.Decompiler.CSharp.Syntax.MemberReferenceExpression);
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
					if (n.Initializer is ICSharpCode.Decompiler.CSharp.Syntax.MemberReferenceExpression)
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
						statements.Add(Decompile(network, proc, method, new ICSharpCode.Decompiler.CSharp.Syntax.ExpressionStatement(new ICSharpCode.Decompiler.CSharp.Syntax.AssignmentExpression(new ICSharpCode.Decompiler.CSharp.Syntax.IdentifierExpression(n.Name), n.Initializer.Clone()))));
				}

				if (statements.Count == 0)
				{
					return new EmptyStatement()
					{
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
		protected virtual SwitchStatement Decompile(NetworkState network, ProcessState proc, MethodState method, ICSharpCode.Decompiler.CSharp.Syntax.SwitchStatement statement)
		{
			var s = new SwitchStatement()
			{
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
		protected virtual Statement Decompile(NetworkState network, ProcessState proc, MethodState method, ICSharpCode.Decompiler.CSharp.Syntax.ReturnStatement statement)
		{
			var s = new ReturnStatement()
			{
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
		protected virtual Constant ResolveArrayLengthOrPrimitive(NetworkState network, ProcessState proc, MethodState method, ICSharpCode.Decompiler.CSharp.Syntax.Expression src)
		{
			if (src is ICSharpCode.Decompiler.CSharp.Syntax.PrimitiveExpression)
				try
				{
					return new Constant
					{
						Source = src,
						DefaultValue = Convert.ToInt32((src as ICSharpCode.Decompiler.CSharp.Syntax.PrimitiveExpression).Value),
						CecilType = LoadType(typeof(int)),
						Parent = method
					};
				}
				catch (Exception ex)
				{
					throw new Exception(string.Format("Unable to resolve as a constant value: {0}", src), ex);
				}

			var ex_left = src as ICSharpCode.Decompiler.CSharp.Syntax.MemberReferenceExpression;
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
					

			if (value is ICSharpCode.Decompiler.CSharp.Syntax.ArrayCreateExpression)
				return new Constant() 
				{ 
					Source = value, 
					DefaultValue = (value as ICSharpCode.Decompiler.CSharp.Syntax.ArrayCreateExpression).Initializer.Children.Count(),
					CecilType = LoadType(typeof(int)),
					Parent = method
				};

            if (value is Array)
                return new Constant()
                {
                    DefaultValue = ((Array)value).Length,
                    Source = value,
                    CecilType = LoadType(typeof(int))
                };

			if (value is IMemberDefinition)
			{
				try
				{
					var mr = value as IMemberDefinition;
					if (mr is FieldDefinition && network.ConstantLookup.ContainsKey(mr as FieldDefinition))
						return ResolveArrayLengthOrPrimitive(network, proc, method, new ICSharpCode.Decompiler.CSharp.Syntax.PrimitiveExpression(network.ConstantLookup[mr as FieldDefinition]));
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
		protected virtual BreakStatement Decompile(NetworkState network, ProcessState proc, MethodState method, ICSharpCode.Decompiler.CSharp.Syntax.BreakStatement statement)
		{
			return new BreakStatement()
			{
				Parent = method,
			};
		}

        /// <summary>
        /// Processes a single statement from the decompiler and returns an AST entry for it
        /// </summary>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method where the statement is found.</param>
        /// <param name="statement">The decompiler statement to process.</param>
        protected virtual GotoStatement Decompile(NetworkState network, ProcessState proc, MethodState method, ICSharpCode.Decompiler.CSharp.Syntax.GotoStatement statement)
        {
            return new GotoStatement()
            {
                Parent = method,
                Label = statement.Label
            };
        }

        /// <summary>
        /// Processes a single statement from the decompiler and returns an AST entry for it
        /// </summary>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method where the statement is found.</param>
        /// <param name="statement">The decompiler statement to process.</param>
        protected virtual LabelStatement Decompile(NetworkState network, ProcessState proc, MethodState method, ICSharpCode.Decompiler.CSharp.Syntax.LabelStatement statement)
        {
            return new LabelStatement()
            {
                Parent = method,
                Label = statement.Label
            };
        }
		/// <summary>
		/// Processes a single statement from the decompiler and returns an AST entry for it
		/// </summary>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method where the statement is found.</param>
		/// <param name="statement">The decompiler statement to process.</param>
		protected virtual ForStatement Decompile(NetworkState network, ProcessState proc, MethodState method, ICSharpCode.Decompiler.CSharp.Syntax.ForStatement statement)
		{
			if (statement.Initializers.Count != 1)
				throw new Exception(string.Format("Only plain style for loops supported: {0}", statement));

			if (statement.Iterators.Count != 1)
				throw new Exception(string.Format("Only plain style for loops supported: {0}", statement));


			var init = statement.Initializers.First() as ICSharpCode.Decompiler.CSharp.Syntax.VariableDeclarationStatement;
			if (init == null)
				throw new Exception(string.Format("Only plain style for loops supported: {0}", statement));

			if (init.Variables.Count != 1)
				throw new Exception(string.Format("Only plain style for loops supported: {0}", statement));

			var name = init.Variables.First().Name;
			var initial = init.Variables.First().Initializer as ICSharpCode.Decompiler.CSharp.Syntax.PrimitiveExpression;

			if (initial == null || !(initial.Value is int))
				throw new Exception(string.Format("Only plain style for loops supported: {0}", statement));

			var startvalue = new Constant() 
			{ 
				Source = initial, 
				DefaultValue = (int)initial.Value,
				CecilType = LoadType(typeof(int))
			};

			var cond = statement.Condition as ICSharpCode.Decompiler.CSharp.Syntax.BinaryOperatorExpression;
			if (cond == null)
				throw new Exception(string.Format("Only plain style for loops supported: {0}", statement));

			if (cond.Operator != BinaryOperatorType.LessThan)
				throw new Exception(string.Format("Only plain style for loops supported: {0}", statement));

			var condleft = cond.Left as ICSharpCode.Decompiler.CSharp.Syntax.IdentifierExpression;
			var condright = cond.Right as ICSharpCode.Decompiler.CSharp.Syntax.PrimitiveExpression;

			// Handling cases where the upper limit is the length of an array
			if (condright == null)
			{
				// Some plus/minus expression
				if (cond.Right is ICSharpCode.Decompiler.CSharp.Syntax.BinaryOperatorExpression)
				{
					var binop = cond.Right as ICSharpCode.Decompiler.CSharp.Syntax.BinaryOperatorExpression;

					var leftval = ResolveArrayLengthOrPrimitive(network, proc, method, binop.Left);
					var righval = ResolveArrayLengthOrPrimitive(network, proc, method, binop.Right);

					if (binop.Operator == BinaryOperatorType.Add)
						condright = new ICSharpCode.Decompiler.CSharp.Syntax.PrimitiveExpression((int)leftval.DefaultValue + (int)righval.DefaultValue);
					else if (binop.Operator == BinaryOperatorType.Subtract)
						condright = new ICSharpCode.Decompiler.CSharp.Syntax.PrimitiveExpression((int)leftval.DefaultValue - (int)righval.DefaultValue);
					else
						throw new Exception(string.Format("Only add and subtract operations are supported in for loop bounds: {0}", statement));
				}
				// Plain limit
				else if (cond.Right is ICSharpCode.Decompiler.CSharp.Syntax.IdentifierExpression || cond.Right is ICSharpCode.Decompiler.CSharp.Syntax.MemberReferenceExpression)
				{
					condright = new ICSharpCode.Decompiler.CSharp.Syntax.PrimitiveExpression(ResolveArrayLengthOrPrimitive(network, proc, method, cond.Right).DefaultValue);
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

			var itr = statement.Iterators.First() as ICSharpCode.Decompiler.CSharp.Syntax.ExpressionStatement;
            if (itr == null || !(itr.Expression is ICSharpCode.Decompiler.CSharp.Syntax.UnaryOperatorExpression))
            {
                var ae = itr == null ? null : itr.Expression as ICSharpCode.Decompiler.CSharp.Syntax.AssignmentExpression;

                if (ae != null && ae.Left is ICSharpCode.Decompiler.CSharp.Syntax.IdentifierExpression && (ae.Left as ICSharpCode.Decompiler.CSharp.Syntax.IdentifierExpression).Identifier == name && ae.Right is ICSharpCode.Decompiler.CSharp.Syntax.PrimitiveExpression)
                {
                    // Support for increments like "i += 2"
                    increment = new Constant()
                    {
                        Source = ae.Right,
                        DefaultValue = ResolveArrayLengthOrPrimitive(network, proc, method, ae.Right as ICSharpCode.Decompiler.CSharp.Syntax.PrimitiveExpression),
                        CecilType = LoadType(typeof(int))
                    };
                }
                else
                    throw new Exception(string.Format("Only plain style for loops supported: {0}", statement));
            }
            else
            {
                var itre = itr.Expression as ICSharpCode.Decompiler.CSharp.Syntax.UnaryOperatorExpression;
                var itro = itre.Expression as ICSharpCode.Decompiler.CSharp.Syntax.IdentifierExpression;

                if (itro == null || itre.Operator != UnaryOperatorType.PostIncrement || itro.Identifier != name)
                    throw new Exception(string.Format("Only plain style for loops supported: {0}", statement));
            }

			var loopvar = new Variable()
			{
				CecilType = LoadType(typeof(int)),
				Name = name,
				Source = statement.Clone(),
                DefaultValue = startvalue.DefaultValue
			};

			var res = new ForStatement()
			{
				StartValue = startvalue,
				EndValue = endvalue,
				Increment = increment,
				LoopIndex = loopvar,
				//LoopBody = Decompile(network, proc, method, statement.EmbeddedStatement),
				Parent = method
			};

            method.StartScope(res);
            method.AddVariable(loopvar);

			loopvar.Parent = res;
            res.LoopBody = Decompile(network, proc, method, statement.EmbeddedStatement);

			res.LoopBody.Parent = res;
            method.FinishScope(res);

			return res;
		}
	}
}
