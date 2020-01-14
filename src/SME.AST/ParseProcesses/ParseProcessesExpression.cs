using System;
using System.Linq;
using ICSharpCode.Decompiler.CSharp.Syntax;

namespace SME.AST
{
	// This partial part deals with expressions
	public partial class ParseProcesses
	{
		/// <summary>
		/// Decompile the specified expression, given the network, process, method, and statement
		/// </summary>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method where the statement is found.</param>
		/// <param name="statement">The statement where the expression is found.</param>
		/// <param name="expression">The expression to decompile</param>
		protected Expression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, ICSharpCode.Decompiler.CSharp.Syntax.Expression expression)
		{
            if (expression is ICSharpCode.Decompiler.CSharp.Syntax.AssignmentExpression)
                return Decompile(network, proc, method, statement, expression as ICSharpCode.Decompiler.CSharp.Syntax.AssignmentExpression);
            else if (expression is ICSharpCode.Decompiler.CSharp.Syntax.IdentifierExpression)
                return Decompile(network, proc, method, statement, expression as ICSharpCode.Decompiler.CSharp.Syntax.IdentifierExpression);
            else if (expression is ICSharpCode.Decompiler.CSharp.Syntax.MemberReferenceExpression)
            	return Decompile(network, proc, method, statement, expression as ICSharpCode.Decompiler.CSharp.Syntax.MemberReferenceExpression);
            else if (expression is ICSharpCode.Decompiler.CSharp.Syntax.PrimitiveExpression)
                return Decompile(network, proc, method, statement, expression as ICSharpCode.Decompiler.CSharp.Syntax.PrimitiveExpression);
            else if (expression is ICSharpCode.Decompiler.CSharp.Syntax.BinaryOperatorExpression)
                return Decompile(network, proc, method, statement, expression as ICSharpCode.Decompiler.CSharp.Syntax.BinaryOperatorExpression);
            else if (expression is ICSharpCode.Decompiler.CSharp.Syntax.UnaryOperatorExpression)
                return Decompile(network, proc, method, statement, expression as ICSharpCode.Decompiler.CSharp.Syntax.UnaryOperatorExpression);
            else if (expression is ICSharpCode.Decompiler.CSharp.Syntax.IndexerExpression)
                return Decompile(network, proc, method, statement, expression as ICSharpCode.Decompiler.CSharp.Syntax.IndexerExpression);
            else if (expression is ICSharpCode.Decompiler.CSharp.Syntax.CastExpression)
                return Decompile(network, proc, method, statement, expression as ICSharpCode.Decompiler.CSharp.Syntax.CastExpression);
            else if (expression is ICSharpCode.Decompiler.CSharp.Syntax.ConditionalExpression)
                return Decompile(network, proc, method, statement, expression as ICSharpCode.Decompiler.CSharp.Syntax.ConditionalExpression);
            else if (expression is ICSharpCode.Decompiler.CSharp.Syntax.InvocationExpression)
			{
                var si = expression as ICSharpCode.Decompiler.CSharp.Syntax.InvocationExpression;
                var mt = si.Target as ICSharpCode.Decompiler.CSharp.Syntax.MemberReferenceExpression;

                if (mt.ToString() == "base.PrintDebug" || mt.ToString() == "base.SimulationOnly")
					return new EmptyExpression()
					{
						SourceExpression = si,
						Parent = statement
					};

				if (mt.ToString() == "Console.WriteLine" || mt.ToString() == "Console.Write")
					return new EmptyExpression()
					{
						SourceExpression = si,
						Parent = statement
					};


				// Catch common translations
                if (mt != null && (expression as ICSharpCode.Decompiler.CSharp.Syntax.InvocationExpression).Arguments.Count == 1)
				{
					if (mt.MemberName == "op_Implicit" || mt.MemberName == "op_Explicit")
					{
						var mtm = Decompile(network, proc, method, statement, mt);
                        return Decompile(network, proc, method, statement, new ICSharpCode.Decompiler.CSharp.Syntax.CastExpression(AstType.Create(mtm.SourceResultType.FullName), si.Arguments.First().Clone()));
					}
					else if (mt.MemberName == "op_Increment")
                        return Decompile(network, proc, method, statement, new ICSharpCode.Decompiler.CSharp.Syntax.UnaryOperatorExpression(UnaryOperatorType.Increment, si.Arguments.First().Clone()));
					else if (mt.MemberName == "op_Decrement")
                        return Decompile(network, proc, method, statement, new ICSharpCode.Decompiler.CSharp.Syntax.UnaryOperatorExpression(UnaryOperatorType.Decrement, si.Arguments.First().Clone()));
				}

                return Decompile(network, proc, method, statement, expression as ICSharpCode.Decompiler.CSharp.Syntax.InvocationExpression);
			}
            else if (expression is ICSharpCode.Decompiler.CSharp.Syntax.ParenthesizedExpression)
                return Decompile(network, proc, method, statement, expression as ICSharpCode.Decompiler.CSharp.Syntax.ParenthesizedExpression);
            else if (expression is ICSharpCode.Decompiler.CSharp.Syntax.NullReferenceExpression)
                return Decompile(network, proc, method, statement, expression as ICSharpCode.Decompiler.CSharp.Syntax.NullReferenceExpression);
            else if (expression is ICSharpCode.Decompiler.CSharp.Syntax.ArrayCreateExpression)
                return Decompile(network, proc, method, statement, expression as ICSharpCode.Decompiler.CSharp.Syntax.ArrayCreateExpression);
            else if (expression is ICSharpCode.Decompiler.CSharp.Syntax.CheckedExpression)
                return Decompile(network, proc, method, statement, expression as ICSharpCode.Decompiler.CSharp.Syntax.CheckedExpression);
            else if (expression is ICSharpCode.Decompiler.CSharp.Syntax.UncheckedExpression)
                return Decompile(network, proc, method, statement, expression as ICSharpCode.Decompiler.CSharp.Syntax.UncheckedExpression);
            else if (expression == ICSharpCode.Decompiler.CSharp.Syntax.Expression.Null)
				return new EmptyExpression() { SourceExpression = expression };
			else
				throw new Exception(string.Format("Unsupported expression: {0} ({1})", expression, expression.GetType().FullName));
		}


		/// <summary>
		/// Decompile the specified expression, given the network, process, method, and statement
		/// </summary>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method where the statement is found.</param>
		/// <param name="statement">The statement where the expression is found.</param>
		/// <param name="expression">The expression to decompile</param>
        protected Expression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, ICSharpCode.Decompiler.CSharp.Syntax.AssignmentExpression expression)
		{
			if (expression.ToString().StartsWith("base.DebugOutput = ", StringComparison.Ordinal))
				return new EmptyExpression()
				{
					Parent = statement,
					SourceExpression = expression,
				};

			if (expression.ToString().StartsWith("this.DebugOutput = ", StringComparison.Ordinal))
				return new EmptyExpression()
				{
					Parent = statement,
					SourceExpression = expression,
				};

			var lhs = Decompile(network, proc, method, statement, expression.Left);
			var rhs = Decompile(network, proc, method, statement, expression.Right);

			var res = new AssignmentExpression()
			{
				Operator = expression.Operator,
				SourceResultType = ResolveExpressionType(network, proc, method, statement, expression),
				SourceExpression = expression,
				Left = lhs,
				Right = rhs,
				Parent = statement
			};

			res.Left.Parent = res;
			res.Right.Parent = res;

			return res;
		}

		/// <summary>
		/// Decompile the specified expression, given the network, process, method, and statement
		/// </summary>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method where the statement is found.</param>
		/// <param name="statement">The statement where the expression is found.</param>
		/// <param name="expression">The expression to decompile</param>
		protected IdentifierExpression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, ICSharpCode.Decompiler.CSharp.Syntax.IdentifierExpression expression)
		{
			return new IdentifierExpression()
			{
				SourceResultType = ResolveExpressionType(network, proc, method, statement, expression),
				SourceExpression = expression,
				Target = LocateDataElement(network, proc, method, statement, expression),
				Parent = statement
			};
		}

		/// <summary>
		/// Decompile the specified expression, given the network, process, method, and statement
		/// </summary>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method where the statement is found.</param>
		/// <param name="statement">The statement where the expression is found.</param>
		/// <param name="expression">The expression to decompile</param>
		protected MemberReferenceExpression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, ICSharpCode.Decompiler.CSharp.Syntax.MemberReferenceExpression expression)
		{
			return new MemberReferenceExpression()
			{
				SourceResultType = ResolveExpressionType(network, proc, method, statement, expression),
				SourceExpression = expression,
				Target = LocateDataElement(network, proc, method, statement, expression),
				Parent = statement
			};
		}

		/// <summary>
		/// Decompile the specified expression, given the network, process, method, and statement
		/// </summary>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method where the statement is found.</param>
		/// <param name="statement">The statement where the expression is found.</param>
		/// <param name="expression">The expression to decompile</param>
		protected PrimitiveExpression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, ICSharpCode.Decompiler.CSharp.Syntax.PrimitiveExpression expression)
        {            
			return new PrimitiveExpression()
			{
				SourceResultType = ResolveExpressionType(network, proc, method, statement, expression),
				SourceExpression = expression,
				Value = expression.Value,
				Parent = statement
			};
		}

		/// <summary>
		/// Decompile the specified expression, given the network, process, method, and statement
		/// </summary>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method where the statement is found.</param>
		/// <param name="statement">The statement where the expression is found.</param>
		/// <param name="expression">The expression to decompile</param>
		protected BinaryOperatorExpression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, ICSharpCode.Decompiler.CSharp.Syntax.BinaryOperatorExpression expression)
		{
			var res = new BinaryOperatorExpression()
			{
				SourceResultType = ResolveExpressionType(network, proc, method, statement, expression),
				SourceExpression = expression,
				Operator = expression.Operator,
				Left = Decompile(network, proc, method, statement, expression.Left),
				Right = Decompile(network, proc, method, statement, expression.Right),
				Parent = statement
			};

			res.Left.Parent = res;
			res.Right.Parent = res;

			return res;
		}

		/// <summary>
		/// Decompile the specified expression, given the network, process, method, and statement
		/// </summary>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method where the statement is found.</param>
		/// <param name="statement">The statement where the expression is found.</param>
		/// <param name="expression">The expression to decompile</param>
        protected Expression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, ICSharpCode.Decompiler.CSharp.Syntax.UnaryOperatorExpression expression)
		{
            if (expression.Operator == UnaryOperatorType.Await)
            {
                var res = new AwaitExpression()
                {
                    SourceResultType = method.SourceMethod.Module.ImportReference(typeof(void)),
                    SourceExpression = expression,
                    Parent = statement
                };

                if (expression.Expression.ToString() != "base.ClockAsync ()")
                    throw new Exception("Only clock waits are supported for now");

                return res;
            }
            else
            {

                var res = new UnaryOperatorExpression()
                {
                    SourceResultType = ResolveExpressionType(network, proc, method, statement, expression),
                    SourceExpression = expression,
                    Operator = expression.Operator,
                    Operand = Decompile(network, proc, method, statement, expression.Expression),
                    Parent = statement
                };

                res.Operand.Parent = res;

                return res;
            }
		}

		/// <summary>
		/// Decompile the specified expression, given the network, process, method, and statement
		/// </summary>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method where the statement is found.</param>
		/// <param name="statement">The statement where the expression is found.</param>
		/// <param name="expression">The expression to decompile</param>
		protected IndexerExpression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, ICSharpCode.Decompiler.CSharp.Syntax.IndexerExpression expression)
		{
			if (expression.Arguments.Count != 1)
				throw new Exception($"Indexer expression had {expression.Arguments.Count} index arguments, only one is supported");

			var res = new IndexerExpression()
			{
				SourceResultType = ResolveExpressionType(network, proc, method, statement, expression),
				SourceExpression = expression,
				Target = LocateDataElement(network, proc, method, statement, expression),
				TargetExpression = Decompile(network, proc, method, statement, expression.Target),
				IndexExpression = Decompile(network, proc, method, statement, expression.Arguments.First()),
				Parent = statement
			};

			res.TargetExpression.Parent = res;
			res.IndexExpression.Parent = res;

			return res;
		}

		/// <summary>
		/// Decompile the specified expression, given the network, process, method, and statement
		/// </summary>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method where the statement is found.</param>
		/// <param name="statement">The statement where the expression is found.</param>
		/// <param name="expression">The expression to decompile</param>
		protected CastExpression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, ICSharpCode.Decompiler.CSharp.Syntax.CastExpression expression)
		{
			var res = new CastExpression()
			{
				SourceResultType = ResolveExpressionType(network, proc, method, statement, expression),
				SourceExpression = expression,
				Expression = Decompile(network, proc, method, statement, expression.Expression),
				Parent = statement
			};

			res.Expression.Parent = res;

			return res;
		}

		/// <summary>
		/// Decompile the specified expression, given the network, process, method, and statement
		/// </summary>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method where the statement is found.</param>
		/// <param name="statement">The statement where the expression is found.</param>
		/// <param name="expression">The expression to decompile</param>
		protected ConditionalExpression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, ICSharpCode.Decompiler.CSharp.Syntax.ConditionalExpression expression)
		{
			var res = new ConditionalExpression()
			{
				SourceResultType = ResolveExpressionType(network, proc, method, statement, expression),
				SourceExpression = expression,
				ConditionExpression = Decompile(network, proc, method, statement, expression.Condition),
				TrueExpression = Decompile(network, proc, method, statement, expression.TrueExpression),
				FalseExpression = Decompile(network, proc, method, statement, expression.FalseExpression),
				Parent = statement
			};

			res.ConditionExpression.Parent = res;
			res.TrueExpression.Parent = res;
			res.FalseExpression.Parent = res;

			return res;
		}

		/// <summary>
		/// Decompile the specified expression, given the network, process, method, and statement
		/// </summary>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method where the statement is found.</param>
		/// <param name="statement">The statement where the expression is found.</param>
		/// <param name="expression">The expression to decompile</param>
		protected InvocationExpression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, ICSharpCode.Decompiler.CSharp.Syntax.InvocationExpression expression)
		{
			var res = new InvocationExpression()
			{
				//SourceResultType = ResolveExpressionType(network, proc, method, statement, expression),
				SourceExpression = expression,
				//Target = LocateDataElement(network, proc, method, statement, expression),
				//TargetExpression = Decompile(network, proc, method, statement, expression.Target),
				ArgumentExpressions = expression.Arguments.Select(x => Decompile(network, proc, method, statement, x)).ToArray(),
				Parent = statement
			};

			foreach (var x in res.ArgumentExpressions)
				x.Parent = res;

			// Register for later lookup
			proc.MethodTargets.Enqueue(new Tuple<Statement, MethodState, InvocationExpression>(statement, method, res));

			return res;
		}

		/// <summary>
		/// Decompile the specified expression, given the network, process, method, and statement
		/// </summary>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method where the statement is found.</param>
		/// <param name="statement">The statement where the expression is found.</param>
		/// <param name="expression">The expression to decompile</param>
		protected ParenthesizedExpression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, ICSharpCode.Decompiler.CSharp.Syntax.ParenthesizedExpression expression)
		{
			var res = new ParenthesizedExpression()
			{
				SourceResultType = ResolveExpressionType(network, proc, method, statement, expression),
				SourceExpression = expression,
				Expression = Decompile(network, proc, method, statement, expression.Expression)				
			};

			res.Expression.Parent = res;

			return res;
		}

		/// <summary>
		/// Decompile the specified expression, given the network, process, method, and statement
		/// </summary>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method where the statement is found.</param>
		/// <param name="statement">The statement where the expression is found.</param>
		/// <param name="expression">The expression to decompile</param>
		protected NullReferenceExpression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, ICSharpCode.Decompiler.CSharp.Syntax.NullReferenceExpression expression)
		{
			return new NullReferenceExpression()
			{
				SourceResultType = ResolveExpressionType(network, proc, method, statement, expression),
				SourceExpression = expression,
				Parent = statement
			};
		}

		/// <summary>
		/// Decompile the specified expression, given the network, process, method, and statement
		/// </summary>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method where the statement is found.</param>
		/// <param name="statement">The statement where the expression is found.</param>
		/// <param name="expression">The expression to decompile</param>
		protected ArrayCreateExpression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, ICSharpCode.Decompiler.CSharp.Syntax.ArrayCreateExpression expression)
		{
			var res = new ArrayCreateExpression()
			{
				SourceResultType = ResolveExpressionType(network, proc, method, statement, expression),
				SourceExpression = expression,
				ElementExpressions = expression.Initializer.Elements.Select(x => Decompile(network, proc, method, statement, x)).ToArray(),
				Parent = statement
			};

			foreach (var x in res.ElementExpressions)
				x.Parent = res;

			return res;
		}

		/// <summary>
		/// Decompile the specified expression, given the network, process, method, and statement
		/// </summary>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method where the statement is found.</param>
		/// <param name="statement">The statement where the expression is found.</param>
		/// <param name="expression">The expression to decompile</param>
		protected CheckedExpression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, ICSharpCode.Decompiler.CSharp.Syntax.CheckedExpression expression)
		{
			var res = new CheckedExpression()
			{
				SourceResultType = ResolveExpressionType(network, proc, method, statement, expression),
				SourceExpression = expression,
				Expression = Decompile(network, proc, method, statement, expression.Expression),
				Parent = statement
			};

			res.Expression.Parent = res;

			return res;
		}

		/// <summary>
		/// Decompile the specified expression, given the network, process, method, and statement
		/// </summary>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method where the statement is found.</param>
		/// <param name="statement">The statement where the expression is found.</param>
		/// <param name="expression">The expression to decompile</param>
		protected UncheckedExpression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, ICSharpCode.Decompiler.CSharp.Syntax.UncheckedExpression expression)
		{
			var res = new UncheckedExpression()
			{
				SourceResultType = ResolveExpressionType(network, proc, method, statement, expression),
				SourceExpression = expression,
				Expression = Decompile(network, proc, method, statement, expression.Expression),
				Parent = statement
			};

			res.Expression.Parent = res;

			return res;
		}

	}
}
