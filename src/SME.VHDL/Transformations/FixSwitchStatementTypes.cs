using System;
using System.Linq;
using SME.AST;
using SME.AST.Transform;

namespace SME.VHDL.Transformations
{
    /// <summary>
    /// Fixes types in a switch statement, where the switch expression does not have the same types as the cases.
    /// </summary>
    public class FixSwitchStatementTypes : IASTTransform
    {
        /// <summary>
        /// The render state.
        /// </summary>
        private readonly RenderState State;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SME.VHDL.Transformations.FixSwitchStatementTypes"/> class.
        /// </summary>
        /// <param name="state">The render state.</param>
        public FixSwitchStatementTypes(RenderState state)
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
            var ss = el as SwitchStatement;
            if (ss == null)
                return el;

            var exptype = State.VHDLType(ss.SwitchExpression);

            // Extract expression types
            var targets = ss
                .Cases
                .SelectMany(x => x.Item1)
                .Where(x => x != null && !(x is EmptyExpression))
                .Select(x => State.VHDLType(x))
                .Distinct()
                .ToArray();

            // Case where the expressions are all integer literals,
            // but the source is some numeric
            if (targets.Length == 1 && targets.First() != exptype)
            {
                var mp = ss.GetNearestParent<Method>();
                if (mp != null)
                {
                    ss.SwitchExpression = VHDL.VHDLTypeConversion.ConvertExpression(State, mp, ss.SwitchExpression, targets.First(), ss.SwitchExpression.SourceResultType, false);
                    return el;
                }
            }

            return el;
        }
    }
}
