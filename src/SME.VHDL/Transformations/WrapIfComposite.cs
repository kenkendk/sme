using System;
using SME.AST;

namespace SME.VHDL.Transformations
{
    /// <summary>
    /// Converts an composite if into a full if statement.
    /// </summary>
    public class WrapIfComposite : SME.AST.Transform.WrapIfComposite
    {
        /// <summary>
        /// Constructs a new instance of the transformation.
        /// </summary>
        public WrapIfComposite()
            : base(new Type[] { typeof(CustomNodes.ConversionExpression) }, false)
        { }

        /// <summary>
        /// Applies the transformation.
        /// </summary>
        /// <returns>The transformed item.</returns>
        /// <param name="item">The item to visit.</param>
        public override AST.ASTItem Transform(AST.ASTItem item)
        {
            var exp = item as Expression;
            if (exp == null)
                return item;

            // If this is top-level, we do not want a wrapping
            if (exp.Parent is Statement || exp.Parent is ParenthesizedExpression || exp.Parent is CustomNodes.ConversionExpression)
                return item;

            return base.Transform(item);
        }
    }
}
