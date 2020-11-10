using System;
using System.Collections.Generic;
using System.Linq;
using SME.AST;
using SME.AST.Transform;

namespace SME.VHDL.Transformations
{
    /// <summary>
    /// Untangles else statements.
    /// </summary>
    public class UntangleElseStatements : IASTTransform
    {
        /// <summary>
        /// The render state.
        /// </summary>
        private readonly RenderState State;
        /// <summary>
        /// The method being transformed.
        /// </summary>
        private readonly Method Method;

        /// <summary>
        /// Cache of handled methods.
        /// </summary>
        private HashSet<ASTItem> m_handled = new HashSet<ASTItem>();

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SME.VHDL.Transformations.UntangleElseStatements"/> class.
        /// </summary>
        /// <param name="state">The render state.</param>
        /// <param name="method">The method being rendered.</param>
        public UntangleElseStatements(RenderState state, Method method)
        {
            State = state;
            Method = method;
        }

        /// <summary>
        /// Applies the transformation.
        /// </summary>
        /// <returns>The transformed item.</returns>
        /// <param name="item">The item to visit.</param>
        public ASTItem Transform(ASTItem item)
        {
            if (item is ReturnStatement && Method.Parent is AST.Process && ((AST.Process)Method.Parent).MainMethod == Method)
            {
                if (m_handled.Contains(item))
                    return item;

                m_handled.Add(item);

                var parentif = item.Parent;
                while (parentif != null && !(parentif is AST.IfElseStatement))
                    parentif = parentif.Parent;

                if (parentif == null)
                {
                    Console.WriteLine("Unable to transform return statement in main method, try building the source program in Debug mode.");
                    return item;
                }

                if (!(((AST.IfElseStatement)parentif).FalseStatement is EmptyStatement))
                {
                    Console.WriteLine("Unable to transform return statement in main method, try building the source program in Debug mode.");
                    return item;
                }

                Statement[] blocksource;
                if (parentif.Parent is AST.Method)
                {
                    blocksource = (parentif.Parent as AST.Method).Statements;
                }
                else if (parentif.Parent is AST.BlockStatement)
                {
                    blocksource = (parentif.Parent as AST.BlockStatement).Statements;
                }
                else
                {
                    Console.WriteLine("Unable to transform return statement in main method, try building the source program in Debug mode.");
                    return item;
                }

                var ix = Array.IndexOf(blocksource, parentif);
                if (ix < 0)
                {
                    Console.WriteLine("Unable to transform return statement in main method, try building the source program in Debug mode.");
                    return item;
                }

                var remain = blocksource.Skip(ix + 1).ToArray();

                ((ReturnStatement)item).ReplaceWith(new EmptyStatement() { Parent = item.Parent });

                // If there are no other statements, we are good, but this should not happen
                if (remain.Length == 0)
                {
                    return null;
                }
                else
                {
                    if (parentif.Parent is AST.Method)
                    {
                        (parentif.Parent as AST.Method).Statements = blocksource.Take(ix + 1).ToArray();
                    }
                    else // if (parentif.Parent is AST.BlockStatement)
                    {
                        (parentif.Parent as AST.BlockStatement).Statements = blocksource.Take(ix + 1).ToArray();
                    }

                    // One left, then skip the block construct
                    if (remain.Length == 1)
                    {
                        ((AST.IfElseStatement)parentif).FalseStatement = remain[0];
                    }
                    else
                    {
                        ((AST.IfElseStatement)parentif).FalseStatement = new BlockStatement()
                        {
                            Parent = parentif,
                            Statements = remain,
                        };
                    }

                    ((AST.IfElseStatement)parentif).FalseStatement.UpdateParents();

                    return null;
                }

            }

            return item;
        }
    }
}
