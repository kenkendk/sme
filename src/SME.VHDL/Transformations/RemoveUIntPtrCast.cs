using System;
using SME.AST;

namespace SME.VHDL.Transformations
{
	/// <summary>
	/// Removes <seealso cref="UIntPtr"/> casts injected by NRefactory
	/// </summary>
	public class RemoveUIntPtrCast : IASTTransform
	{
		/// <summary>
		/// Applies the transformation
		/// </summary>
		/// <returns>The transformed item.</returns>
		/// <param name="_item">The item to visit.</param>
		public ASTItem Transform(ASTItem _item)
		{
			var item = _item as Expression;
			if (item == null)
				return _item;

			// This fixes a case where the NRefactory code injects a cast to UIntPtr, 
			// even though none is present in the code, nor the IL 

			var wrap = false;
			var self = item;
			if (self is ParenthesizedExpression)
			{
				self = (self as ParenthesizedExpression).Expression;
				wrap = true;
			}

			if (self is CastExpression && (self as CastExpression).SourceResultType.IsSameTypeReference(typeof(UIntPtr)))
				self = (self as CastExpression).Expression;
			else
				self = item;

			if (wrap && self != item)
			{
				var p = self;
				self = new ParenthesizedExpression()
				{
					Expression = p as Expression,
					SourceExpression = (p as Expression).SourceExpression,
					SourceResultType = (p as Expression).SourceResultType,
					Parent = p.Parent,
				};

				p.Parent = self;
			}

			if (self != item)
				return item.ReplaceWith(self);

			return item;
		}
	}
}
