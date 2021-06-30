using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

namespace SME.AST.Transform
{
    /// <summary>
    /// Rebuilds switch statements that are decomposed into GOTO statements.
    /// </summary>
    public class RecontructSwitchStatement : IASTTransform
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

            // Figure out if the switch is using goto statements
            var goto_statements = ss.Cases.SelectMany(x => x.Item2).SelectMany(x => x.All()).OfType<GotoStatement>().ToList();
            if (goto_statements.Count == 0)
                return item;

            // Extract the cases that we will work with
            var cases = ss.Cases.ToList();

            // Grab the name of the label, so we can look for it
            var labelname = goto_statements.First().Label;
            if (goto_statements.Any(x => x.Label != labelname))
                return item;

            // We assume that there is just one label with the given name in the method
            var mp = item.GetNearestParent<Method>();
            if (mp == null)
                return item;

            // Find the one label statement that is the goto target
            var lbltarget = mp.All().OfType<LabelStatement>().Where(x => x.Label == labelname).FirstOrDefault();
            if (lbltarget == null)
                return item;

            // Find the if(...) statements that are actually stray case statements
            foreach (var ifs in ((Method)mp).Statements.SelectMany(x => x.All()).OfType<IfElseStatement>())
            {
                // Goldilocks testing, we only accept something like:
                // if (switch_var == value) {
                //     ...
                //     goto label;
                // }

                if (!(ifs.FalseStatement is EmptyStatement)) continue;
                if (!(ifs.TrueStatement.All().Last() is GotoStatement)) continue;
                if ((ifs.TrueStatement.All().Last() as GotoStatement).Label != labelname) continue;
                if (!(ifs.Condition is BinaryOperatorExpression)) continue;

                var beo = ifs.Condition as BinaryOperatorExpression;

                if (beo.Operator != SyntaxKind.EqualsEqualsToken) continue;

                Expression casetarget;

                if (beo.Left.GetTarget() == ss.SwitchExpression.GetTarget())
                    casetarget = beo.Right;
                else if (beo.Right.GetTarget() == ss.SwitchExpression.GetTarget())
                    casetarget = beo.Left;
                else
                    continue;

                if (casetarget == null)
                    continue;

                // We have a case that looks just right :)
                cases.Add(new Tuple<Expression[], Statement[]>(
                    new Expression[] { casetarget },
                    new Statement[] { ifs.TrueStatement }
                ));

                ifs.ReplaceWith(new EmptyStatement());

                ifs.TrueStatement.Parent = ss;
                ifs.TrueStatement.UpdateParents();
            }

            // Sometimes we have an added filter on the outside
            if (ss.Parent != lbltarget.Parent)
            {
                var ssp = ss.Parent;
                while (ssp != null && !(ssp is IfElseStatement))
                    ssp = ssp.Parent;

                var ssifp = ssp as IfElseStatement;
                if (ssifp != null)
                {
                    var beo = ssifp.Condition as BinaryOperatorExpression;
                    if (beo != null)
                    {
                        if (beo.Left.GetTarget() == ss.SwitchExpression.GetTarget() || beo.Right.GetTarget() == ss.SwitchExpression.GetTarget())
                        {
                            var replace = false;
                            if (ssifp.TrueStatement.All().Contains(ss))
                                replace = !ssifp.FalseStatement.LeavesOnly().Any(x => !(x is EmptyStatement));
                            else if (ssifp.FalseStatement.All().Contains(ss))
                                replace = !ssifp.TrueStatement.LeavesOnly().Any(x => !(x is EmptyStatement));

                            if (replace)
                                ssifp.ReplaceWith(ss);
                        }
                    }
                }
            }

            // Find the container for the goto label
            Statement[] ps;
            if (lbltarget.Parent is BlockStatement)
                ps = (lbltarget.Parent as BlockStatement).Statements;
            else
                ps = (lbltarget.Parent as Method).Statements;

            // Find the switch and label taget in the list
            var labelindex = Array.IndexOf(ps, lbltarget);
            var switchindex = Array.IndexOf(ps, ss);

            // If we have different parents, the index does not point to the switch,
            // so we figure out where the switch ends
            if (switchindex < 0)
            {
                var ssp = ss.Parent;
                while (ssp != null && ssp.Parent != lbltarget.Parent)
                    ssp = ssp.Parent;

                if (ssp == null)
                    return item;

                switchindex = Array.IndexOf(ps, ssp);
            }

            if (labelindex < 0 || switchindex < 0 || switchindex > labelindex)
                return item;


            // Extract the items between the switch and the label target and use as the default case
            var defaultstatements = ps.Skip(switchindex + 1).Take(labelindex - switchindex - 1).Where(x => !(x is EmptyStatement)).ToList();
            var remainingstatements = ps.Take(switchindex + 1).Concat(ps.Skip(labelindex + 1)).ToArray();


            // We can get an empty case due to the transformations above
            cases = cases.Where(x => !x.Item2.All(y => y is EmptyStatement)).ToList();

            // If we have default actions, or the switch is missing a default entry, add it
            if (defaultstatements.Count != 0 || !cases.Any(x => x.Item1.All(y => y is EmptyExpression)))
            {
                cases.Add(new Tuple<Expression[], Statement[]>(
                    new Expression[] { new EmptyExpression() },
                    defaultstatements.ToArray()
                ));

                foreach (var n in defaultstatements)
                    n.Parent = ss;
                defaultstatements.UpdateParents();
            }

            // Set up the switch and replace the statements
            ss.Cases = cases.ToArray();
            ss.UpdateParents();

            // Rewrite the list of statements back, excluding the default case
            if (lbltarget.Parent is BlockStatement)
            {
                (lbltarget.Parent as BlockStatement).Statements = remainingstatements;
                (lbltarget.Parent as BlockStatement).UpdateParents();
            }
            else
            {
                (lbltarget.Parent as Method).Statements = remainingstatements;
                foreach (var n in remainingstatements)
                    n.UpdateParents();
            }

            // Rewrite all goto's into break statements, requery the cases as we may have changed them
            goto_statements = ss.Cases.SelectMany(x => x.Item2).SelectMany(x => x.All()).OfType<GotoStatement>().ToList();
            foreach (var n in goto_statements)
                n.ReplaceWith(new EmptyStatement());

            return null;
        }
    }
}
