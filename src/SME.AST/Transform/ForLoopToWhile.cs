using System;
using System.Linq;

namespace SME.AST.Transform
{
    /// <summary>
    /// Transforms a <see cref="ForStatement"/> to a <see cref="WhileStatement"/>.
    /// </summary>
    public class ForLoopToWhile : IASTTransform
    {
        /// <summary>
        /// Transforms a <see cref="ForStatement"/> to a <see cref="WhileStatement"/>.
        /// </summary>
        /// <returns>The input item or a newly created <see cref="WhileStatement"/>.</returns>
        /// <param name="item">The item to transform.</param>
        public ASTItem Transform(ASTItem item)
        {
            var fs = item as ForStatement;
            if (fs == null)
                return item;

            if (fs.LoopBody.All().OfType<BreakStatement>().Any())
                throw new Exception("Cannot transform loops with break or continue inside");

            var init = new ExpressionStatement(fs.Initializer);
            var ws = new WhileStatement(fs.Condition, fs.LoopBody);
            ws.Body.AppendStatement(new ExpressionStatement(fs.Increment));

            fs.ReplaceWith(ws);
            return fs;
        }
    }
}
