using System;
using System.Collections.Generic;
using SME.AST;

namespace SME.VHDL.Transformations
{
	/// <summary>
	/// Injects assignment statements before return statements
	/// </summary>
	public class InsertReturnAssignments : IASTTransform
	{
		/// <summary>
		/// The render state
		/// </summary>
		private readonly RenderState State;
		/// <summary>
		/// The method being compiled
		/// </summary>
		private readonly Method Method;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SME.VHDL.Transformations.InjectTypeConversions"/> class.
		/// </summary>
		/// <param name="state">The render state.</param>
		/// <param name="method">The method being rendered.</param>
		public InsertReturnAssignments(RenderState state, Method method)
		{
			State = state;
			Method = method;
		}

		/// <summary>
		/// Applies the transformation
		/// </summary>
		/// <returns>The transformed item.</returns>
		/// <param name="item">The item to visit.</param>
		public ASTItem Transform(ASTItem item)
		{
			var rs = item as AST.ReturnStatement;
			if (rs == null)
				return item;

			if (rs.ReturnExpression is EmptyExpression)
				return item;

			var stm = new ExpressionStatement()
			{
				SourceStatement = rs.SourceStatement.Clone(),
				Expression = new AST.AssignmentExpression()
				{
					Left = new AST.MemberReferenceExpression()
					{
						Target = Method.ReturnVariable,
						Name = Method.ReturnVariable.Name,
						SourceExpression = rs.ReturnExpression.SourceExpression,
						SourceResultType = Method.ReturnVariable.CecilType
					},
					Right = rs.ReturnExpression,
					SourceExpression = rs.ReturnExpression.SourceExpression,
					SourceResultType = Method.ReturnVariable.CecilType,
					Operator = ICSharpCode.NRefactory.CSharp.AssignmentOperatorType.Assign
				}
			};

			rs.PrependStatement(stm);
			stm.UpdateParents();

			rs.ReplaceWith(new ReturnStatement()
			{
				ReturnExpression = new EmptyExpression() {
					SourceExpression = rs.ReturnExpression.SourceExpression,
					SourceResultType = Method.ReturnVariable.CecilType.LoadType(typeof(void))
				},
				SourceStatement = rs.SourceStatement.Clone()
			});

			return stm;
		}
	}
}
