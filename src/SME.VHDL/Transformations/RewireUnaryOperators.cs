﻿using System;
using System.Linq;
using SME.AST;
using SME.AST.Transform;
using Microsoft.CodeAnalysis.CSharp;

namespace SME.VHDL.Transformations
{
    /// <summary>
    /// Converts unary operators into full statements.
    /// </summary>
    public class RewireUnaryOperators : IASTTransform
    {
        /// <summary>
        /// The render state.
        /// </summary>
        private readonly RenderState State;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SME.VHDL.Transformations.InjectTypeConversions"/> class.
        /// </summary>
        /// <param name="state">The render state.</param>
        public RewireUnaryOperators(RenderState state)
        {
            State = state;
        }

        /// <summary>
        /// Applies the transformation.
        /// </summary>
        /// <returns>The transformed item.</returns>
        /// <param name="el">The item to visit.</param>
        public ASTItem Transform(ASTItem el)
        {
            var uoe = el as UnaryOperatorExpression;
            if (uoe == null)
                return el;

            var incr =
                new[] {
                    SyntaxKind.PlusPlusToken,
                    SyntaxKind.MinusMinusToken
                }.Contains(uoe.Operator);

            if (!incr)
                return el;


            var cnst = new PrimitiveExpression()
            {
                SourceExpression = uoe.SourceExpression,
                SourceResultType = uoe.SourceResultType.LoadType(typeof(int)),
                Value = 1
            };

            State.TypeLookup[cnst] = VHDLTypes.INTEGER;

            var boe = new BinaryOperatorExpression()
            {
                Left = uoe.Operand.Clone(),
                Right = cnst,
                SourceExpression = uoe.SourceExpression,
                SourceResultType = uoe.SourceResultType,
                Operator =
                    uoe.Operator == SyntaxKind.MinusMinusToken

                   ? SyntaxKind.MinusToken
                   : SyntaxKind.PlusToken
            };

            State.TypeLookup[boe] = State.VHDLType(uoe.Operand);

            // Prepare an assignment expresion
            var ase = new AssignmentExpression()
            {
                Left = uoe.Operand.Clone(),
                Right = boe,
                Operator = SyntaxKind.EqualsToken,
                SourceExpression = uoe.SourceExpression,
                SourceResultType = uoe.SourceResultType
            };

            var exps = new ExpressionStatement()
            {
                Expression = ase,
            };

            exps.UpdateParents();

            if (uoe.Operator == SyntaxKind.PlusPlusToken || uoe.Operator == SyntaxKind.MinusMinusToken)
            {
                if (uoe.Parent is ExpressionStatement || uoe.Parent is ForStatement)
                {
                    // Simple "i++;" statements are replaced with i = i + 1
                    uoe.ReplaceWith(ase);
                    return ase;
                }
                else
                {
                    // More complicated are split
                    uoe.AppendStatement(exps);

                    // Remove the operator now, as the assignment happens after
                    uoe.ReplaceWith(uoe.Operand);
                    return exps;
                }
            }
            else
            {
                // If the left-hand-side is also the target, we can just replace
                // with the binary operator without needing a temporary variable
                if (uoe.Operand.GetTarget() != null && boe.Parent is AssignmentExpression && ((AssignmentExpression)boe.Parent).Left.GetTarget() == uoe.Operand.GetTarget())
                {
                    uoe.ReplaceWith(boe);
                    return boe;
                }
                else
                {
                    // TODO: Can have more complex cases where
                    // the expression is use multiple times in the same statement and some need the updated version,
                    // others the non-updated version.
                    // To support this, we need to seek for references to the target
                    // and replace the references with the correct pre/post versions
                    uoe.PrependStatement(exps);
                    uoe.ReplaceWith(boe);
                    return boe;
                }
            }
        }
    }
}
