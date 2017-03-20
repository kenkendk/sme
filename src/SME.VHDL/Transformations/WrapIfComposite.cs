using System;
using System.Linq;
using SME.AST;

namespace SME.VHDL.Transformations
{
	/// <summary>
	/// Puts a parenthesized expression around an expression that cannot be non-wrapped in VHDL
	/// </summary>
	public class WrapIfComposite : IASTTransform
	{
		/// <summary>
		/// The types that do not have to be wrapped
		/// </summary>
		private static readonly Type[] SIMPLE_TYPES = new [] {
			typeof(IndexerExpression),
			typeof(MemberReferenceExpression),
			typeof(MethodReferenceExpression),
			typeof(PrimitiveExpression),
			typeof(IdentifierExpression),
			typeof(IndexerExpression),
			typeof(InvocationExpression),
			typeof(ParenthesizedExpression),
			typeof(CastExpression),
			typeof(EmptyExpression),
			typeof(CustomNodes.ConversionExpression)
		};

		/// <summary>
		/// Applies the transformation
		/// </summary>
		/// <returns>The transformed item.</returns>
		/// <param name="item">The item to visit.</param>
		public ASTItem Transform(ASTItem item)
		{
			var exp = item as Expression;
			if (exp == null)
				return item;
			
			// If this is top-level, we do not want a wrapping
			if (exp.Parent is Statement || exp.Parent is ParenthesizedExpression || exp.Parent is CustomNodes.ConversionExpression)
				return item;

			if (!SIMPLE_TYPES.Any(x => exp.GetType().IsAssignableFrom(x)) && !(exp.Parent is AssignmentExpression))
			{
				var np = new ParenthesizedExpression()
				{
					Expression = exp,
					Parent = exp.Parent,
					Name = exp.Name,
					SourceExpression = exp.SourceExpression,
					SourceResultType = exp.SourceResultType
				};

				exp.ReplaceWith(np);
				exp.Parent = np;

				return np;
			}

			return item;
		}
	}
}
