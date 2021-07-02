using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SME.AST
{
    // This partial part deals with statements.
    public partial class ParseProcesses
    {
        /// <summary>
        /// Processes a single statement from the decompiler and returns an AST entry for it.
        /// </summary>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method where the statement is found.</param>
        /// <param name="statement">The decompiler statement to process.</param>
        protected virtual Statement Decompile(NetworkState network, ProcessState proc, MethodState method, StatementSyntax statement)
        {
            if (statement is ExpressionStatementSyntax)
                return Decompile(network, proc, method, statement as ExpressionStatementSyntax);
            else if (statement is IfStatementSyntax)
                return Decompile(network, proc, method, statement as IfStatementSyntax);
            else if (statement is BlockSyntax)
                return Decompile(network, proc, method, statement as BlockSyntax);
            else if (statement is LocalDeclarationStatementSyntax)
                return Decompile(network, proc, method, statement as LocalDeclarationStatementSyntax);
            else if (statement is SwitchStatementSyntax)
                return Decompile(network, proc, method, statement as SwitchStatementSyntax);
            else if (statement is ReturnStatementSyntax)
                return Decompile(network, proc, method, statement as ReturnStatementSyntax);
            else if (statement is ForStatementSyntax)
                return Decompile(network, proc, method, statement as ForStatementSyntax);
            else if (statement is BreakStatementSyntax)
                return Decompile(network, proc, method, statement as BreakStatementSyntax);
            else if (statement is CheckedStatementSyntax)
            {
                // Checking for overflow is not translated.
                return Decompile(network, proc, method, (statement as CheckedStatementSyntax).Block);
            }
            else if (statement is GotoStatementSyntax)
                return Decompile(network, proc, method, statement as GotoStatementSyntax);
            else if (statement is LabeledStatementSyntax)
                return Decompile(network, proc, method, statement as LabeledStatementSyntax);
            else if (statement is WhileStatementSyntax)
                return Decompile(network, proc, method, statement as WhileStatementSyntax);
            else if (statement is null)
                return new EmptyStatement();
            else
                throw new Exception(string.Format("Unsupported statement: {0} ({1})", statement, statement.GetType().FullName));
        }

        /// <summary>
        /// Processes a single expression statement from the decompiler and returns an AST entry for it.
        /// </summary>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method where the statement is found.</param>
        /// <param name="statement">The decompiler statement to process.</param>
        protected virtual ExpressionStatement Decompile(NetworkState network, ProcessState proc, MethodState method, ExpressionStatementSyntax statement)
        {
            if (statement.GetType() != typeof(ExpressionStatementSyntax))
                throw new Exception(string.Format("Unsupported expression statement: {0} ({1})", statement, statement.GetType().FullName));

            var s = new ExpressionStatement()
            {
                Parent = method
            };

            s.Expression = Decompile(network, proc, method, s, statement.Expression);
            return s;
        }

        /// <summary>
        /// Processes a single if statement from the decompiler and returns an AST entry for it.
        /// </summary>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method where the statement is found.</param>
        /// <param name="statement">The decompiler statement to process.</param>
        protected virtual IfElseStatement Decompile(NetworkState network, ProcessState proc, MethodState method, IfStatementSyntax statement)
        {
            var s = new IfElseStatement()
            {
                TrueStatement = Decompile(network, proc, method, statement.Statement),
                FalseStatement = Decompile(network, proc, method, statement.Else?.Statement),
                Parent = method
            };

            s.Condition = Decompile(network, proc, method, s, statement.Condition);
            s.TrueStatement.Parent = s;
            s.FalseStatement.Parent = s;

            return s;
        }

        /// <summary>
        /// Processes a single block statement from the decompiler and returns an AST entry for it.
        /// </summary>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method where the statement is found.</param>
        /// <param name="statement">The decompiler statement to process.</param>
        protected virtual BlockStatement Decompile(NetworkState network, ProcessState proc, MethodState method, BlockSyntax statement)
        {

            var s = new BlockStatement
            {
                Parent = method
            };

            method.StartScope(s);

            s.Statements = statement.Statements.Select(x =>
            {
                var n = Decompile(network, proc, method, x);
                n.Parent = s;
                return n;
            }).ToArray();

            method.FinishScope(s);


            return s;
        }

        /// <summary>
        /// Processes a single local variable declaration statement from the decompiler and returns an AST entry for it.
        /// </summary>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method where the statement is found.</param>
        /// <param name="statement">The decompiler statement to process.</param>
        protected virtual Statement Decompile(NetworkState network, ProcessState proc, MethodState method, LocalDeclarationStatementSyntax statement)
        {
            ITypeSymbol vartype = null;

            var init = statement.Declaration.Variables.FirstOrDefault(x => x.Initializer != null && x.Initializer.Value is MemberAccessExpressionSyntax);
            if (init != null)
            {
                var mt = TryLocateElement(network, proc, method, null, init.Initializer.Value);
                if (mt != null && mt is AST.Bus)
                    vartype = LoadType(((AST.Bus)mt).SourceType);
            }

            if (vartype == null)
                vartype = LoadType(statement.Declaration.Type, method);

            if (vartype.IsBusType())
            {
                foreach (var n in statement.Declaration.Variables)
                {
                    if (n.Initializer.Value is MemberAccessExpressionSyntax)
                    {
                        proc.BusInstances[n.Identifier.Text] = LocateBus(network, proc, method, n.Initializer.Value);
                    }
                    else
                    {
                        var match = proc.MSCAType.GetClassDecl().Members.OfType<FieldDeclarationSyntax>().Where(x => LoadType(x.Declaration.Type).IsSameTypeReference(vartype)).FirstOrDefault();
                        if (match != null)
                            proc.BusInstances[n.Identifier.Text] = LocateBus(network, proc, method, n.Initializer.Value);
                        else
                            Console.WriteLine("Unable to determine what bus is assigned to variable {0}", n.Identifier.Text);
                    }
                }

                return new EmptyStatement()
                {
                    Parent = method
                };
            }
            else
            {
                var statements = new List<Statement>();

                foreach (var n in statement.Declaration.Variables)
                {
                    RegisterVariable(network, proc, method, vartype, n);
                    if (n.Initializer != null)
                        statements.Add(Decompile(network, proc, method, SyntaxFactory.ExpressionStatement(
                                SyntaxFactory.AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    SyntaxFactory.IdentifierName(n.Identifier.ValueText),
                                    SyntaxFactory.Token(SyntaxKind.EqualsToken),
                                    n.Initializer.Value))));
                }

                if (statements.Count == 0)
                {
                    return new EmptyStatement()
                    {
                        Parent = method
                    };
                }
                else if (statements.Count == 1)
                {
                    statements[0].Parent = method;
                    return statements[0];
                }
                else
                {
                    var s = new BlockStatement()
                    {
                        Statements = statements.ToArray(),
                        Parent = method
                    };

                    foreach (var x in s.Statements)
                        x.Parent = s;

                    return s;
                }
            }
        }

        /// <summary>
        /// Processes a single switch statement from the decompiler and returns an AST entry for it.
        /// </summary>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method where the statement is found.</param>
        /// <param name="statement">The decompiler statement to process.</param>
        protected virtual SwitchStatement Decompile(NetworkState network, ProcessState proc, MethodState method, SwitchStatementSyntax statement)
        {
            var s = new SwitchStatement()
            {
                Parent = method,
                // Default expression is a null expression
                HasDefault = statement.Sections
                    .SelectMany(x => x.Labels.OfType<DefaultSwitchLabelSyntax>())
                    .Any()
            };

            s.SwitchExpression = Decompile(network, proc, method, s, statement.Expression);

            s.Cases = statement
                .Sections
                .Select(x => new Tuple<Expression[], Statement[]>(
                    x.Labels.Select(y => y is CaseSwitchLabelSyntax ? Decompile(network, proc, method, s, (y as CaseSwitchLabelSyntax).Value) : new EmptyExpression()).ToArray(),
                    x.Statements.Select(y => Decompile(network, proc, method, y)).ToArray()
                ))
                .ToArray();

            foreach (var c in s.Cases)
            {
                foreach (var x in c.Item1)
                    x.Parent = s;
                foreach (var x in c.Item2)
                    x.Parent = s;
            }

            return s;
        }

        /// <summary>
        /// Processes a single return statement from the decompiler and returns an AST entry for it.
        /// </summary>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method where the statement is found.</param>
        /// <param name="statement">The decompiler statement to process.</param>
        protected virtual Statement Decompile(NetworkState network, ProcessState proc, MethodState method, ReturnStatementSyntax statement)
        {
            var s = new ReturnStatement()
            {
                Parent = method
            };

            s.ReturnExpression = statement.Expression == null ? new EmptyExpression() : Decompile(network, proc, method, s, statement.Expression);

            return s;
        }

        /// <summary>
        /// Finds the length of an array or a primitive value for use in loop bounds.
        /// </summary>
        /// <returns>The array length or primitive.</returns>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method where the statement is found.</param>
        /// <param name="src">The expression to examine.</param>
        protected virtual DataElement ResolveArrayLengthOrPrimitive(NetworkState network, ProcessState proc, MethodState method, ExpressionSyntax src)
        {
            if (src is LiteralExpressionSyntax)
                try
                {
                    return new Constant
                    {
                        Source = src,
                        DefaultValue = Convert.ToInt32((src as LiteralExpressionSyntax).Token.Value),
                        MSCAType = LoadType(typeof(int)),
                        Parent = method
                    };
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("Unable to resolve as a constant value: {0}", src), ex);
                }
            DataElement member;
            var ex_left = src as MemberAccessExpressionSyntax;
            if (ex_left != null)
            {
                if (ex_left.Name.Identifier.ValueText != "Length")
                    throw new Exception(string.Format("Only plain style for loops supported: {0}", src));
                member = LocateDataElement(network, proc, method, null, ex_left.Expression);
            }
            else
            {
                var ex_id = src as IdentifierNameSyntax;
                if (ex_id == null)
                    throw new ArgumentException(string.Format("Unable to resolve loop limit: {0}", src));
                return LocateDataElement(network, proc, method, null, ex_id);
            }

            if (member.MSCAType.IsFixedArrayType())
            {
                if (member.Source is MemberDeclarationSyntax)
                {
                    return new Constant
                    {
                        Source = member,
                        DefaultValue = ((MemberDeclarationSyntax)member.Source).GetFixedArrayLength(m_semantics),
                        MSCAType = LoadType(typeof(int))
                    };
                }
                else if (member.Source is System.Reflection.MemberInfo)
                {
                    return new Constant
                    {
                        Source = member,
                        DefaultValue = ((System.Reflection.MemberInfo)member.Source).GetFixedArrayLength(),
                        MSCAType = LoadType(typeof(int))
                    };
                }
            }

            var value = member.DefaultValue;

            if (value is AST.ArrayCreateExpression)
            {
                var ce = (value as ArrayCreateExpression);
                var target = ce.ElementExpressions.Length;
                return new Constant()
                {
                    DefaultValue = target,
                    Source = ce,
                    MSCAType = LoadType(typeof(int))
                };

            }
            else if (value is AST.EmptyArrayCreateExpression)
            {
                var ce = (value as EmptyArrayCreateExpression);
                var target = ce.SizeExpression.GetTarget();
                if (target == null)
                {
                    return new Constant()
                    {
                        DefaultValue = ((PrimitiveExpression)ce.SizeExpression).Value,
                        Source = ce,
                        MSCAType = LoadType(typeof(int))
                    };
                }
                else
                {
                    return new Constant()
                    {
                        DefaultValue = target.DefaultValue,
                        Source = target.Source,
                        MSCAType = LoadType(typeof(int))
                    };
                }
            }


            if (value is ArrayCreationExpressionSyntax)
                return new Constant()
                {
                    Source = value,
                    DefaultValue = (value as ArrayCreationExpressionSyntax).Initializer.Expressions.Count(),
                    MSCAType = LoadType(typeof(int)),
                    Parent = method
                };

            if (value is Array)
                return new Constant()
                {
                    DefaultValue = ((Array)value).Length,
                    Source = value,
                    MSCAType = LoadType(typeof(int))
                };

            if (value is MemberDeclarationSyntax)
            {
                try
                {
                    var mr = value as MemberDeclarationSyntax;
                    var mrsym = (mr as FieldDeclarationSyntax).LoadSymbol(m_semantics) as IFieldSymbol;
                    if (mr is FieldDeclarationSyntax && network.ConstantLookup.Keys.Where(x => SymbolEqualityComparer.Default.Equals(x.Item2,mrsym)).Any())
                    {
                        return ResolveArrayLengthOrPrimitive(network, proc, method, ((FieldDeclarationSyntax)mr).Declaration.Variables.First().Initializer.Value);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("Unable to resolve as a constant value: {0}", src), ex);
                }
            }



            try
            {
                return new Constant() {
                    Source = value,
                    DefaultValue = Convert.ToInt32(value),
                    MSCAType = LoadType(typeof(int)),
                    Parent = method
                };
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Unable to resolve as a constant value: {0}", src), ex);
            }
        }
        /// <summary>
        /// Processes a single break statement from the decompiler and returns an AST entry for it.
        /// </summary>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method where the statement is found.</param>
        /// <param name="statement">The decompiler statement to process.</param>
        protected virtual BreakStatement Decompile(NetworkState network, ProcessState proc, MethodState method, BreakStatementSyntax statement)
        {
            return new BreakStatement()
            {
                Parent = method,
            };
        }

        /// <summary>
        /// Processes a single goto statement from the decompiler and returns an AST entry for it.
        /// </summary>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method where the statement is found.</param>
        /// <param name="statement">The decompiler statement to process.</param>
        protected virtual GotoStatement Decompile(NetworkState network, ProcessState proc, MethodState method, GotoStatementSyntax statement)
        {
            return new GotoStatement()
            {
                Parent = method,
                Label = (statement.Expression as IdentifierNameSyntax).Identifier.ValueText
            };
        }

        /// <summary>
        /// Processes a single labeled statement from the decompiler and returns an AST entry for it.
        /// </summary>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method where the statement is found.</param>
        /// <param name="statement">The decompiler statement to process.</param>
        protected virtual LabelStatement Decompile(NetworkState network, ProcessState proc, MethodState method, LabeledStatementSyntax statement)
        {
            return new LabelStatement()
            {
                Parent = method,
                Label = statement.Identifier.ValueText
            };
        }

        /// <summary>
        /// Processes a single while statement from the decompiler and returns an AST entry for it.
        /// </summary>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method where the statement is found.</param>
        /// <param name="statement">The decompiler statement to process.</param>
        protected virtual WhileStatement Decompile(NetworkState network, ProcessState proc, MethodState method, WhileStatementSyntax statement)
        {
            var res = new WhileStatement()
            {
                Parent = method
            };


            res.Condition = Decompile(network, proc, method, res, statement.Condition);
            res.Body = Decompile(network, proc, method, statement.Statement);
            res.Body.Parent = res;

            return res;
        }

        /// <summary>
        /// Processes a single for statement from the decompiler and returns an AST entry for it.
        /// </summary>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method where the statement is found.</param>
        /// <param name="statement">The decompiler statement to process.</param>
        protected virtual ForStatement Decompile(NetworkState network, ProcessState proc, MethodState method, ForStatementSyntax statement)
        {
            if (statement.Incrementors.Count != 1)
                throw new Exception(string.Format("Only plain style for loops supported: {0}", statement));

            if (statement.Declaration.Variables.Count != 1)
                throw new Exception(string.Format("Only plain style for loops supported: {0}", statement));

            var vari = statement.Declaration.Variables.First();
            var name = vari.Identifier.ValueText;

            var itr = statement.Incrementors.First();
            if (itr == null)
                throw new Exception($"Unsupported iterator expression: {statement.Incrementors.First()}");

            var loopvar = new Variable()
            {
                MSCAType = LoadType(typeof(int)),
                Name = name,
                Source = statement,
                isLoopIndex = true
            };

            var initial = SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName(vari.Identifier.ValueText),
                SyntaxFactory.Token(SyntaxKind.EqualsToken),
                vari.Initializer.Value);

            var res = new ForStatement()
            {
                LoopIndex = loopvar,
                Parent = method
            };

            method.StartScope(res);
            method.AddVariable(loopvar);

            loopvar.Parent = res;
            res.Initializer = Decompile(network, proc, method, res, vari.Initializer.Value);
            res.Condition = Decompile(network, proc, method, res, statement.Condition);
            res.Increment = Decompile(network, proc, method, res, itr);
            res.LoopBody = Decompile(network, proc, method, statement.Statement);

            res.LoopBody.Parent = res;
            method.FinishScope(res);

            return res;
        }
    }
}
