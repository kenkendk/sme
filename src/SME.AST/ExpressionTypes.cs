using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SME.AST
{
    /// <summary>
    /// An expression found in a statement.
    /// </summary>
    public abstract class Expression : ASTItem
    {
        /// <summary>
        /// The source expression used in the statement.
        /// </summary>
        public ExpressionSyntax SourceExpression;

        /// <summary>
        /// The source result type.
        /// </summary>
        public ITypeSymbol SourceResultType;

        /// <summary>
        /// Returns a string representation of the Expression.
        /// </summary>
        public abstract override string ToString();
    }

    /// <summary>
    /// Base class for custom defined expressions that can enter into a the normal tree.
    /// </summary>
    public abstract class CustomExpression : Expression
    {
        /// <summary>
        /// Visits the element, optionally with a visitor callback.
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
        /// List the children of this element.
        /// </summary>
        public abstract Expression[] Children { get; set; }

        /// <summary>
        /// Gets the target for the item or null.
        /// </summary>
        public abstract DataElement GetTarget();

        /// <summary>
        /// Returns a string representation of the Expression.
        /// </summary>
        public abstract override string ToString();
    }

    /// <summary>
    /// Base class for an expression that wraps another expression.
    /// </summary>
    public abstract class WrappingExpression : Expression
    {
        /// <summary>
        /// The wrapped expression.
        /// </summary>
        public Expression Expression;

        /// <summary>
        /// Returns a string representation of the Expression.
        /// </summary>
        public override string ToString()
        {
            return Expression.ToString();
        }
    }

    /// <summary>
    /// An expression that creates an array.
    /// </summary>
    public class ArrayCreateExpression : Expression
    {
        /// <summary>
        /// The elements in the array.
        /// </summary>
        public Expression[] ElementExpressions;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ArrayCreateExpression()
        {
        }

        /// <summary>
        /// Specialized helper constructor.
        /// </summary>
        /// <param name="ElementExpressions">The elements to assign.</param>
        public ArrayCreateExpression(Expression[] ElementExpressions)
        {
            this.ElementExpressions = ElementExpressions;
            foreach (var e in this.ElementExpressions ?? new Expression[0])
                e.Parent = this;
        }

        /// <summary>
        /// Returns a string representation of the Expression.
        /// </summary>
        public override string ToString()
        {
            string tmp = "";
            for (int i = 0; i < ElementExpressions.Length; i++)
            {
                tmp += i > 0 ? ", " : "";
                tmp += ElementExpressions[i].ToString();
            }
            return $"[{tmp}]";
        }
    }

    /// <summary>
    /// An expression that indicates that the array is created with default values.
    /// </summary>
    public class EmptyArrayCreateExpression : Expression
    {
        /// <summary>
        /// The expression used to indicate the size of the array.
        /// </summary>
        public Expression SizeExpression;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public EmptyArrayCreateExpression()
        {
        }

        /// <summary>
        /// Specialized helper constructor.
        /// </summary>
        /// <param name="SizeExpression">The expression that gives the size of the empty array.</param>
        public EmptyArrayCreateExpression(Expression SizeExpression)
        {
            this.SizeExpression = SizeExpression;
            this.SizeExpression.Parent = this;
        }

        /// <summary>
        /// Returns a string representation of the Expression.
        /// </summary>
        public override string ToString()
        {
            return $"[{SizeExpression.ToString()}]";
        }
    }


    /// <summary>
    /// Assignment expression.
    /// </summary>
    public class AssignmentExpression : Expression
    {
        /// <summary>
        /// The assignment operator.
        /// </summary>
        public SyntaxKind Operator = SyntaxKind.EqualsToken;
        /// <summary>
        /// The left-hand-side of the assignment.
        /// </summary>
        public Expression Left;
        /// <summary>
        /// The right hand side of the assignment.
        /// </summary>
        public Expression Right;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public AssignmentExpression()
        {
        }

        /// <summary>
        /// Specialized helper constructor.
        /// </summary>
        /// <param name="left">The left-hand-side expression.</param>
        /// <param name="op">The operator to use.</param>
        /// <param name="right">The right-hand-size expression.</param>
        public AssignmentExpression(Expression left, SyntaxKind op, Expression right)
        {
            this.Left = left;
            this.Operator = op;
            this.Right = right;

            this.SourceResultType = this.Left.SourceResultType;

            this.Left.Parent = this;
            this.Right.Parent = this;
        }

        /// <summary>
        /// Specialized helper constructor.
        /// </summary>
        /// <param name="left">The left-hand-side expression.</param>
        /// <param name="right">The right-hand-size expression.</param>
        public AssignmentExpression(Expression left, Expression right)
            : this(left, SyntaxKind.EqualsToken, right)
        {
        }

        /// <summary>
        /// Returns a string representation of the Expression.
        /// </summary>
        public override string ToString()
        {
            return $"{Left.ToString()} = {Right.ToString()}";
        }
    }

    /// <summary>
    /// A binary operator expression.
    /// </summary>
    public class BinaryOperatorExpression : Expression
    {
        /// <summary>
        /// The operator being used.
        /// </summary>
        public SyntaxKind Operator;
        /// <summary>
        /// The left-hand-side of the operation.
        /// </summary>
        public Expression Left;
        /// <summary>
        /// The left-hand-side of the operation.
        /// </summary>
        public Expression Right;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public BinaryOperatorExpression()
        {
        }

        /// <summary>
        /// Specialized helper constructor.
        /// </summary>
        /// <param name="left">The left-hand-side expression.</param>
        /// <param name="op">The operator to use.</param>
        /// <param name="right">The right-hand-size expression.</param>
        public BinaryOperatorExpression(Expression left, SyntaxKind op, Expression right)
        {
            this.Left = left;
            this.Operator = op;
            this.Right = right;

            this.Left.Parent = this;
            this.Right.Parent = this;
        }

        /// <summary>
        /// Returns a string representation of the Expression.
        /// </summary>
        public override string ToString()
        {
            return $"{Left.ToString()} {Operator.ToString()} {Right.ToString()}";
        }
    }

    /// <summary>
    /// A type case expression.
    /// </summary>
    public class CastExpression : WrappingExpression
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public CastExpression()
        {
        }

        /// <summary>
        /// Specialized helper constructor.
        /// </summary>
        /// <param name="source">The cast source.</param>
        public CastExpression(Expression source)
        {
            this.Expression = source;
            this.SourceResultType = this.Expression.SourceResultType;
            this.Expression.Parent = this;
        }

        /// TODO Fix the ToString() methods. There should be a better method for correctly displaying values in the debugger.
        /// <summary>
        /// Returns a string representation of the Expression.
        /// </summary>
        public override string ToString()
        {
            return $"({SourceResultType.ToString()}){Expression.ToString()}";
        }
    }

    /// <summary>
    /// A checked expression.
    /// </summary>
    public class CheckedExpression : WrappingExpression
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public CheckedExpression()
        {
        }

        /// <summary>
        /// Specialized helper constructor.
        /// </summary>
        /// <param name="source">The checked source.</param>
        public CheckedExpression(Expression source)
        {
            this.Expression = source;
            this.SourceResultType = this.Expression.SourceResultType;
            this.Expression.Parent = this;
        }

        /// <summary>
        /// Returns a string representation of the Expression.
        /// </summary>
        public override string ToString()
        {
            return $"checked({Expression.ToString()})";
        }
    }

    /// <summary>
    /// A ternary conditional expression.
    /// </summary>
    public class ConditionalExpression : Expression
    {
        /// <summary>
        /// The condition to evaluate.
        /// </summary>
        public Expression ConditionExpression;
        /// <summary>
        /// The expression to use if the condition is true.
        /// </summary>
        public Expression TrueExpression;
        /// <summary>
        /// The expression to use if the condition is false.
        /// </summary>
        public Expression FalseExpression;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ConditionalExpression()
        {
        }

        /// <summary>
        /// Specialized helper constructor.
        /// </summary>
        /// <param name="condition">The condition expression.</param>
        /// <param name="trueExpression">The truth expression.</param>
        /// <param name="falseExpression">The false expression.</param>
        public ConditionalExpression(Expression condition, Expression trueExpression, Expression falseExpression)
        {
            this.ConditionExpression = condition;
            this.TrueExpression = trueExpression;
            this.FalseExpression = falseExpression;
            this.SourceResultType = trueExpression is EmptyExpression ? falseExpression.SourceResultType : trueExpression.SourceResultType;
        }

        /// <summary>
        /// Returns a string representation of the Expression.
        /// </summary>
        public override string ToString()
        {
            return $"{ConditionExpression.ToString()} ? {TrueExpression.ToString()} : {FalseExpression.ToString()}";
        }
    }

    /// <summary>
    /// An empty expression.
    /// </summary>
    public partial class EmptyExpression : Expression
    {
        /// <summary>
        /// Returns a string representation of the Expression.
        /// </summary>
        public override string ToString()
        {
            return "";
        }
    }

    /// <summary>
    /// An expression that uses an identifier.
    /// </summary>
    public partial class IdentifierExpression : Expression
    {
        /// <summary>
        /// The item the identifier points to.
        /// </summary>
        public DataElement Target;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public IdentifierExpression()
        {
        }

        /// <summary>
        /// Specialized helper constructor.
        /// </summary>
        /// <param name="target">The item the identifier resolves to.</param>
        public IdentifierExpression(DataElement target)
        {
            this.Target = target;
            this.SourceResultType = target.MSCAType;
            this.Name = target.Name;
        }

        /// <summary>
        /// Returns a string representation of the Expression.
        /// </summary>
        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>
    /// An index expression.
    /// </summary>
    public class IndexerExpression : Expression
    {
        /// <summary>
        /// The item being indexed.
        /// </summary>
        public DataElement Target;
        /// <summary>
        /// The expression used to find the target.
        /// </summary>
        public Expression TargetExpression;
        /// <summary>
        /// The index expression.
        /// </summary>
        public Expression IndexExpression;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public IndexerExpression()
        {
        }

        /// <summary>
        /// Specialized helper constructor.
        /// </summary>
        /// <param name="target">The element that is being indexed.</param>
        /// <param name="targetExpression">The expression that is being indexed.</param>
        /// <param name="indexExpression">The expression that computes the index.</param>
        public IndexerExpression(DataElement target, Expression targetExpression, Expression indexExpression)
        {
            this.Target = target;
            this.TargetExpression = targetExpression;
            this.IndexExpression = indexExpression;
            this.SourceResultType = targetExpression.SourceResultType.GetArrayElementType();
            this.TargetExpression.Parent = this;
            this.IndexExpression.Parent = this;
        }

        /// <summary>
        /// Returns a string representation of the Expression.
        /// </summary>
        public override string ToString()
        {
            return SourceExpression.ToString();
        }
    }

    /// <summary>
    /// An expression that performs a function invocation.
    /// </summary>
    public class InvocationExpression : Expression
    {
        /// <summary>
        /// The method being accessed.
        /// </summary>
        public Method Target;
        /// <summary>
        /// The expression for the method.
        /// </summary>
        public Expression TargetExpression;
        /// <summary>
        /// The expressions for the arguments.
        /// </summary>
        public Expression[] ArgumentExpressions;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public InvocationExpression()
        {
        }

        /// <summary>
        /// Specialized helper constructor.
        /// </summary>
        /// <param name="target">The method being invoked.</param>
        /// <param name="targetExpression">The expression for the target method.</param>
        /// <param name="argumentExpressions">The arguments to the method invocation.</param>
        public InvocationExpression(Method target, Expression targetExpression, params Expression[] argumentExpressions)
        {
            this.Target = target;
            this.TargetExpression = targetExpression;
            this.ArgumentExpressions = argumentExpressions;
            if (target.MSCAReturnType == null)
                this.SourceResultType = target.ReturnVariable == null ? target.GetNearestParent<Process>().MSCAType.LoadType(typeof(void)) : target.ReturnVariable.MSCAType;
            else
                this.SourceResultType = target.MSCAReturnType;

            this.Target.Parent = this;
            foreach (var e in this.ArgumentExpressions ?? new Expression[0])
                e.Parent = this;
        }

        /// <summary>
        /// Returns a string representation of the Expression.
        /// </summary>
        public override string ToString()
        {
            var args = "";
            for (int i = 0; i < ArgumentExpressions.Length; i++)
            {
                args += i > 0 ? ", " : "";
                args += ArgumentExpressions[i].ToString();
            }
            return $"{Target.Name}({args})";
        }
    }

    /// <summary>
    /// An expression targeting a member. The format is "target.member".
    /// </summary>
    public class MemberReferenceExpression : Expression
    {
        /// <summary>
        /// The item being targeted.
        /// </summary>
        public DataElement Target;
        /// <summary>
        /// The expression for the target.
        /// </summary>
        public Expression TargetExpression;
        /// <summary>
        /// The expression for the member.
        /// </summary>
        public Expression MemberExpression;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public MemberReferenceExpression()
        {
        }

        /// <summary>
        /// Specialized helper constructor.
        /// </summary>
        /// <param name="target">The member that is being referenced.</param>
        public MemberReferenceExpression(DataElement target, Expression targetExpression, Expression memberExpression)
        {
            this.Target = target;
            this.SourceResultType = target.MSCAType;
            this.TargetExpression = targetExpression;
            this.MemberExpression = memberExpression;
        }

        /// <summary>
        /// Returns a string representation of the Expression.
        /// </summary>
        public override string ToString()
        {
            return SourceExpression.ToString();
        }
    }

    /// <summary>
    /// An expression targeting a member.
    /// </summary>
    public partial class MethodReferenceExpression : Expression
    {
        /// <summary>
        /// The item being targeted.
        /// </summary>
        public Method Target;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public MethodReferenceExpression()
        {
        }

        /// <summary>
        /// Specialized helper constructor.
        /// </summary>
        /// <param name="target">The method that is being referenced.</param>
        public MethodReferenceExpression(Method target)
        {
            this.Target = target;
            if (target.MSCAReturnType == null)
                this.SourceResultType = target.ReturnVariable == null ? target.GetNearestParent<Process>().MSCAType.LoadType(typeof(void)) : target.ReturnVariable.MSCAType;
            else
                this.SourceResultType = target.MSCAReturnType;
        }

        /// <summary>
        /// Returns a string representation of the Expression.
        /// </summary>
        public override string ToString()
        {
            return Target.Name;
        }
    }

    /// <summary>
    /// An expression that wraps another expression in parenthesis.
    /// </summary>
    public class ParenthesizedExpression : WrappingExpression
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public ParenthesizedExpression()
        {
        }

        /// <summary>
        /// Specialized helper constructor.
        /// </summary>
        /// <param name="target">The expression inside the parenthesis.</param>
        public ParenthesizedExpression(Expression target)
        {
            this.Expression = target;
            this.SourceResultType = this.Expression.SourceResultType;
            this.Expression.Parent = this;
        }

        /// <summary>
        /// Returns a string representation of the Expression.
        /// </summary>
        public override string ToString()
        {
            return $"({Expression.ToString()})";
        }
    }

    /// <summary>
    /// A primitive expression.
    /// </summary>
    public class PrimitiveExpression : Expression
    {
        /// <summary>
        /// The object in the primitive expression.
        /// </summary>
        public object Value;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public PrimitiveExpression()
        {
        }

        /// <summary>
        /// Specialized helper constructor.
        /// </summary>
        /// <param name="value">The item to use as the value.</param>
        /// <param name="sourcetype">The data element type.</param>
        public PrimitiveExpression(object value, ITypeSymbol sourcetype)
        {
            this.Value = value;
            this.SourceResultType = sourcetype;
            if (sourcetype == null)
                throw new ArgumentNullException(nameof(sourcetype));
            this.Name = value.ToString();
        }

        /// <summary>
        /// Returns a string representation of the Expression.
        /// </summary>
        public override string ToString()
        {
            return Value.ToString();
        }
    }

    /// <summary>
    /// A unary operator expression.
    /// </summary>
    public class UnaryOperatorExpression : Expression
    {
        /// <summary>
        /// The operator being applied.
        /// </summary>
        public SyntaxKind Operator;
        /// <summary>
        /// The expression the operand is applied to.
        /// </summary>
        public Expression Operand;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public UnaryOperatorExpression()
        {
        }

        /// <summary>
        /// Specialized helper constructor.
        /// </summary>
        /// <param name="op">The operation to apply.</param>
        /// <param name="operand">The operand to apply the operation to.</param>
        public UnaryOperatorExpression(SyntaxKind op, Expression operand)
        {
            this.Operator = op;
            this.Operand = operand;
            this.SourceResultType = this.Operand.SourceResultType;
            this.Operand.Parent = this;
        }

        /// <summary>
        /// Returns a string representation of the Expression.
        /// </summary>
        public override string ToString()
        {
            return $"{Operator.ToString()}{Operand.ToString()}";
        }
    }

    /// <summary>
    /// An expression decorated with a direction specifier.
    /// </summary>
    public class DirectionExpression : Expression
    {
        /// <summary>
        /// Enum type for the different directions a field can have.
        /// </summary>
        public enum FieldDirection
        {
            _in, _out, _ref
        }

        /// <summary>
        /// The internal expression
        /// </summary>
        public Expression Expression;

        /// <summary>
        /// The direction of the expression.
        /// </summary>
        public FieldDirection direction;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public DirectionExpression() { }

        /// <summary>
        /// Specialized helper constructor.
        /// </summary>
        public DirectionExpression(Expression target)
        {
            this.Expression = target;
            this.SourceResultType = this.Expression.SourceResultType;
            this.Expression.Parent = this;
        }

        /// <summary>
        /// Returns a string representation of the expression.
        /// </summary>
        public override string ToString()
        {
            return $"{direction} {Expression.ToString()}";
        }
    }

    /// <summary>
    /// An unchecked expression.
    /// </summary>
    public class UncheckedExpression : WrappingExpression
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public UncheckedExpression()
        {
        }

        /// <summary>
        /// Specialized helper constructor.
        /// </summary>
        /// <param name="target">The expression being performed unchecked.</param>
        public UncheckedExpression(Expression target)
        {
            this.Expression = target;
            this.SourceResultType = this.Expression.SourceResultType;
            this.Expression.Parent = this;
        }

        /// <summary>
        /// Returns a string representation of the Expression.
        /// </summary>
        public override string ToString()
        {
            return $"unchecked({Expression.ToString()})";
        }
    }

    /// <summary>
    /// A null reference expression.
    /// </summary>
    public class NullReferenceExpression : Expression
    {
        /// <summary>
        /// Returns a string representation of the Expression.
        /// </summary>
        public override string ToString()
        {
            return "null";
        }
    }

    /// <summary>
    /// An await statement for clock or condition.
    /// </summary>
    public class AwaitExpression : Expression
    {
        /// <summary>
        /// The expression to wait for or null if we are awaiting the clock.
        /// </summary>
        public Expression Expression;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SME.AST.AwaitStatement"/> class.
        /// </summary>
        public AwaitExpression() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SME.AST.AwaitExpression"/> class.
        /// </summary>
        /// <param name="expression">The expression to await.</param>
        public AwaitExpression(Expression expression)
        {
            Expression = expression;
        }

        /// <summary>
        /// Returns a string representation of the Expression.
        /// </summary>
        public override string ToString()
        {
            return $"await {Expression.ToString()}";
        }
    }
}
