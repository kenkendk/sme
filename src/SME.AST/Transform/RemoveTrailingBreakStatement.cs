using System;
using System.Collections.Generic;
using System.Linq;

namespace SME.AST.Transform
{
    /// <summary>
    /// Removes break statements that are the last line of a switch case.
    /// </summary>
    public class RemoveTrailingBreakStatement : IASTTransform
    {
        /// <summary>
        /// Lookup table of visited statements to speed up the lookup.
        /// </summary>
        private HashSet<AST.SwitchStatement> m_visited = new HashSet<SwitchStatement>();

        /// <summary>
        /// Applies the transformation.
        /// </summary>
        /// <returns>The transformed item.</returns>
        /// <param name="item">The item to visit.</param>
        public virtual ASTItem Transform(ASTItem item)
        {
            var ss = item as AST.SwitchStatement;
            if (ss == null)
                return item;

            if (m_visited.Contains(ss))
                return item;
            m_visited.Add(ss);

            var changed = false;
            foreach (var cs in ss.Cases)
            {
                var last = cs.Item2.SelectMany(x => x.LeavesOnly()).LastOrDefault();
                if (last is BreakStatement)
                {
                    var es = new EmptyStatement();

                    (last as Statement).ReplaceWith(es);
                    changed = true;
                }
            }

            return changed ? null : ss;
        }
    }
}
