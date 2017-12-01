using System;
using System.Linq;
using SME.AST;
using SME.AST.Transform;

namespace SME.VHDL.Transformations
{
    /// <summary>
    /// Fixes types in a switch statement, where the switch expression does not have the same types as the cases
    /// </summary>
    public class FixSwitchStatementTypes : IASTTransform
    {
        /// <summary>
        /// The render state
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
        /// Applies the transformation
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
                    var targettype = ss
                        .Cases
                        .SelectMany(x => x.Item1)
                        .First(x => x != null && !(x is EmptyExpression))
                        .SourceResultType;

                    // Create a variable the same type as the cases
                    // and set it to the switch expression
                    var nvar = State.RegisterTemporaryVariable(mp, targettype);
                    State.TypeLookup[nvar] = targets.First();

                    var nvexp = new IdentifierExpression()
                    {
                        Name = nvar.Name,
                        SourceExpression = ss.SwitchExpression.SourceExpression,
                        SourceResultType = nvar.CecilType,
                        Target = nvar
                    };

                    var asss = new AST.ExpressionStatement()
                    {
                        Expression = new AST.AssignmentExpression()
                        {
                            Left = nvexp,
                            Operator = ICSharpCode.NRefactory.CSharp.AssignmentOperatorType.Assign,
                            Right = ss.SwitchExpression,
                            SourceExpression = ss.SwitchExpression.SourceExpression,
                            SourceResultType = nvar.CecilType
                        }
                    };

                    ss.SwitchExpression = nvexp.Clone();
                    ss.SwitchExpression.Parent = ss;

                    asss.UpdateParents();
                    ss.PrependStatement(asss);
                    return null;
                }
            }

            return el;
        }
    }
}
