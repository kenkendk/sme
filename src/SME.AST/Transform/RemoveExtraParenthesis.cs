using System;

namespace SME.AST.Transform
{
    /// <summary>
    /// Removes parenthesis wrappings that are not required. This needs to run as the last step, as it battles with <seealso cref="WrapIfComposite"/>.
    /// </summary>
    public class RemoveExtraParenthesis : IASTTransform
    {
        /// <summary>
        /// Applies the transformation.
        /// </summary>
        /// <returns>The transformed item.</returns>
        /// <param name="item">The item to visit.</param>
        public virtual ASTItem Transform(ASTItem item)
        {
            if (item is AssignmentExpression)
            {
                if (item.Parent is Statement)
                {
                    var aes = item as AssignmentExpression;
                    if (aes.Right is ParenthesizedExpression)
                        return aes.Right.ReplaceWith(((ParenthesizedExpression)aes.Right).Expression);
                }
            }
            else if (item is IfElseStatement)
            {
                var ies = item as IfElseStatement;
                if (ies.Condition is ParenthesizedExpression)
                    return ies.Condition.ReplaceWith(((ParenthesizedExpression)ies.Condition).Expression);
            }

            return item;
        }
    }
}
