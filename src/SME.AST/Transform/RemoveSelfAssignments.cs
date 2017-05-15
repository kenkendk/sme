using System;
using SME.AST;

namespace SME.AST.Transform
{
	/// <summary>
	/// Remove self assignments, such as &quot;x = x&quot;, which can be emitted by the C# compiler.
	/// </summary>
	public class RemoveSelfAssignments : IASTTransform
	{
		/// <summary>
		/// Applies the transformation
		/// </summary>
		/// <returns>The transformed item.</returns>
		/// <param name="item">The item to visit.</param>
		public virtual ASTItem Transform(ASTItem item)
		{
			var expression = item as AssignmentExpression;

			if (expression == null)
				return item;

			var target_left = expression.Left.GetTarget();
			var target_right = expression.Right.GetTarget();

			if (target_left == target_right && target_left != null && expression.Parent is ExpressionStatement && expression.Operator == ICSharpCode.NRefactory.CSharp.AssignmentOperatorType.Assign)
				return expression.ReplaceWith(new EmptyExpression()
				{
					SourceExpression = expression.SourceExpression,
				});

			return item;
		}
	}
}
