using System;

namespace SME.AST.Transform
{
    /// <summary>
    /// Interface for performing a transformation on the AST.
    /// </summary>
    public interface IASTTransform
    {
        /// <summary>
        /// Applies the transformation.
        /// </summary>
        /// <returns>The transformed item, if this is not the input item, the transformation will sequence will restart.</returns>
        /// <param name="item">The item to visit.</param>
        ASTItem Transform(ASTItem item);
    }
}
