using System;

namespace SME.AST
{
	/// <summary>
	/// A statement in the method body
	/// </summary>
	public abstract class Statement : ASTItem
	{
        
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

        /// <summary>
        /// Default constructor
        /// </summary>
        public ExpressionStatement()
        {
        }

        /// <summary>
        /// Creates a new statement for the expression
        /// </summary>
        /// <param name="expression">The expression to use.</param>
        public ExpressionStatement(Expression expression)
        {
            this.Expression = expression;
            this.Expression.Parent = this;
        }
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

        /// <summary>
        /// Default constructor
        /// </summary>
        public IfElseStatement()
        {
        }

        /// <summary>
        /// Constructs a new <see cref="IfElseStatement"/>
        /// </summary>
        /// <param name="condition">The condition expression.</param>
        /// <param name="trueStatement">The true statement.</param>
        /// <param name="falseStatement">The false statement.</param>
        public IfElseStatement(Expression condition, Statement trueStatement, Statement falseStatement)
        {
            this.Condition = condition;
            this.TrueStatement = trueStatement;
            this.FalseStatement = falseStatement;
            this.Condition.Parent = this;
            this.TrueStatement.Parent = this;
            this.FalseStatement.Parent = this;
        }
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

        /// <summary>
        /// The variables defined in the block scope
        /// </summary>
        public Variable[] Variables;

        /// <summary>
        /// Default constructor
        /// </summary>
        public BlockStatement()
        {
        }

        /// <summary>
        /// Constructs a new block statement
        /// </summary>
        /// <param name="statements">The statements in the block.</param>
        /// <param name="variables">The variables used in the block.</param>
        public BlockStatement(Statement[] statements, Variable[] variables)
        {
            this.Statements = statements;
            this.Variables = variables;
            foreach (var s in this.Statements ?? new Statement[0])
                s.Parent = this;
        }
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

        /// <summary>
        /// Default constructor
        /// </summary>
        public SwitchStatement()
        {
        }

        /// <summary>
        /// Constructs a new switch statement
        /// </summary>
        /// <param name="switchExpression">The expression to switch on.</param>
        /// <param name="cases">The cases in the statement.</param>
        public SwitchStatement(Expression switchExpression, Tuple<Expression[], Statement[]>[] cases)
        {
            this.SwitchExpression = switchExpression;
            this.Cases = cases;
            this.SwitchExpression.Parent = this;
            foreach (var c in cases ?? new Tuple<Expression[], Statement[]>[0])
            {
                foreach (var e in c.Item1 ?? new Expression[0])
                    e.Parent = this;
                foreach (var s in c.Item2 ?? new Statement[0])
                    s.Parent = this;
            }
        }
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

        /// <summary>
        /// Default constructor
        /// </summary>
        public ReturnStatement()
        {
        }

        /// <summary>
        /// Creates a new return statement
        /// </summary>
        /// <param name="expression">The expression to return.</param>
        public ReturnStatement(Expression expression)
        {
            this.ReturnExpression = expression;
            this.ReturnExpression.Parent = this;
        }
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

        /// <summary>
        /// Default constructor
        /// </summary>
        public ForStatement()
        {
        }

        /// <summary>
        /// Creates a new loop statement
        /// </summary>
        /// <param name="startValue">The start value.</param>
        /// <param name="endValue">The end value.</param>
        /// <param name="increment">The loop increment.</param>
        /// <param name="loopIndex">The loop index.</param>
        /// <param name="body">The loop body.</param>
        public ForStatement(Constant startValue, Constant endValue, Constant increment, Variable loopIndex, Statement body)
        {
            this.StartValue = startValue;
            this.EndValue = endValue;
            this.Increment = increment;
            this.LoopIndex = loopIndex;
            this.LoopBody = body;
            this.LoopBody.Parent = this;
        }
	}

	/// <summary>
	/// A break statement
	/// </summary>
	public class BreakStatement : Statement
	{
	}

    /// <summary>
    /// A goto statement
    /// </summary>
    public class GotoStatement : Statement
    {
        /// <summary>
        /// The target label
        /// </summary>
        public string Label;

        /// <summary>
        /// Default constructor
        /// </summary>
        public GotoStatement()
        {
        }

        /// <summary>
        /// Creates a new goto statement
        /// </summary>
        /// <param name="label">The label target.</param>
        public GotoStatement(string label)
        {
        }
    }

    /// <summary>
    /// A label statement
    /// </summary>
    public class LabelStatement : Statement
    {
        /// <summary>
        /// The label
        /// </summary>
        public string Label;
    }

	/// <summary>
	/// A statement used to output a comment
	/// </summary>
	public class CommentStatement : Statement
	{
        /// <summary>
        /// The comment message
        /// </summary>
        public string Message;

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
	}

}
