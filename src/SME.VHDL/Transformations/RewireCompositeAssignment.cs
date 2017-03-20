using System;
using ICSharpCode.NRefactory.CSharp;
using SME.AST;

namespace SME.VHDL.Transformations
{
	/// <summary>
	/// This transformation fixes assignments using a composite assignment,
	/// such as &quot;a += 4&quot;
	/// </summary>
	public class RewireCompositeAssignment : IASTTransform
	{
		/// <summary>
		/// Applies the transformation
		/// </summary>
		/// <returns>The transformed item.</returns>
		/// <param name="el">The item to visit.</param>
		public ASTItem Transform(ASTItem el)
		{
			if (el is AST.AssignmentExpression)
			{
				var ase = ((AST.AssignmentExpression)el);
				if (ase.Operator != AssignmentOperatorType.Assign)
				{
					AST.Expression clonedleft = ase.Left.Clone();

					var newop = new AST.BinaryOperatorExpression()
					{
						Operator = ase.Operator.ToBinaryOperator(),
						Left = clonedleft,
						Right = ase.Right,
						Parent = ase,
						SourceExpression = ase.SourceExpression,
						SourceResultType = ase.SourceResultType
					};

					newop.Left.Parent = newop.Right.Parent = newop;
					ase.Operator = AssignmentOperatorType.Assign;
					ase.Right = newop;

					return null;
				}
			}

			return el;
		}
	}
}
