using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SME.AST
{
	public enum VisitorState
	{
		/// <summary>
		/// Enter the element
		/// </summary>
		Enter,
		/// <summary>
		/// Visit the element
		/// </summary>
		Visit,
		/// <summary>
		/// Visited the element
		/// </summary>
		Visited,
		/// <summary>
		/// Leave the element
		/// </summary>
		Leave
	}

	/// <summary>
	/// Methods that enumerate all elements in various parts of the network
	/// </summary>
	public static class EnumerationExtensions
	{
		/// <summary>
		/// Visits all elements in depth-first-search, using post-order vistits
		/// </summary>
		/// <returns>The sequence of elements in depth-first post-order.</returns>
		/// <param name="self">The item to enumerate.</param>
		/// <param name="visitor">The visitor method. Return <c>false</c> to prevent entering this node.</param>
		public static IEnumerable<ASTItem> DepthFirstPostOrder(this ASTItem self, Func<ASTItem, VisitorState, bool> visitor = null)
		{
			visitor = visitor ?? ((a, b) => true);
			var work = new List<ASTItem>();
			foreach (var n in All(self, (el, state) => {
				if (state == VisitorState.Leave)
					work.Add(el);
				return visitor(el, state);
			}))
			{
				foreach (var el in work)
					yield return el;

				work.Clear();
			}

			foreach (var el in work)
				yield return el;
		}

		/// <summary>
		/// Returns all the leaves in the sequence originating in the given item
		/// </summary>
		/// <returns>The leaves.</returns>
		/// <param name="self">The items to enumerate.</param>
		/// <param name="visitor">The visitor method. Return <c>false</c> to prevent entering this node.</param>
		public static IEnumerable<ASTItem> LeavesOnly(this ASTItem self, Func<ASTItem, VisitorState, bool> visitor = null)
		{
			var work = new List<ASTItem>();
			ASTItem last = null;
			foreach (var n in All(self, (el, state) =>
			{
				if (state == VisitorState.Enter)
					last = el;
				if (state == VisitorState.Leave)
				{
					if (el == last) 
						work.Add(el);
					last = null;
				}
				return visitor(el, state);
			}))
			{
				foreach (var el in work)
					yield return el;

				work.Clear();
			}
			foreach (var el in work)
				yield return el;
		}


		/// <summary>
		/// Enumerates all elements in the item, and optionally applies the visitor function
		/// </summary>
		/// <param name="self">The item to traverse.</param>
		/// <param name="visitor">The visitor method. Return <c>false</c> to prevent entering this node.</param>
		public static IEnumerable<ASTItem> All(this ASTItem self, Func<ASTItem, VisitorState, bool> visitor = null)
		{
			if (self is Network)
				return All((Network)self, visitor);
			if (self is Process)
				return All((Process)self, visitor);
			if (self is Bus)
				return All((Bus)self, visitor);
			if (self is Method)
				return All((Method)self, visitor);
			if (self is Statement)
				return All((Statement)self, visitor);
			if (self is Expression)
				return All((Expression)self, visitor);

			throw new Exception($"Unable to visit expression of type {self.GetType().FullName}");
		}

		/// <summary>
		/// Enumerates all elements in the network, and optionally applies the visitor function
		/// </summary>
		/// <param name="network">The network to traverse.</param>
		/// <param name="visitor">The visitor method. Return <c>false</c> to prevent entering this node.</param>
		public static IEnumerable<ASTItem> All(this Network network, Func<ASTItem, VisitorState, bool> visitor = null)
		{
			visitor = visitor ?? ((a, b) => true);
			if (!visitor(network, VisitorState.Enter))
				yield break;

			if (!visitor(network, VisitorState.Visit))
				yield break;
			yield return network;
			if (!visitor(network, VisitorState.Visited))
				yield break;

			foreach (var p in network.Constants)
				yield return p;

			foreach (var p in network.Processes)
				foreach (var x in p.All(visitor))
					yield return x;

			visitor(network, VisitorState.Leave);
		}

		/// <summary>
		/// Enumerates all elements in the process, and optionally applies the visitor function
		/// </summary>
		/// <param name="proc">The process to traverse.</param>
		/// <param name="visitor">The visitor method. Return <c>false</c> to prevent entering this node.</param>
		public static IEnumerable<ASTItem> All(this Process proc, Func<ASTItem, VisitorState, bool> visitor = null)
		{
			visitor = visitor ?? ((a, b) => true);

			if (!visitor(proc, VisitorState.Enter))
				yield break;

			if (!visitor(proc, VisitorState.Visit))
				yield break;
			yield return proc;
			if (!visitor(proc, VisitorState.Visited))
				yield break;

			foreach (var p in proc.InputBusses)
				foreach (var x in p.All(visitor))
					yield return x;

			foreach (var p in proc.InternalBusses)
				foreach (var x in p.All(visitor))
					yield return x;
			
			foreach (var p in proc.OutputBusses)
				foreach (var x in p.All(visitor))
					yield return x;

			foreach (var p in proc.SharedSignals)
				if (visitor(p, VisitorState.Enter))
				{
					if (!visitor(p, VisitorState.Visit))
						yield break;
					yield return p;
					if (!visitor(p, VisitorState.Visited))
						yield break;
					visitor(p, VisitorState.Leave);
				}

			foreach (var p in proc.SharedVariables)
				if (visitor(p, VisitorState.Enter))
				{
					if (!visitor(proc, VisitorState.Visit))
						yield break;
					yield return p;
					if (!visitor(proc, VisitorState.Visited))
						yield break;
					visitor(p, VisitorState.Leave);
				}

			if (proc.MainMethod != null)
				foreach (var x in proc.MainMethod.All(visitor))
					yield return x;

			if (proc.Methods != null)
				foreach (var p in proc.Methods)
					foreach (var x in p.All(visitor))
						yield return x;

			visitor(proc, VisitorState.Leave);
		}

		/// <summary>
		/// Enumerates all elements in the bus, and optionally applies the visitor function
		/// </summary>
		/// <param name="bus">The bus to traverse.</param>
		/// <param name="visitor">The visitor method. Return <c>false</c> to prevent entering this node.</param>
		public static IEnumerable<ASTItem> All(this Bus bus, Func<ASTItem, VisitorState, bool> visitor = null)
		{
			visitor = visitor ?? ((a, b) => true);
			if (!visitor(bus, VisitorState.Enter))
				yield break;

			if (!visitor(bus, VisitorState.Visit))
				yield break;
			yield return bus;
			if (!visitor(bus, VisitorState.Visited))
				yield break;

			foreach (var s in bus.Signals)
				if (visitor(s, VisitorState.Enter))
				{
					if (!visitor(s, VisitorState.Visit))
						yield break;
					yield return s;
					if (!visitor(s, VisitorState.Visited))
						yield break;
 					visitor(s, VisitorState.Leave);
				}

			visitor(bus, VisitorState.Leave);
		}

		/// <summary>
		/// Enumerates all elements in the method, and optionally applies the visitor function
		/// </summary>
		/// <param name="method">The method to traverse.</param>
		/// <param name="visitor">The visitor method. Return <c>false</c> to prevent entering this node.</param>
		public static IEnumerable<ASTItem> All(this Method method, Func<ASTItem, VisitorState, bool> visitor = null)
		{
			visitor = visitor ?? ((a, b) => true);
			if (!visitor(method, VisitorState.Enter))
				yield break;

			if (!visitor(method, VisitorState.Visit))
				yield break;
			yield return method;
			if (!visitor(method, VisitorState.Visited))
				yield break;

			foreach (var p in method.Parameters)
				if (visitor(p, VisitorState.Enter))
				{
					if (!visitor(p, VisitorState.Visit))
						yield break;
					yield return p;
					if (!visitor(p, VisitorState.Visited))
						yield break;
					visitor(p, VisitorState.Leave);
				}
			
			foreach (var p in method.Variables)
				if (visitor(p, VisitorState.Enter))
				{
					if (!visitor(p, VisitorState.Visit))
						yield break;
					yield return p;
					if (!visitor(p, VisitorState.Visited))
						yield break;
					visitor(p, VisitorState.Leave);
				}

			if (method.ReturnVariable != null)
				if (visitor(method.ReturnVariable, VisitorState.Enter))
				{
					if (!visitor(method.ReturnVariable, VisitorState.Visit))
						yield break;
					yield return method.ReturnVariable;
					if (!visitor(method.ReturnVariable, VisitorState.Visited))
						yield break;
					visitor(method.ReturnVariable, VisitorState.Leave);
				}

			foreach (var s in method.Statements)
				foreach (var x in s.All(visitor))
					yield return x;

			visitor(method, VisitorState.Leave);
		}

		/// <summary>
		/// Enumerates all elements in the statement, and optionally applies the visitor function
		/// </summary>
		/// <param name="statement">The statement to traverse.</param>
		/// <param name="visitor">The visitor method. Return <c>false</c> to prevent entering this node.</param>
		public static IEnumerable<ASTItem> All(this Statement statement, Func<ASTItem, VisitorState, bool> visitor = null)
		{
			visitor = visitor ?? ((a, b) => true);
			if (!visitor(statement, VisitorState.Enter))
				yield break;

			if (!visitor(statement, VisitorState.Visit))
				yield break;
			yield return statement;
			if (!visitor(statement, VisitorState.Visited))
				yield break;

			if (statement is ExpressionStatement)
			{
				foreach (var x in ((ExpressionStatement)statement).Expression.All(visitor))
					yield return x;
			}
			else if (statement is EmptyStatement)
			{
			}
			else if (statement is IfElseStatement)
			{
				var e = statement as IfElseStatement;
				foreach (var p in e.Condition.All(visitor))
					yield return p;

				foreach (var p in e.TrueStatement.All(visitor))
					yield return p;

				foreach (var p in e.FalseStatement.All(visitor))
					yield return p;

			}
			else if (statement is BlockStatement)
			{
				var e = statement as BlockStatement;
				foreach (var p in e.Statements)
					foreach (var x in p.All(visitor))
						yield return x;
			}
			else if (statement is SwitchStatement)
			{
				var e = statement as SwitchStatement;
				foreach (var p in e.SwitchExpression.All(visitor))
					yield return p;

				foreach (var p in e.Cases)
				{
					foreach (var x in p.Item1)
						foreach (var y in x.All(visitor))
							yield return y;
					foreach (var x in p.Item2)
						foreach (var y in x.All(visitor))
							yield return y;
				}
			}
			else if (statement is ReturnStatement)
			{
				var e = statement as ReturnStatement;
				if (e.ReturnExpression != null)
					foreach (var p in e.ReturnExpression.All(visitor))
						yield return p;
			}
			else if (statement is ForStatement)
			{
				var e = statement as ForStatement;

				if (visitor(e.StartValue, VisitorState.Enter))
				{
					if (!visitor(e.StartValue, VisitorState.Visit))
						yield break;
					yield return e.StartValue;
					if (!visitor(e.StartValue, VisitorState.Visited))
						yield break;
					visitor(e.StartValue, VisitorState.Leave);
				}

				if (visitor(e.EndValue, VisitorState.Enter))
				{
					if (!visitor(e.EndValue, VisitorState.Visit))
						yield break;
					yield return e.EndValue;
					if (!visitor(e.EndValue, VisitorState.Visited))
						yield break;
					visitor(e.EndValue, VisitorState.Leave);
				}

				if (visitor(e.Increment, VisitorState.Enter))
				{
					if (!visitor(e.Increment, VisitorState.Visit))
						yield break;
					yield return e.Increment;
					if (!visitor(e.Increment, VisitorState.Visited))
						yield break;
					visitor(e.Increment, VisitorState.Leave);
				}

				if (visitor(e.LoopIndex, VisitorState.Enter))
				{
					if (!visitor(e.LoopIndex, VisitorState.Visited))
						yield break;
					yield return e.LoopIndex;
					if (!visitor(e.LoopIndex, VisitorState.Visited))
						yield break;
					visitor(e.LoopIndex, VisitorState.Leave);
				}

				foreach (var p in e.LoopBody.All(visitor))
					yield return p;
			}
			else if (statement is CommentStatement || statement is BreakStatement)
			{
			}
			else
			{
				throw new Exception($"Unsupported statement type: {statement.GetType().FullName}");
			}

			visitor(statement, VisitorState.Leave);
		}

		/// <summary>
		/// Enumerates all elements in the expression, and optionally applies the visitor function
		/// </summary>
		/// <param name="expression">The expression to traverse.</param>
		/// <param name="visitor">The visitor method. Return <c>false</c> to prevent entering this node.</param>
		public static IEnumerable<ASTItem> All(this Expression expression, Func<ASTItem, VisitorState, bool> visitor = null)
		{
			visitor = visitor ?? ((a, b) => true);
			if (expression is CustomExpression)
			{
				foreach (var e in ((CustomExpression)expression).Visit(visitor))
					yield return e;
			}
			else
			{
				if (!visitor(expression, VisitorState.Enter))
					yield break;

				var isSplitOp =
					expression is AssignmentExpression
					||
					expression is BinaryOperatorExpression;

				if (!isSplitOp)
				{
					if (!visitor(expression, VisitorState.Visit))
						yield break;
					yield return expression;
					if (!visitor(expression, VisitorState.Visited))
						yield break;
				}

				if (expression is ArrayCreateExpression)
				{
					foreach (var p in ((ArrayCreateExpression)expression).ElementExpressions)
						foreach (var x in p.All(visitor))
							yield return x;
				}
				else if (expression is EmptyArrayCreateExpression)
				{
					foreach (var x in ((EmptyArrayCreateExpression)expression).SizeExpression.All(visitor))
						yield return x;
				}
				else if (expression is AssignmentExpression)
				{
					foreach (var x in ((AssignmentExpression)expression).Left.All(visitor))
						yield return x;

					if (!visitor(expression, VisitorState.Visit))
						yield break;
					yield return expression;
					if (!visitor(expression, VisitorState.Visited))
						yield break;

					foreach (var x in ((AssignmentExpression)expression).Right.All(visitor))
						yield return x;
				}
				else if (expression is BinaryOperatorExpression)
				{
					foreach (var x in ((BinaryOperatorExpression)expression).Left.All(visitor))
						yield return x;

					if (!visitor(expression, VisitorState.Visit))
						yield break;
					yield return expression;
					if (!visitor(expression, VisitorState.Visited))
						yield break;

					foreach (var x in ((BinaryOperatorExpression)expression).Right.All(visitor))
						yield return x;
				}
				else if (expression is ConditionalExpression)
				{
					foreach (var x in ((ConditionalExpression)expression).ConditionExpression.All(visitor))
						yield return x;
					foreach (var x in ((ConditionalExpression)expression).TrueExpression.All(visitor))
						yield return x;
					foreach (var x in ((ConditionalExpression)expression).FalseExpression.All(visitor))
						yield return x;
				}
				else if (expression is EmptyExpression || expression is NullReferenceExpression)
				{
				}
				else if (expression is IdentifierExpression)
				{
				}
				else if (expression is IndexerExpression)
				{
					foreach (var x in ((IndexerExpression)expression).TargetExpression.All(visitor))
						yield return x;
					foreach (var x in ((IndexerExpression)expression).IndexExpression.All(visitor))
						yield return x;
				}
				else if (expression is InvocationExpression)
				{
					foreach (var x in ((InvocationExpression)expression).TargetExpression.All(visitor))
						yield return x;
					foreach (var p in ((InvocationExpression)expression).ArgumentExpressions)
						foreach (var x in p.All(visitor))
							yield return x;
				}
				else if (expression is MemberReferenceExpression || expression is MethodReferenceExpression)
				{
				}
				else if (expression is ParenthesizedExpression)
				{
					foreach (var x in ((ParenthesizedExpression)expression).Expression.All(visitor))
						yield return x;
				}
				else if (expression is PrimitiveExpression)
				{
				}
				else if (expression is UnaryOperatorExpression)
				{
					foreach (var x in ((UnaryOperatorExpression)expression).Operand.All(visitor))
						yield return x;
				}
				else if (
					expression.GetType() == typeof(WrappingExpression)
					|| expression is UncheckedExpression
					|| expression is ParenthesizedExpression
					|| expression is CheckedExpression
					|| expression is CastExpression)
				{
					foreach (var x in ((WrappingExpression)expression).Expression.All(visitor))
						yield return x;
				}
				else if (expression is CustomExpression)
				{
					foreach (var x in ((CustomExpression)expression).Visit(visitor))
						yield return x;
				}
				else
				{
					throw new Exception($"No handler for expression of type {expression.GetType().FullName}");
				}

				visitor(expression, VisitorState.Leave);
			}
		}

		/// <summary>
		/// Replaces one statement with another
		/// </summary>
		/// <param name="self">The statement to replace.</param>
		/// <param name="replacement">The replacement statement.</param>
		public static void ReplaceWith(this Statement self, Statement replacement)
		{
			if (self.Parent is Method)
			{
				var mt = self.Parent as Method;
				for (var i = 0; i < mt.Statements.Length; i++)
					if (mt.Statements[i] == self)
					{
						mt.Statements[i] = replacement;
						replacement.Parent = mt;
						return;
					}
			}
			else
			{
				var parent = self.Parent as Statement;
				if (parent == null)
					throw new Exception($"The parent was expected to be a {nameof(Statement)} or a {nameof(Method)}, but was {self.Parent.GetType().FullName}");

				if (parent is IfElseStatement)
				{
					var ts = parent as IfElseStatement;
					if (ts.TrueStatement == self)
					{
						ts.TrueStatement = replacement;
						replacement.Parent = ts;
						return;
					}
					else if (ts.FalseStatement == self)
					{
						ts.FalseStatement = replacement;
						replacement.Parent = ts;
						return;
					}
				}
				else if (parent is BlockStatement)
				{
					var ts = parent as BlockStatement;
					for (var i = 0; i < ts.Statements.Length; i++)
						if (ts.Statements[i] == self)
						{
							ts.Statements[i] = replacement;
							replacement.Parent = ts;
							return;
						}
				}
				else if (parent is SwitchStatement)
				{
					var ts = parent as SwitchStatement;
					foreach(var cs in ts.Cases)
						for (var i = 0; i < cs.Item2.Length; i++)
							if (cs.Item2[i] == self)
							{
								cs.Item2[i] = replacement;
								replacement.Parent = ts;
								return;
							}
				}
				else if (parent is ForStatement)
				{
					var ts = parent as ForStatement;
					if (ts.LoopBody == self)
					{
						ts.LoopBody = replacement;
						replacement.Parent = ts;
						return;
					}
				}
			}

			throw new Exception("Item not found in parent");
		}

		/// <summary>
		/// Performs a replacement, by inserting the replacement expression instead of the current expression
		/// </summary>
		/// <param name="self">The expression to replace.</param>
		/// <param name="replacement">The expression to replace it with.</param>
		public static Expression ReplaceWith(this Expression self, Expression replacement)
		{
			if (self.Parent is Statement)
			{
				var parent = self.Parent as Statement;
				if (parent is IfElseStatement)
				{
					var ts = parent as IfElseStatement;
					if (ts.Condition == self)
					{
						ts.Condition = replacement;
						replacement.Parent = parent;
						return replacement;
					}
				}
				else if (parent is ExpressionStatement)
				{
					var ts = parent as ExpressionStatement;
					if (ts.Expression == self)
					{
						ts.Expression = replacement;
						replacement.Parent = parent;
						return replacement;
					}
				}
				else if (parent is SwitchStatement)
				{
					var ts = parent as SwitchStatement;
					if (ts.SwitchExpression == self)
					{
						ts.SwitchExpression = replacement;
						replacement.Parent = parent;
						return replacement;
					}

					foreach (var cs in ts.Cases)
						for (var i = 0; i < cs.Item1.Length; i++)
							if (cs.Item1[i] == self)
							{
								cs.Item1[i] = replacement;
								replacement.Parent = parent;
								return replacement;
							}
				}
			}
			else
			{
				var parent = (Expression)self.Parent;

				if (parent is ArrayCreateExpression)
				{
					var ap = ((ArrayCreateExpression)parent).ElementExpressions;

					for (var i = 0; i < ap.Length; i++)
						if (ap[i] == self)
						{
							ap[i] = replacement;
							replacement.Parent = parent;
							return replacement;
						}
				}
				else if (parent is EmptyArrayCreateExpression)
				{
					var ap = ((EmptyArrayCreateExpression)parent);
					if (ap.SizeExpression == self)
					{
						ap.SizeExpression = replacement;
						replacement.Parent = parent;
						return replacement;
					}					
				}
				else if (parent is AssignmentExpression)
				{
					var ap = ((AssignmentExpression)parent);
					if (ap.Left == self)
					{
						ap.Left = replacement;
						replacement.Parent = parent;
						return replacement;
					}
					else if (ap.Right == self)
					{
						ap.Right = replacement;
						replacement.Parent = parent;
						return replacement;
					}
				}
				else if (parent is BinaryOperatorExpression)
				{
					var ap = ((BinaryOperatorExpression)parent);
					if (ap.Left == self)
					{
						ap.Left = replacement;
						replacement.Parent = parent;
						return replacement;
					}
					else if (ap.Right == self)
					{
						ap.Right = replacement;
						replacement.Parent = parent;
						return replacement;
					}
				}
				else if (parent is ConditionalExpression)
				{
					var ap = ((ConditionalExpression)parent);
					if (ap.ConditionExpression == self)
					{
						ap.ConditionExpression = replacement;
						replacement.Parent = parent;
						return replacement;
					}
					else if (ap.TrueExpression == self)
					{
						ap.TrueExpression = replacement;
						replacement.Parent = parent;
						return replacement;
					}
					else if (ap.FalseExpression == self)
					{
						ap.FalseExpression = replacement;
						replacement.Parent = parent;
						return replacement;
					}
				}
				else if (parent is IndexerExpression)
				{
					var ap = ((IndexerExpression)parent);
					if (ap.TargetExpression == self)
					{
						ap.TargetExpression = replacement;
						replacement.Parent = parent;
						return replacement;
					}
					else if (ap.IndexExpression == self)
					{
						ap.IndexExpression = replacement;
						replacement.Parent = parent;
						return replacement;
					}
				}
				else if (parent is InvocationExpression)
				{
					var ap = ((InvocationExpression)parent);
					if (ap.TargetExpression == self)
					{
						ap.TargetExpression = replacement;
						replacement.Parent = parent;
						return replacement;
					}

					for (var i = 0; i < ap.ArgumentExpressions.Length; i++)
						if (ap.ArgumentExpressions[i] == self)
						{
							ap.ArgumentExpressions[i] = replacement;
							replacement.Parent = parent;
							return replacement;
						}
				}
				else if (parent is ParenthesizedExpression)
				{
					var ap = ((ParenthesizedExpression)parent);
					if (ap.Expression == self)
					{
						ap.Expression = replacement;
						replacement.Parent = parent;
						return replacement;
					}
				}
				else if (parent is UnaryOperatorExpression)
				{
					var ap = ((UnaryOperatorExpression)parent);
					if (ap.Operand == self)
					{
						ap.Operand = replacement;
						replacement.Parent = parent;
						return replacement;
					}
				}
				else if (
					parent.GetType() == typeof(WrappingExpression)
					|| parent is UncheckedExpression
					|| parent is ParenthesizedExpression
					|| parent is CheckedExpression
					|| parent is CastExpression)
				{
					var ap = ((WrappingExpression)parent);
					if (ap.Expression == self)
					{
						ap.Expression = replacement;
						replacement.Parent = parent;
						return replacement;
					}
				}
				else if (parent is CustomExpression)
				{
					var children = ((CustomExpression)parent).Children;
					if (children != null)
						for (var i = 0; i < children.Length; i++)
							if (children[i] == self)
							{
								children[i] = replacement;
								replacement.Parent = parent;
								((CustomExpression)parent).Children = children;

								if (((CustomExpression)parent).Children[i] != replacement)
									throw new Exception($"Cannot update the children of an element of type {parent.GetType().FullName}, make sure it returns a real array reference");
							
 								return replacement;
							}
				}
			}

			throw new Exception("Item not found in parent");
		}

		/// <summary>
		/// Helper method that updates all child expressions or statements
		/// by setting their parent to the immediate parent
		/// </summary>
		/// <param name="self">The item to update.</param>
		public static void UpdateParents(this Statement self)
		{
			var parents = new Stack<ASTItem>();

			foreach (var n in self.All((item, type) =>
			{
				if (!(item is Statement || item is Expression))
					return false;
				
				if (type == VisitorState.Enter)
				{
					if (item != self)
						item.Parent = parents.Peek();
					parents.Push(item);
				}
				else if (type == VisitorState.Leave)
					parents.Pop();

				return true;
			})) { }

			if (parents.Count != 0)
				throw new Exception($"{nameof(UpdateParents)} is broken ...");
		}

		/// <summary>
		/// Inserts a statement before the source expression
		/// </summary>
		/// <param name="source">The source expression.</param>
		/// <param name="target">The statement to insert.</param>
		public static void PrependStatement(this Expression source, Statement target)
		{
			var p = source.Parent;
			while (p != null && !(p is Statement))
				p = p.Parent;

			var stm = p as Statement;
			if (stm == null)
				throw new Exception("Unable to find a parent statement");

			stm.PrependStatement(target);
		}

		/// <summary>
		/// Inserts a statement before the source statement
		/// </summary>
		/// <param name="source">The source statement.</param>
		/// <param name="target">The statement to insert.</param>
		public static void PrependStatement(this Statement source, Statement target)
		{
			if (source is BlockStatement)
			{
				var bst = source as BlockStatement;
				var n = new Statement[bst.Statements.Length + 1];
				Array.Copy(bst.Statements, 0, n, 1, bst.Statements.Length);
				n[0] = target;
				target.Parent = bst;
				bst.Statements = n;
				return;
			}
			else
			{
				var blst = new BlockStatement()
				{
					Parent = source.Parent,
					SourceStatement = source.SourceStatement,
					Statements = new Statement[] { target, source }
				};

				source.ReplaceWith(blst);
				target.Parent = source.Parent = blst;
			}
		}

		/// <summary>
		/// Inserts a statement after the source expression
		/// </summary>
		/// <param name="source">The source expression.</param>
		/// <param name="target">The statement to insert.</param>
		public static void AppendStatement(this Expression source, Statement target)
		{
			var p = source.Parent;
			while (p != null && !(p is Statement))
				p = p.Parent;

			var stm = p as Statement;
			if (stm == null)
				throw new Exception("Unable to find a parent statement");

			stm.AppendStatement(target);
		}

		/// <summary>
		/// Inserts a statement after the source statement
		/// </summary>
		/// <param name="source">The source statement.</param>
		/// <param name="target">The statement to insert.</param>
		public static void AppendStatement(this Statement source, Statement target)
		{
			if (source is BlockStatement)
			{
				var bst = source as BlockStatement;
				var n = new Statement[bst.Statements.Length + 1];
				Array.Copy(bst.Statements, 0, n, 0, bst.Statements.Length);
				n[n.Length - 1] = target;
				target.Parent = bst;
				bst.Statements = n;
				return;
			}
			else
			{
				var blst = new BlockStatement()
				{
					Parent = source.Parent,
					SourceStatement = source.SourceStatement,
					Statements = new Statement[] { source, target }
				};

				source.ReplaceWith(blst);
				target.Parent = source.Parent = blst;
			}
		}
	}
}
