using System;
using ICSharpCode.NRefactory.CSharp;

namespace SME.AST
{
	/// <summary>
	/// A statement in the method body
	/// </summary>
	public abstract class Statement : ASTItem
	{
		/// <summary>
		/// The source statement.
		/// </summary>
		public ICSharpCode.NRefactory.CSharp.Statement SourceStatement;
	}

	/// <summary>
	/// An expression statement
	/// </summary>
	public class ExpressionStatement : Statement
	{
		/// <summary>
		/// The expression inside the statement
		/// </summary>
		public Expression Expression;
	}

	/// <summary>
	/// A simple empty statement
	/// </summary>
	public class EmptyStatement : Statement
	{
	}

	/// <summary>
	/// An If/Else statement
	/// </summary>
	public class IfElseStatement : Statement
	{
		/// <summary>
		/// The expression to evaluate
		/// </summary>
		public Expression Condition;
		/// <summary>
		/// The statements to execute if the condition is true
		/// </summary>
		public Statement TrueStatement;
		/// <summary>
		/// The statements to execute if the condition is false
		/// </summary>
		public Statement FalseStatement;
	}

	/// <summary>
	/// A block statement
	/// </summary>
	public class BlockStatement : Statement
	{
		/// <summary>
		/// The statements in the block
		/// </summary>
		public Statement[] Statements;	
	}

	/// <summary>
	/// A switch statement
	/// </summary>
	public class SwitchStatement : Statement
	{
		/// <summary>
		/// The expression the switch is performed on
		/// </summary>
		public Expression SwitchExpression;

		/// <summary>
		/// The cases and labels for the statement
		/// </summary>
		public Tuple<Expression[], Statement[]>[] Cases;
	}

	/// <summary>
	/// A return statement
	/// </summary>
	public class ReturnStatement : Statement
	{
		/// <summary>
		/// The expression that should be evaluated to return this
		/// </summary>
		public Expression ReturnExpression;
	}

	/// <summary>
	/// A for statement
	/// </summary>
	public class ForStatement : Statement
	{
		/// <summary>
		/// The initial value for the loop index
		/// </summary>
		public Constant StartValue;
		/// <summary>
		/// The final value for the loop index
		/// </summary>
		public Constant EndValue;
		/// <summary>
		/// The increment for each loop iteration.
		/// </summary>
		public Constant Increment;
		/// <summary>
		/// The variable used to hold the loop value
		/// </summary>
		public Variable LoopIndex;

		/// <summary>
		/// The loop body content
		/// </summary>
		public Statement LoopBody;
	}

	/// <summary>
	/// A break statement
	/// </summary>
	public class BreakStatement : Statement
	{
	}

	/// <summary>
	/// A statement used to output a comment
	/// </summary>
	public class CommentStatement : Statement
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:SME.AST.CommentStatement"/> class.
		/// </summary>
		public CommentStatement() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SME.AST.CommentStatement"/> class.
		/// </summary>
		/// <param name="message">The message to set.</param>
		public CommentStatement(string message)
		{
			Message = message; 
		}

		/// <summary>
		/// The comment message
		/// </summary>
		public string Message;
	}

}
