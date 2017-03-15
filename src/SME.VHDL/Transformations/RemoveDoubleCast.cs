using System;
using SME.AST;

namespace SME.VHDL.Transformations
{
	/// <summary>
	/// Removes double cast expression
	/// </summary>
	public class RemoveDoubleCast : IASTTransform
	{
		/// <summary>
		/// Applies the transformation
		/// </summary>
		/// <returns>The transformed item.</returns>
		/// <param name="item">The item to visit.</param>
		public ASTItem Transform(ASTItem item)
		{
			// This fixes a case where the code has double castings that introduce unwanted parenthesis 
			var self = item as CastExpression;
			if (self == null)
				return item;

			var child = self.Expression;

			if (child is CastExpression && child.SourceResultType.IsSameTypeReference(self.SourceResultType))
			{
				self.Expression = ((CastExpression)child).Expression;
				self.Expression.Parent = self;

				return null;
			}

			return item;
		}
	}
}
