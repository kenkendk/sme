using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SME.AST
{
    // This partial part deals with the IL side of the parsing.
    public partial class ParseProcesses
    {
        /// <summary>
        /// Processes a single method and extracts all the statements in it.
        /// </summary>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method to decompile.</param>
        protected virtual Statement[] Decompile(NetworkState network, ProcessState proc, MethodState method)
        {
            var sx = method.MSCAMethod.Body.Statements;

            foreach (var s in sx.OfType<UsingStatementSyntax>())
                proc.Imports.Add(s.Expression.ToString());

            method.MSCAReturnType = LoadType(method.MSCAMethod.ReturnType);
            method.ReturnVariable =
                (
                    method.MSCAReturnType.IsSameTypeReference(typeof(void))
                    ||
                    method.MSCAReturnType.IsSameTypeReference(typeof(System.Threading.Tasks.Task))
                )
                ? null
                : RegisterTemporaryVariable(network, proc, method, LoadType(method.MSCAMethod.ReturnType, method), method.MSCAMethod);

            if (method.ReturnVariable != null)
                method.ReturnVariable.Parent = method;

            var statements = new List<Statement>();
            var instructions = sx;

            //if (method.IsStateMachine)
            //{
            //    var initial = instructions.FirstOrDefault(x => x.NodeType == NodeType.Statement);
            //    if (!(initial is ICSharpCode. .WhileStatement))
            //        throw new Exception("The first statement in a state process must be a while statement");

            //    instructions = (initial as AST.WhileStatement).Children.Skip(1);
            //    if (instructions.First() is ICSharpCode.Decompiler.CSharp.Syntax.BlockStatement && instructions.Count() == 1)
            //        instructions = instructions.First().Children;

            //}

            foreach (var n in instructions)
                /*try
                {*/
                    statements.Add(Decompile(network, proc, method, n));
                /*}
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to process statement: {n} -> {ex}");
                    statements.Add(new CommentStatement($"Failed to process statement: {n} -> {ex}"));
                    throw ex;
                }*/

            return statements.ToArray();
        }

        /// <summary>
        /// Decompile the specified process, using the specified entry method.
        /// </summary>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The entry method to start decompilation from.</param>
        protected virtual void Decompile(NetworkState network, ProcessState proc, System.Reflection.MethodInfo method)
        {
            var statements = new List<Statement>();
            if (proc.MSCAType == null)
                proc.MSCAType = LoadType(proc.SourceType);

            var proctype = proc.MSCAType;
            IEnumerable<IMethodSymbol> methsyns = new List<IMethodSymbol>();
            while (proctype.ContainingNamespace.ToString().Equals(method.DeclaringType.Namespace ?? "<global namespace>"))
            {
                methsyns = methsyns.Concat(proctype
                    .GetMembers()
                    .OfType<IMethodSymbol>()
                );
                proctype = proctype.BaseType;
            }

            var msy = methsyns
                .FirstOrDefault(x =>
                    x.Name.Equals(method.Name) &&
                    x.Parameters.Length == method.GetParameters().Length
                );
            if (msy == null)
                throw new Exception($"Unable to find a method with the name {method.Name} in type {proc.MSCAType.ToDisplayString()}");

            proc.MainMethod = Decompile(network, proc, msy);

            // If we have comments from the constructors, add them here
            if (statements.Count > 0)
            {
                statements.AddRange(proc.MainMethod.Statements);
                proc.MainMethod.Statements = statements.ToArray();
            }

            var methods = new List<MethodState>();

            while(proc.MethodTargets.Count > 0)
            {
                var tp = proc.MethodTargets.Dequeue();
                var ix = tp.Item3;

                var ic = (ix.SourceExpression as InvocationExpressionSyntax);
                string r;
                if (ic.Expression is MemberAccessExpressionSyntax)
                    r = ((MemberAccessExpressionSyntax)ic.Expression).TryGetInferredMemberName();
                else if (ic.Expression is IdentifierNameSyntax)
                    r = ((IdentifierNameSyntax)ic.Expression).Identifier.ValueText;
                else
                    r = "";

                // TODO: Maybe we can support overloads here as well
                var dm = methods.FirstOrDefault(x => x.Name.Equals(r));
                if (dm == null)
                {
                    var mrsy = methsyns.FirstOrDefault(x => x.Name.Equals(r));
                    if (mrsy == null)
                        throw new Exception($"Unable to resolve method call to {r}");
                    dm = Decompile(network, proc, mrsy);
                    methods.Add(dm);
                }

                proc.Methods = methods.ToArray();
                ix.Target = dm;
                ix.TargetExpression = new MethodReferenceExpression()
                {
                    Parent = ix,
                    SourceExpression = ix.SourceExpression,
                    SourceResultType = dm.ReturnVariable.MSCAType,
                    Target = dm
                };
                ix.SourceResultType = ix.TargetExpression.SourceResultType;
            }

            methods.Reverse();
            proc.Methods = methods.ToArray();
            proc.SharedSignals = proc.Signals.Values.ToArray();
            proc.SharedVariables = proc.Variables.Values.ToArray();
        }

        /// <summary>
        /// Decompiles the specified method.
        /// </summary>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method to decompile.</param>
        protected virtual MethodState Decompile(NetworkState network, ProcessState proc, IMethodSymbol method)
        {
            var synmeth = method.GetSyntax() as MethodDeclarationSyntax;
            var res = new MethodState()
            {
                Name = synmeth.Identifier.Text,
                MSCAMethod = synmeth,
                MSCAFlow = synmeth.LoadDataFlow(m_semantics),
                Parent = proc,
                Ignore = method.HasAttribute<IgnoreAttribute>(),
                IsStateMachine = proc.SourceInstance.Instance is StateProcess
            };

            res.Parameters = synmeth.ParameterList.Parameters.Select(x => ParseParameter(network, proc, res, x)).ToArray();

            if (res.Ignore)
            {
                res.Statements = new Statement[0];
                res.AllVariables = new Variable[0];
                res.Variables = new Variable[0];
            }
            else
            {
                res.Statements = Decompile(network, proc, res);
                res.AllVariables = res.CollectedVariables.Select(x => {x.Name = $"{res.Name}_{x.Name}"; return x; }).ToArray();
                res.Variables = res.Scopes.First().Value.Values.ToArray();
            }

            if (res.ReturnVariable == null)
            {
                res.ReturnVariable = new Variable()
                {
                    MSCAType = LoadType(synmeth.ReturnType),
                    Parent = res,
                    Source = method
                };
            }

            return res;
        }
    }
}
