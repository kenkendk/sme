using System;
using System.Collections.Generic;
using System.Linq;
using SME.AST;
using SME.AST.Transform;
using Microsoft.CodeAnalysis.CSharp;

namespace SME.VHDL.Transformations
{
    /// <summary>
    /// Handles cases where the loop increment is not one,
    /// by adding a temporary variable that holds the multiplied result,
    /// and updating the sub-tree to use that variable instead.
    /// </summary>
    public class FixForLoopIncrements : IASTTransform
    {
        /// <summary>
        /// The render state.
        /// </summary>
        private readonly RenderState State;
        /// <summary>
        /// The method being transformed.
        /// </summary>
        private readonly Method Method;
        /// <summary>
        /// Cache of already processed statements.
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
        /// Applies the transformation.
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

            var tmp = State.RegisterTemporaryVariable(Method, stm.LoopIndex.MSCAType);
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
                        SourceResultType = tmp.MSCAType
                    },
                    Operator = SyntaxKind.EqualsToken,
                    Right = new BinaryOperatorExpression()
                    {
                        Left = new IdentifierExpression()
                        {
                            Name = stm.LoopIndex.Name,
                            Target = stm.LoopIndex,
                            SourceExpression = exp,
                            SourceResultType = stm.LoopIndex.MSCAType
                        },
                        Operator = SyntaxKind.AsteriskToken,
                        Right = new PrimitiveExpression()
                        {
                            Value = incr,
                            SourceResultType = tmp.MSCAType,
                            SourceExpression = exp,
                        },
                        SourceExpression = exp,
                        SourceResultType = tmp.MSCAType
                    },
                    SourceExpression = exp,
                    SourceResultType = tmp.MSCAType
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
                    SyntaxKind.PlusToken,
                    new PrimitiveExpression(1, tmp.MSCAType)
                )
                { SourceResultType = tmp.MSCAType }
            ) { Parent = stm, SourceResultType = tmp.MSCAType };

            stm.Condition = new BinaryOperatorExpression(
                new IdentifierExpression(stm.LoopIndex),
                SyntaxKind.LessThanToken,
                new PrimitiveExpression((loopedges.Item2-1-loopedges.Item1) / loopedges.Item3 + 1,
                tmp.MSCAType.LoadType(typeof(int)))
            )
            {
                Parent = stm,
                SourceResultType = tmp.MSCAType.LoadType(typeof(bool))
            };

            stm.Initializer = new PrimitiveExpression(0, tmp.MSCAType);

            return nstm;
        }
    }

}
