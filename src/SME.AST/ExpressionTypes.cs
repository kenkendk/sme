using System;
using System.Collections.Generic;

namespace SME.AST
{
	/// <summary>
	/// An expression found in a statement
	/// </summary>
	public abstract class Expression : ASTItem
	{
		/// <summary>
		/// The source expression used in the statement
		/// </summary>
		public ICSharpCode.NRefactory.CSharp.Expression SourceExpression;

		/// <summary>
		/// The source result type 
		/// </summary>
		public Mono.Cecil.TypeReference SourceResultType;
	}

	/// <summary>
	/// Base class for custom defined expressions that can enter into a the normal tree
	/// </summary>
	public abstract class CustomExpression : Expression
	{
		/// <summary>
		/// Visits the element, optionally with a visitor callback
		/// </summary>
		/// <returns>The elements in this element and its children.</returns>
		/// <param name="visitor">An optional visitor function.</param>
		public virtual IEnumerable<ASTItem> Visit(Func<ASTItem, VisitorState, bool> visitor = null)
		{
			visitor = visitor ?? ((a, b) => true);

			if (!visitor(this, VisitorState.Enter))
				yield break;

			if (!visitor(this, VisitorState.Visit))
				yield break;
			yield return this;
			if (!visitor(this, VisitorState.Visited))
				yield break;

			if (this.Children != null)
				foreach (var p in this.Children)
					foreach (var x in p.All(visitor))
						yield return x;

			if (!visitor(this, VisitorState.Leave))
				yield break;

		}

		/// <summary>
		/// List the children of this element
		/// </summary>
		public abstract Expression[] Children { get; set; }
	}

	/// <summary>
	/// Base class for an expression that wraps another expression
	/// </summary>
	public abstract class WrappingExpression : Expression
	{
		public Expression Expression;
	}

	/// <summary>
	/// An expression that creates an array
	/// </summary>
	public class ArrayCreateExpression : Expression
	{
		/// <summary>
		/// The elements in the array
		/// </summary>
		public Expression[] ElementExpressions;
	}

	/// <summary>
	/// An expression that indicates that the array is created with default values
	/// </summary>
	public class EmptyArrayCreateExpression : Expression
	{
		/// <summary>
		/// The expression used to indicate the size of the array
		/// </summary>
		public Expression SizeExpression;
	}

	/// <summary>
	/// Assignment expression.
	/// </summary>
	public class AssignmentExpression : Expression
	{
		/// <summary>
		/// The assignment operator
		/// </summary>
		public ICSharpCode.NRefactory.CSharp.AssignmentOperatorType Operator = ICSharpCode.NRefactory.CSharp.AssignmentOperatorType.Assign;
		/// <summary>
		/// The left-hand-side of the assignment
		/// </summary>
		public Expression Left;
		/// <summary>
		/// The right hand side of the assignment
		/// </summary>
		public Expression Right;
	}

	/// <summary>
	/// A binary operator expression.
	/// </summary>
	public class BinaryOperatorExpression : Expression
	{
		/// <summary>
		/// The operator being used
		/// </summary>
		public ICSharpCode.NRefactory.CSharp.BinaryOperatorType Operator;
		/// <summary>
		/// The left-hand-side of the operation
		/// </summary>
		public Expression Left;
		/// <summary>
		/// The left-hand-side of the operation
		/// </summary>
		public Expression Right;
	}

	/// <summary>
	/// A type case expression
	/// </summary>
	public class CastExpression : WrappingExpression
	{
	}

	/// <summary>
	/// A checked expression
	/// </summary>
	public class CheckedExpression : WrappingExpression
	{ 
	}

	/// <summary>
	/// A ternary conditional expression
	/// </summary>
	public class ConditionalExpression : Expression
	{
		/// <summary>
		/// The condition to evaluate
		/// </summary>
		public Expression ConditionExpression;
		/// <summary>
		/// The expression to use if the condition is true
		/// </summary>
		public Expression TrueExpression;
		/// <summary>
		/// The expression to use if the condition is false
		/// </summary>
		public Expression FalseExpression;
	}

	/// <summary>
	/// An empty expression.
	/// </summary>
	public class EmptyExpression : Expression
	{
	}

	/// <summary>
	/// An expression that uses an identifier
	/// </summary>
	public class IdentifierExpression : Expression
	{
		/// <summary>
		/// The item the identifier points to
		/// </summary>
		public DataElement Target;
	}

	/// <summary>
	/// An index expression
	/// </summary>
	public class IndexerExpression : Expression
	{
		/// <summary>
		/// The item being indexed
		/// </summary>
		public DataElement Target;
		/// <summary>
		/// The expression used to find the target
		/// </summary>
		public Expression TargetExpression;
		/// <summary>
		/// The index expression
		/// </summary>
		public Expression IndexExpression;
	}

	/// <summary>
	/// An expression that performs a function invocation
	/// </summary>
	public class InvocationExpression : Expression
	{
		/// <summary>
		/// The method being accessed
		/// </summary>
		public Method Target;
		/// <summary>
		/// The expression for the method
		/// </summary>
		public Expression TargetExpression;
		/// <summary>
		/// The expressions for the arguments
		/// </summary>
		public Expression[] ArgumentExpressions;
	}

	/// <summary>
	/// An expression targeting a member
	/// </summary>
	public class MemberReferenceExpression : Expression
	{
		/// <summary>
		/// The item being targeted
		/// </summary>
		public DataElement Target;
	}

	/// <summary>
	/// An expression targeting a member
	/// </summary>
	public class MethodReferenceExpression : Expression
	{
		/// <summary>
		/// The item being targeted
		/// </summary>
		public Method Target;
	}

	/// <summary>
	/// An expression that wraps another expression in parenthesis
	/// </summary>
	public class ParenthesizedExpression : WrappingExpression
	{
	}

	/// <summary>
	/// A primitive expression.
	/// </summary>
	public class PrimitiveExpression : Expression
	{
		public object Value;
	}

	/// <summary>
	/// A unary operator expression.
	/// </summary>
	public class UnaryOperatorExpression : Expression
	{
		/// <summary>
		/// The operator being applied
		/// </summary>
		public ICSharpCode.NRefactory.CSharp.UnaryOperatorType Operator;
		/// <summary>
		/// The expression the operand is applied to
		/// </summary>
		public Expression Operand;
	}

	/// <summary>
	/// An unchecked expression.
	/// </summary>
	public class UncheckedExpression : WrappingExpression
	{
	}

	/// <summary>
	/// A null reference expression.
	/// </summary>
	public class NullReferenceExpression : Expression
	{
	}
}
