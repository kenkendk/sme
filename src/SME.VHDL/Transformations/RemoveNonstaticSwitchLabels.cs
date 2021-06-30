using System;
using System.Linq;
using SME.AST;
using SME.AST.Transform;

namespace SME.VHDL.Transformations
{
    /// <summary>
    /// Removes some switch statements that are the result of a typecasting.
    /// </summary>
    public class RemoveNonstaticSwitchLabels : IASTTransform
    {
        /// <summary>
        /// The render state.
        /// </summary>
        private readonly RenderState State;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SME.VHDL.Transformations.AssignVhdlType"/> class.
        /// </summary>
        /// <param name="state">The render state.</param>
        public RemoveNonstaticSwitchLabels(RenderState state)
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

            // We cannot support conversions as part of the switch statements,
            // so we statically compute them here
            var nonstatic = ss.Cases.SelectMany(x => x.Item1).OfType<CustomNodes.ConversionExpression>().ToArray();
            if (nonstatic.Length != 0)
            {
                foreach (var e in nonstatic)
                {
                    var et = State.VHDLType(e);
                    var cx = e.Expression;
                    var pt = State.VHDLType(cx);

                    // If we have a coversion thing caused by an off enum
                    if (et.IsEnum && pt == VHDLTypes.INTEGER && cx is PrimitiveExpression)
                    {
                        var name = State.RegisterCustomEnum(ss.SwitchExpression.SourceResultType, et, (cx as PrimitiveExpression).Value);
                        var c = new AST.Constant() {
                            Name = name,
                            MSCAType = ss.SwitchExpression.SourceResultType,
                            DefaultValue = name // (cx as PrimitiveExpression).Value
                        };

                        var mr = new AST.MemberReferenceExpression()
                        {
                            Parent = ss,
                            SourceResultType = c.MSCAType,
                            Target = c
                        };

                        e.ReplaceWith(mr);
                    }
                }

                return null;
            }

            return el;
        }
    }
}
