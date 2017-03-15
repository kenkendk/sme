using System;
using System.Collections.Generic;
using System.Linq;
using SME.AST;

namespace SME.VHDL.Transformations
{
	/// <summary>
	/// Handles cases where the loop increment is not one,
	/// by adding a temporary variable that holds the multiplied result,
	/// and updating the sub-tree to use that variable instead
	/// </summary>
	public class FixForLoopIncrements : IASTTransform
	{
		/// <summary>
		/// The render state.
		/// </summary>
		private readonly RenderState State;
		/// <summary>
		/// The method being transformed
		/// </summary>
		private readonly Method Method;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SME.VHDL.Transformations.FixForLoopIncrements"/> class.
		/// </summary>
		/// <param name="state">The render state.</param>
		/// <param name="method">The method being rendered.</param>
		public FixForLoopIncrements(RenderState state, Method method)
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
			var stm = item as AST.ForStatement;
			if (stm == null)
				return item;

			var incr = 1;
			var defincr = stm.Increment.DefaultValue;
			if (defincr is AST.Constant)
				incr = (int)((Constant)(defincr)).DefaultValue;
			else 			
				incr = (int)stm.Increment.DefaultValue;
			
			if (incr == 1)
				return item;

			var tmp = State.RegisterTemporaryVariable(Method, stm.LoopIndex.CecilType);
			State.TypeLookup[tmp] = VHDLTypes.INTEGER;

			// Find the first expression, so we can inject the assignment before it
			var firstexp = stm.LoopBody.All().OfType<Expression>().First();

			// Replace all the references
			foreach (var x in stm.All().OfType<Expression>())
			{
				var target = x.GetTarget();
				if (target == stm.LoopIndex)
					x.SetTarget(tmp);	
			}

			var exp = firstexp.SourceExpression;

			// Inject the assignment
			var nstm = new ExpressionStatement()
			{
				SourceStatement = stm.SourceStatement.Clone(),
				Expression = new AssignmentExpression()
				{
					Left = new IdentifierExpression()
					{
						Name = tmp.Name,
						Target = tmp,
						SourceExpression = exp,
						SourceResultType = tmp.CecilType
					},
					Operator = ICSharpCode.NRefactory.CSharp.AssignmentOperatorType.Assign,
					Right = new BinaryOperatorExpression()
					{
						Left = new IdentifierExpression()
						{
							Name = stm.LoopIndex.Name,
							Target = stm.LoopIndex,
							SourceExpression = exp,
							SourceResultType = stm.LoopIndex.CecilType
						},
						Operator = ICSharpCode.NRefactory.CSharp.BinaryOperatorType.Multiply,
						Right = new PrimitiveExpression()
						{
							Value = incr,
							SourceResultType = tmp.CecilType,
							SourceExpression = exp,
						},
						SourceExpression = exp,
						SourceResultType = tmp.CecilType
					},
					SourceExpression = exp,
					SourceResultType = tmp.CecilType
				}
			};

			nstm.UpdateParents();
			foreach (var x in nstm.All().OfType<Expression>())
				State.TypeLookup[x] = VHDLTypes.INTEGER;

			stm.LoopBody.PrependStatement(nstm);

			//Do not fix again
			stm.Increment = new Constant()
			{
				DefaultValue = 1,
				CecilType = tmp.CecilType,
				Source = stm,
				Parent = stm					
			};

			return nstm;
		}
	}
}
