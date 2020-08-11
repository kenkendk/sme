using System;
using System.Linq;

namespace SME.AST
{
    /// <summary>
    /// A statement in the method body.
    /// </summary>
    public abstract class Statement : ASTItem
    {

    }

    /// <summary>
    /// An expression statement.
    /// </summary>
    public class ExpressionStatement : Statement
    {
        /// <summary>
        /// The expression inside the statement.
        /// </summary>
        public Expression Expression;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ExpressionStatement()
        {
        }

        /// <summary>
        /// Creates a new statement for the expression.
        /// </summary>
        /// <param name="expression">The expression to use.</param>
        public ExpressionStatement(Expression expression)
        {
            this.Expression = expression;
            this.Expression.Parent = this;
        }
    }

    /// <summary>
    /// A simple empty statement.
    /// </summary>
    public class EmptyStatement : Statement
    {
    }

    /// <summary>
    /// An If/Else statement.
    /// </summary>
    public class IfElseStatement : Statement
    {
        /// <summary>
        /// The expression to evaluate.
        /// </summary>
        public Expression Condition;
        /// <summary>
        /// The statements to execute if the condition is true.
        /// </summary>
        public Statement TrueStatement;
        /// <summary>
        /// The statements to execute if the condition is false.
        /// </summary>
        public Statement FalseStatement;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public IfElseStatement()
        {
        }

        /// <summary>
        /// Constructs a new <see cref="IfElseStatement"/>.
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
    /// A block statement.
    /// </summary>
    public class BlockStatement : Statement
    {
        /// <summary>
        /// The statements in the block.
        /// </summary>
        public Statement[] Statements;

        /// <summary>
        /// The variables defined in the block scope.
        /// </summary>
        public Variable[] Variables;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public BlockStatement()
        {
        }

        /// <summary>
        /// Constructs a new block statement.
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
    /// A switch statement.
    /// </summary>
    public class SwitchStatement : Statement
    {
        /// <summary>
        /// The expression the switch is performed on.
        /// </summary>
        public Expression SwitchExpression;

        /// <summary>
        /// The cases and labels for the statement.
        /// </summary>
        public Tuple<Expression[], Statement[]>[] Cases;

        /// <summary>
        /// States whether or not the SwitchStatement contains a default: case.
        /// </summary>
        public bool HasDefault;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SwitchStatement()
        {
        }

        /// <summary>
        /// Constructs a new switch statement.
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
    /// A return statement.
    /// </summary>
    public class ReturnStatement : Statement
    {
        /// <summary>
        /// The expression that should be evaluated to return this.
        /// </summary>
        public Expression ReturnExpression;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ReturnStatement()
        {
        }

        /// <summary>
        /// Creates a new return statement.
        /// </summary>
        /// <param name="expression">The expression to return.</param>
        public ReturnStatement(Expression expression)
        {
            this.ReturnExpression = expression;
            this.ReturnExpression.Parent = this;
        }
    }

    /// <summary>
    /// A for statement.
    /// </summary>
    public class ForStatement : Statement
    {
        /// <summary>
        /// The initialization expression for the loop index.
        /// </summary>
        public Expression Initializer;
        /// <summary>
        /// The final value for the loop index.
        /// </summary>
        public Expression Condition;
        /// <summary>
        /// The increment for each loop iteration.
        /// </summary>
        public Expression Increment;
        /// <summary>
        /// The variable used to hold the loop value.
        /// </summary>
        public Variable LoopIndex;

        /// <summary>
        /// The loop body content.
        /// </summary>
        public Statement LoopBody;

        /// <summary>
        /// A variable indicating if the for loop has a statically known size.
        /// </summary>
        public bool HasStaticSize;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ForStatement()
        {
        }

        /// <summary>
        /// Creates a new loop statement.
        /// </summary>
        /// <param name="initializer">The initial expression.</param>
        /// <param name="condition">The condition expression value.</param>
        /// <param name="increment">The loop increment expression.</param>
        /// <param name="loopIndex">The loop index.</param>
        /// <param name="body">The loop body.</param>
        public ForStatement(Expression initializer, Expression condition, Expression increment, Variable loopIndex, Statement body)
        {
            this.Initializer = initializer;
            this.Condition = condition;
            this.Increment = increment;
            this.LoopIndex = loopIndex;
            this.LoopBody = body;
            this.LoopBody.Parent = this;
            this.HasStaticSize =
                new[] { initializer, condition, increment }
                .Count(x => (x as PrimitiveExpression)?.GetTarget() is Constant) == 3;
        }
    }

    /// <summary>
    /// A break statement.
    /// </summary>
    public class BreakStatement : Statement
    {
    }

    /// <summary>
    /// A goto statement.
    /// </summary>
    public class GotoStatement : Statement
    {
        /// <summary>
        /// The target label.
        /// </summary>
        public string Label;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public GotoStatement()
        {
        }

        /// <summary>
        /// Creates a new goto statement.
        /// </summary>
        /// <param name="label">The label target.</param>
        public GotoStatement(string label)
        {
        }
    }

    /// <summary>
    /// A label statement.
    /// </summary>
    public class LabelStatement : Statement
    {
        /// <summary>
        /// The label.
        /// </summary>
        public string Label;
    }

    /// <summary>
    /// A statement used to output a comment.
    /// </summary>
    public class CommentStatement : Statement
    {
        /// <summary>
        /// The comment message.
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

    /// <summary>
    /// A while statement.
    /// </summary>
    public class WhileStatement : Statement
    {
        /// <summary>
        /// The while condition expression.
        /// </summary>
        public Expression Condition;

        /// <summary>
        /// The while loop body statement.
        /// </summary>
        public Statement Body;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SME.AST.WhileStatement"/> class.
        /// </summary>
        public WhileStatement() { }


        /// <summary>
        /// Initializes a new instance of the <see cref="T:SME.AST.WhileStatement"/> class.
        /// </summary>
        /// <param name="condition">The while condition.</param>
        /// <param name="body">The while loop body.</param>
        public WhileStatement(Expression condition, Statement body)
        {
            Condition = condition;
            Body = body;
            if (Condition != null)
                Condition.Parent = this;
            if (Body != null)
                Body.Parent = this;
        }
    }
}
