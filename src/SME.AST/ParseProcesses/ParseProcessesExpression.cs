using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SME.AST
{
    // This partial part deals with expressions
    public partial class ParseProcesses
    {
        /// <summary>
        /// Decompile the specified expression, given the network, process, method and statement.
        /// </summary>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method where the statement is found.</param>
        /// <param name="statement">The statement where the expression is found.</param>
        /// <param name="expression">The expression to decompile</param>
        protected Expression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, ExpressionSyntax expression)
        {
            if (expression is AssignmentExpressionSyntax)
                return Decompile(network, proc, method, statement, expression as AssignmentExpressionSyntax);
            else if (expression is IdentifierNameSyntax)
                return Decompile(network, proc, method, statement, expression as IdentifierNameSyntax);
            else if (expression is MemberAccessExpressionSyntax)
                return Decompile(network, proc, method, statement, expression as MemberAccessExpressionSyntax);
            else if (expression is LiteralExpressionSyntax)
                return Decompile(network, proc, method, statement, expression as LiteralExpressionSyntax);
            else if (expression is BinaryExpressionSyntax)
                return Decompile(network, proc, method, statement, expression as BinaryExpressionSyntax);
            else if (expression is PrefixUnaryExpressionSyntax)
                return Decompile(network, proc, method, statement, expression as PrefixUnaryExpressionSyntax);
            else if (expression is PostfixUnaryExpressionSyntax)
                return Decompile(network, proc, method, statement, expression as PostfixUnaryExpressionSyntax);
            else if (expression is ElementAccessExpressionSyntax)
                return Decompile(network, proc, method, statement, expression as ElementAccessExpressionSyntax);
            else if (expression is CastExpressionSyntax)
                return Decompile(network, proc, method, statement, expression as CastExpressionSyntax);
            else if (expression is ConditionalExpressionSyntax)
                return Decompile(network, proc, method, statement, expression as ConditionalExpressionSyntax);
            else if (expression is InvocationExpressionSyntax)
            {
                var si = expression as InvocationExpressionSyntax;
                if (si.Expression is MemberAccessExpressionSyntax)
                {
                    var mt = si.Expression as MemberAccessExpressionSyntax;

                    if (mt.ToString() == "base.SimulationOnly" ||
                        mt.ToString() == "Console.WriteLine" ||
                        mt.ToString() == "Console.Write")
                        return new EmptyExpression()
                        {
                            SourceExpression = si,
                            Parent = statement
                        };
                }
                if (si.Expression is IdentifierNameSyntax)
                {
                    var method_name = ((IdentifierNameSyntax)si.Expression).Identifier.ValueText;
                    if (method_name.Equals("SimulationOnly"))
                        return new EmptyExpression()
                        {
                            SourceExpression = si,
                            Parent = statement
                        };
                }

                return Decompile(network, proc, method, statement, expression as InvocationExpressionSyntax);
            }
            else if (expression is ParenthesizedExpressionSyntax)
                return Decompile(network, proc, method, statement, expression as ParenthesizedExpressionSyntax);
            else if (expression is ArrayCreationExpressionSyntax)
                return Decompile(network, proc, method, statement, expression as ArrayCreationExpressionSyntax);
            else if (expression is CheckedExpressionSyntax)
                return Decompile(network, proc, method, statement, expression as CheckedExpressionSyntax);
            else if (expression is AwaitExpressionSyntax)
                return Decompile(network, proc, method, statement, expression as AwaitExpressionSyntax);
            else if (expression is BaseExpressionSyntax)
                // TODO: handle base expressions properly
                return new EmptyExpression() {
                    SourceExpression = expression,
                    Parent = statement
                };
            else
                throw new Exception(string.Format("Unsupported expression: {0} ({1})", expression, expression.GetType().FullName));
        }


        /// <summary>
        /// Decompile the specified assignment expression, given the network, process, method and statement.
        /// </summary>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method where the statement is found.</param>
        /// <param name="statement">The statement where the expression is found.</param>
        /// <param name="expression">The expression to decompile.</param>
        protected Expression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, AssignmentExpressionSyntax expression)
        {
            var lhs = Decompile(network, proc, method, statement, expression.Left);
            var rhs = Decompile(network, proc, method, statement, expression.Right);

            var res = new AssignmentExpression()
            {
                Operator = (SyntaxKind) expression.OperatorToken.RawKind,
                SourceResultType = ResolveExpressionType(network, proc, method, statement, expression),
                SourceExpression = expression,
                Left = lhs,
                Right = rhs,
                Parent = statement
            };

            res.Left.Parent = res;
            res.Right.Parent = res;

            return res;
        }

        /// <summary>
        /// Decompile the specified await expression, given the network, process, method and statement.
        /// </summary>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method where the statement is found.</param>
        /// <param name="statement">The statement where the expression is found.</param>
        /// <param name="expression">The expression to decompile.</param>
        protected AwaitExpression Decompile(NetworkState network1, ProcessState process, MethodState method, Statement statement, AwaitExpressionSyntax expression)
        {
            var res = new AwaitExpression()
            {
                SourceResultType = LoadType(typeof(void)),
                SourceExpression = expression,
                Parent = statement
            };

            var pred = expression.Expression as InvocationExpressionSyntax;
            if (pred == null || !((pred.Expression as IdentifierNameSyntax).Identifier.Text.Equals("ClockAsync")))
                throw new Exception("Only clock waits are supported for now");

            return res;
        }

        /// <summary>
        /// Decompile the specified identifier name expression, given the network, process, method and statement.
        /// </summary>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method where the statement is found.</param>
        /// <param name="statement">The statement where the expression is found.</param>
        /// <param name="expression">The expression to decompile.</param>
        protected IdentifierExpression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, IdentifierNameSyntax expression)
        {
            return new IdentifierExpression()
            {
                SourceResultType = ResolveExpressionType(network, proc, method, statement, expression),
                SourceExpression = expression,
                Target = LocateDataElement(network, proc, method, statement, expression),
                Parent = statement
            };
        }

        /// <summary>
        /// Decompile the specified member access expression, given the network, process, method and statement.
        /// </summary>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method where the statement is found.</param>
        /// <param name="statement">The statement where the expression is found.</param>
        /// <param name="expression">The expression to decompile.</param>
        protected MemberReferenceExpression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, MemberAccessExpressionSyntax expression)
        {
            var mre = new MemberReferenceExpression()
            {
                SourceResultType = ResolveExpressionType(network, proc, method, statement, expression),
                SourceExpression = expression,
                Target = LocateDataElement(network, proc, method, statement, expression),
                Parent = statement
            };
            // TODO proper evaluation for not-buses. Could be useful for supporting array of constant classes. Maybe also nested structs?
            // TODO if the length checks are removed, then normal buses are evaluated as well. This shouldn't be a problem, but for simpleMIPS, it sets the terminate opcode as being a global constant, which then break later evaluation, as the CPU uses a bus named terminate as well. This is a bug because there's a nameclash, but it should be able to handle it, as while they share a name, it's in two widely different scopes.
            if ((mre.Target is Bus && (mre.Target as Bus).SourceInstances.Length > 1) || (mre.Target is BusSignal &&((mre.Target as BusSignal).Parent as Bus).SourceInstances.Length > 1))
                mre.TargetExpression = Decompile(network, proc, method, statement, expression.Expression);
            return mre;
        }

        /// <summary>
        /// Decompile the specified literal expression, given the network, process, method and statement.
        /// </summary>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method where the statement is found.</param>
        /// <param name="statement">The statement where the expression is found.</param>
        /// <param name="expression">The expression to decompile.</param>
        protected PrimitiveExpression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, LiteralExpressionSyntax expression)
        {
            return new PrimitiveExpression()
            {
                SourceResultType = ResolveExpressionType(network, proc, method, statement, expression),
                SourceExpression = expression,
                Value = expression.Token.Value,
                Parent = statement
            };
        }

        /// <summary>
        /// Decompile the specified binary expression, given the network, process, method and statement.
        /// </summary>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method where the statement is found.</param>
        /// <param name="statement">The statement where the expression is found.</param>
        /// <param name="expression">The expression to decompile.</param>
        protected BinaryOperatorExpression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, BinaryExpressionSyntax expression)
        {
            var res = new BinaryOperatorExpression()
            {
                SourceResultType = ResolveExpressionType(network, proc, method, statement, expression),
                SourceExpression = expression,
                Operator = (SyntaxKind) expression.OperatorToken.RawKind,
                Left = Decompile(network, proc, method, statement, expression.Left),
                Right = Decompile(network, proc, method, statement, expression.Right),
                Parent = statement
            };

            res.Left.Parent = res;
            res.Right.Parent = res;

            return res;
        }

        /// <summary>
        /// Decompile the specified prefix unary expression, given the network, process, method and statement.
        /// </summary>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method where the statement is found.</param>
        /// <param name="statement">The statement where the expression is found.</param>
        /// <param name="expression">The expression to decompile.</param>
        protected Expression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, PrefixUnaryExpressionSyntax expression)
        {
            var res = new UnaryOperatorExpression()
            {
                SourceResultType = ResolveExpressionType(network, proc, method, statement, expression),
                SourceExpression = expression,
                Operator = (SyntaxKind) expression.OperatorToken.RawKind,
                Operand = Decompile(network, proc, method, statement, expression.Operand),
                Parent = statement
            };

            res.Operand.Parent = res;

            return res;
        }

        /// <summary>
        /// Decompile the specified postfix unary expression, given the network, process, method and statement.
        /// </summary>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method where the statement is found.</param>
        /// <param name="statement">The statement where the expression is found.</param>
        /// <param name="expression">The expression to decompile.</param>
        protected Expression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, PostfixUnaryExpressionSyntax expression)
        {
            var res = new UnaryOperatorExpression()
            {
                SourceResultType = ResolveExpressionType(network, proc, method, statement, expression),
                SourceExpression = expression,
                Operator = (SyntaxKind) expression.OperatorToken.RawKind,
                Operand = Decompile(network, proc, method, statement, expression.Operand),
                Parent = statement
            };

            res.Operand.Parent = res;

            return res;
        }

        /// <summary>
        /// Decompile the specified element access expression, given the network, process, method and statement.
        /// </summary>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method where the statement is found.</param>
        /// <param name="statement">The statement where the expression is found.</param>
        /// <param name="expression">The expression to decompile.</param>
        protected IndexerExpression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, ElementAccessExpressionSyntax expression)
        {
            if (expression.ArgumentList.Arguments.Count != 1)
                throw new Exception($"Indexer expression had {expression.ArgumentList.Arguments.Count} index arguments, only one is supported");

            var res = new IndexerExpression()
            {
                SourceResultType = ResolveExpressionType(network, proc, method, statement, expression),
                SourceExpression = expression,
                Target = LocateDataElement(network, proc, method, statement, expression),
                TargetExpression = Decompile(network, proc, method, statement, expression.Expression),
                IndexExpression = Decompile(network, proc, method, statement, expression.ArgumentList.Arguments.First().Expression),
                Parent = statement
            };

            res.TargetExpression.Parent = res;
            res.IndexExpression.Parent = res;

            return res;
        }

        /// <summary>
        /// Decompile the specified cast expression, given the network, process, method and statement.
        /// </summary>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method where the statement is found.</param>
        /// <param name="statement">The statement where the expression is found.</param>
        /// <param name="expression">The expression to decompile.</param>
        protected CastExpression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, CastExpressionSyntax expression)
        {
            var res = new CastExpression()
            {
                SourceResultType = ResolveExpressionType(network, proc, method, statement, expression),
                SourceExpression = expression,
                Expression = Decompile(network, proc, method, statement, expression.Expression),
                Parent = statement
            };

            res.Expression.Parent = res;

            return res;
        }

        /// <summary>
        /// Decompile the specified conditional expression, given the network, process, method and statement.
        /// </summary>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method where the statement is found.</param>
        /// <param name="statement">The statement where the expression is found.</param>
        /// <param name="expression">The expression to decompile.</param>
        protected ConditionalExpression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, ConditionalExpressionSyntax expression)
        {
            var res = new ConditionalExpression()
            {
                SourceResultType = ResolveExpressionType(network, proc, method, statement, expression),
                SourceExpression = expression,
                ConditionExpression = Decompile(network, proc, method, statement, expression.Condition),
                TrueExpression = Decompile(network, proc, method, statement, expression.WhenTrue),
                FalseExpression = Decompile(network, proc, method, statement, expression.WhenFalse),
                Parent = statement
            };

            res.ConditionExpression.Parent = res;
            res.TrueExpression.Parent = res;
            res.FalseExpression.Parent = res;

            return res;
        }

        /// <summary>
        /// Decompile the specified invocation expression, given the network, process, method and statement.
        /// </summary>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method where the statement is found.</param>
        /// <param name="statement">The statement where the expression is found.</param>
        /// <param name="expression">The expression to decompile.</param>
        protected InvocationExpression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, InvocationExpressionSyntax expression)
        {
            var res = new InvocationExpression()
            {
                SourceExpression = expression,
                ArgumentExpressions = expression.ArgumentList.Arguments.Select(x => Decompile(network, proc, method, statement, x.Expression)).ToArray(),
                Parent = statement
            };

            foreach (var x in res.ArgumentExpressions)
                x.Parent = res;

            // Register for later lookup
            proc.MethodTargets.Enqueue(new Tuple<Statement, MethodState, InvocationExpression>(statement, method, res));

            return res;
        }

        /// <summary>
        /// Decompile the specified parenthized expression, given the network, process, method and statement.
        /// </summary>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method where the statement is found.</param>
        /// <param name="statement">The statement where the expression is found.</param>
        /// <param name="expression">The expression to decompile.</param>
        protected ParenthesizedExpression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, ParenthesizedExpressionSyntax expression)
        {
            var res = new ParenthesizedExpression()
            {
                SourceResultType = ResolveExpressionType(network, proc, method, statement, expression),
                SourceExpression = expression,
                Expression = Decompile(network, proc, method, statement, expression.Expression)
            };

            res.Expression.Parent = res;

            return res;
        }

        /// <summary>
        /// Decompile the specified array creation expression, given the network, process, method and statement.
        /// </summary>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method where the statement is found.</param>
        /// <param name="statement">The statement where the expression is found.</param>
        /// <param name="expression">The expression to decompile.</param>
        protected ArrayCreateExpression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, ArrayCreationExpressionSyntax expression)
        {
            var res = new ArrayCreateExpression()
            {
                SourceResultType = ResolveExpressionType(network, proc, method, statement, expression),
                SourceExpression = expression,
                ElementExpressions = expression.Initializer.Expressions.Select(x => Decompile(network, proc, method, statement, x)).ToArray(),
                Parent = statement
            };

            foreach (var x in res.ElementExpressions)
                x.Parent = res;

            return res;
        }

        /// <summary>
        /// Decompile the specified checked expression, given the network, process, method and statement.
        /// </summary>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method where the statement is found.</param>
        /// <param name="statement">The statement where the expression is found.</param>
        /// <param name="expression">The expression to decompile.</param>
        protected CheckedExpression Decompile(NetworkState network, ProcessState proc, MethodState method, Statement statement, CheckedExpressionSyntax expression)
        {
            var res = new CheckedExpression()
            {
                SourceResultType = ResolveExpressionType(network, proc, method, statement, expression),
                SourceExpression = expression,
                Expression = Decompile(network, proc, method, statement, expression.Expression),
                Parent = statement
            };

            res.Expression.Parent = res;

            return res;
        }
    }
}
