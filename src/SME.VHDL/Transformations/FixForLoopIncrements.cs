using System;
using System.Collections.Generic;
using System.Linq;
using SME.AST;
using SME.AST.Transform;

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
        /// Cache of already processed statements
        /// </summary>
        private readonly HashSet<AST.ForStatement> m_processed = new HashSet<ForStatement>();

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

            if (m_processed.Contains(stm))
                return item;
            m_processed.Add(stm);

            Tuple<int, int, int> loopedges = null;
            try
            {
                loopedges = stm.GetStaticForLoopValues();
            }
            catch
            {
                return item;
            }

            var incr = loopedges.Item3;			
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
				Expression = new AssignmentExpression()
				{
					Left = new IdentifierExpression()
					{
						Name = tmp.Name,
						Target = tmp,
						SourceExpression = exp,
						SourceResultType = tmp.CecilType
					},
					Operator = ICSharpCode.Decompiler.CSharp.Syntax.AssignmentOperatorType.Assign,
					Right = new BinaryOperatorExpression()
					{
						Left = new IdentifierExpression()
						{
							Name = stm.LoopIndex.Name,
							Target = stm.LoopIndex,
							SourceExpression = exp,
							SourceResultType = stm.LoopIndex.CecilType
						},
						Operator = ICSharpCode.Decompiler.CSharp.Syntax.BinaryOperatorType.Multiply,
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
            stm.Increment = new AssignmentExpression(
                new IdentifierExpression(stm.LoopIndex),
                new BinaryOperatorExpression(
                    new IdentifierExpression(stm.LoopIndex),
                    ICSharpCode.Decompiler.CSharp.Syntax.BinaryOperatorType.Add,
                    new PrimitiveExpression(1, tmp.CecilType)
                )
                { SourceResultType = tmp.CecilType }
            ) { Parent = stm, SourceResultType = tmp.CecilType };

            stm.Condition = new BinaryOperatorExpression(
                new IdentifierExpression(stm.LoopIndex),
                ICSharpCode.Decompiler.CSharp.Syntax.BinaryOperatorType.LessThan,
                new PrimitiveExpression(loopedges.Item2 / loopedges.Item3, tmp.CecilType.Module.ImportReference(typeof(int)))
            )
            { 
                Parent = stm,
                SourceResultType = tmp.CecilType.Module.ImportReference(typeof(bool))
            };

			return nstm;
		}
	}
}
