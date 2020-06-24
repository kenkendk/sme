using System;
using SME.AST;

namespace SME.VHDL.Transformations
{
    public class WrapIfComposite : SME.AST.Transform.WrapIfComposite
    {
        public WrapIfComposite()
            : base(new Type[] { typeof(CustomNodes.ConversionExpression) }, false)
        { }

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
