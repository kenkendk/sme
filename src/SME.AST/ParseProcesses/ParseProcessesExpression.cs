using System;
using System.Linq;
using ICSharpCode.NRefactory.CSharp;

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
		protected Expression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, ICSharpCode.NRefactory.CSharp.Expression expression)
		{
			if (expression is ICSharpCode.NRefactory.CSharp.AssignmentExpression)
				return Decompile(network, proc, method, statement, expression as ICSharpCode.NRefactory.CSharp.AssignmentExpression);
			else if (expression is ICSharpCode.NRefactory.CSharp.IdentifierExpression)
				return Decompile(network, proc, method, statement, expression as ICSharpCode.NRefactory.CSharp.IdentifierExpression);
			else if (expression is ICSharpCode.NRefactory.CSharp.MemberReferenceExpression)
				return Decompile(network, proc, method, statement, expression as ICSharpCode.NRefactory.CSharp.MemberReferenceExpression);
			else if (expression is ICSharpCode.NRefactory.CSharp.PrimitiveExpression)
				return Decompile(network, proc, method, statement, expression as ICSharpCode.NRefactory.CSharp.PrimitiveExpression);
			else if (expression is ICSharpCode.NRefactory.CSharp.BinaryOperatorExpression)
				return Decompile(network, proc, method, statement, expression as ICSharpCode.NRefactory.CSharp.BinaryOperatorExpression);
			else if (expression is ICSharpCode.NRefactory.CSharp.UnaryOperatorExpression)
				return Decompile(network, proc, method, statement, expression as ICSharpCode.NRefactory.CSharp.UnaryOperatorExpression);
			else if (expression is ICSharpCode.NRefactory.CSharp.IndexerExpression)
				return Decompile(network, proc, method, statement, expression as ICSharpCode.NRefactory.CSharp.IndexerExpression);
			else if (expression is ICSharpCode.NRefactory.CSharp.CastExpression)
				return Decompile(network, proc, method, statement, expression as ICSharpCode.NRefactory.CSharp.CastExpression);
			else if (expression is ICSharpCode.NRefactory.CSharp.ConditionalExpression)
				return Decompile(network, proc, method, statement, expression as ICSharpCode.NRefactory.CSharp.ConditionalExpression);
			else if (expression is ICSharpCode.NRefactory.CSharp.InvocationExpression)
			{
				var si = expression as ICSharpCode.NRefactory.CSharp.InvocationExpression;
				var mt = si.Target as ICSharpCode.NRefactory.CSharp.MemberReferenceExpression;

				if (mt.ToString() == "base.PrintDebug")
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
				if (mt != null && (expression as ICSharpCode.NRefactory.CSharp.InvocationExpression).Arguments.Count == 1)
				{

					if (mt.MemberName == "op_Implicit" || mt.MemberName == "op_Explicit")
					{
						var mtm = Decompile(network, proc, method, statement, mt);
						return Decompile(network, proc, method, statement, new ICSharpCode.NRefactory.CSharp.CastExpression(AstType.Create(mtm.SourceResultType.FullName), si.Arguments.First().Clone()));
					}
					else if (mt.MemberName == "op_Increment")
						return Decompile(network, proc, method, statement, new ICSharpCode.NRefactory.CSharp.UnaryOperatorExpression(UnaryOperatorType.Increment, si.Arguments.First().Clone()));
					else if (mt.MemberName == "op_Decrement")
						return Decompile(network, proc, method, statement, new ICSharpCode.NRefactory.CSharp.UnaryOperatorExpression(UnaryOperatorType.Decrement, si.Arguments.First().Clone()));
				}

				return Decompile(network, proc, method, statement, expression as ICSharpCode.NRefactory.CSharp.InvocationExpression);
			}
			else if (expression is ICSharpCode.NRefactory.CSharp.ParenthesizedExpression)
				return Decompile(network, proc, method, statement, expression as ICSharpCode.NRefactory.CSharp.ParenthesizedExpression);
			else if (expression is ICSharpCode.NRefactory.CSharp.NullReferenceExpression)
				return Decompile(network, proc, method, statement, expression as ICSharpCode.NRefactory.CSharp.NullReferenceExpression);
			else if (expression is ICSharpCode.NRefactory.CSharp.ArrayCreateExpression)
				return Decompile(network, proc, method, statement, expression as ICSharpCode.NRefactory.CSharp.ArrayCreateExpression);
			else if (expression is ICSharpCode.NRefactory.CSharp.CheckedExpression)
				return Decompile(network, proc, method, statement, expression as ICSharpCode.NRefactory.CSharp.CheckedExpression);
			else if (expression is ICSharpCode.NRefactory.CSharp.UncheckedExpression)
				return Decompile(network, proc, method, statement, expression as ICSharpCode.NRefactory.CSharp.UncheckedExpression);
			else if (expression == ICSharpCode.NRefactory.CSharp.Expression.Null)
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
		protected Expression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, ICSharpCode.NRefactory.CSharp.AssignmentExpression expression)
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
		protected IdentifierExpression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, ICSharpCode.NRefactory.CSharp.IdentifierExpression expression)
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
		protected MemberReferenceExpression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, ICSharpCode.NRefactory.CSharp.MemberReferenceExpression expression)
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
		protected PrimitiveExpression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, ICSharpCode.NRefactory.CSharp.PrimitiveExpression expression)
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
		protected BinaryOperatorExpression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, ICSharpCode.NRefactory.CSharp.BinaryOperatorExpression expression)
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
		protected UnaryOperatorExpression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, ICSharpCode.NRefactory.CSharp.UnaryOperatorExpression expression)
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

		/// <summary>
		/// Decompile the specified expression, given the network, process, method, and statement
		/// </summary>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method where the statement is found.</param>
		/// <param name="statement">The statement where the expression is found.</param>
		/// <param name="expression">The expression to decompile</param>
		protected IndexerExpression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, ICSharpCode.NRefactory.CSharp.IndexerExpression expression)
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
		protected CastExpression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, ICSharpCode.NRefactory.CSharp.CastExpression expression)
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
		protected ConditionalExpression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, ICSharpCode.NRefactory.CSharp.ConditionalExpression expression)
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
		protected InvocationExpression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, ICSharpCode.NRefactory.CSharp.InvocationExpression expression)
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
		protected ParenthesizedExpression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, ICSharpCode.NRefactory.CSharp.ParenthesizedExpression expression)
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
		protected NullReferenceExpression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, ICSharpCode.NRefactory.CSharp.NullReferenceExpression expression)
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
		protected ArrayCreateExpression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, ICSharpCode.NRefactory.CSharp.ArrayCreateExpression expression)
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
		protected CheckedExpression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, ICSharpCode.NRefactory.CSharp.CheckedExpression expression)
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
		protected UncheckedExpression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, ICSharpCode.NRefactory.CSharp.UncheckedExpression expression)
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
