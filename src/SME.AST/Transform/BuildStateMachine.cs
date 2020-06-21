using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SME.AST.Transform
{

    public class WhileWithoutAwaitException : Exception
    {
        public WhileWithoutAwaitException(string message) : base(message) { }
    }

    /// <summary>
    /// Builds a state machine from a <see cref="StateProcess"/>'s main method.
    /// </summary>
    public class BuildStateMachine : IASTTransform
    {
        /// <summary>
        /// Checks if all branches contains an <see cref="AwaitExpression"/>. Used by while loops.
        /// <summary>
        /// <param name="statement">The statement to be checked</param>
        private bool AllBranchesHasAwait(Statement statement)
        {
            switch (statement)
            {
                case BlockStatement s:      return s.Statements.Where(x => AllBranchesHasAwait(x)).Any();
                case ExpressionStatement s: return s.Expression is AwaitExpression;
                //case ForStatement s:        return AllBranchesHasAwait(s.LoopBody); // TODO check if empty range?
                case IfElseStatement s:     return AllBranchesHasAwait(s.TrueStatement) && AllBranchesHasAwait(s.FalseStatement);
                case SwitchStatement s:     return s.HasDefault && s.Cases.Select(x => x.Item2).All(x => x.Select(y => AllBranchesHasAwait(y)).Contains(true));
                default:                    return false;
            }
        }

        /// <summary>
        /// Represents a Goto statement used to build the statemachine
        /// </summary>
        private class CaseGotoStatement : GotoStatement
        {
            /// <summary>
            /// The target label
            /// </summary>
            public int CaseLabel;

            /// <summary>
            /// A flag indicating if the statement is a fall-through request
            /// </summary>
            public readonly bool FallThrough;

            /// <summary>
            /// Initializes a new instance of the <see cref="T:SME.AST.Transform.BuildStateMachine.CaseGotoStatement"/> class.
            /// </summary>
            /// <param name="label">The target label.</param>
            public CaseGotoStatement(int label, bool fallthrough)
            {
                CaseLabel = label;
                FallThrough = fallthrough;
            }

            public CaseGotoStatement()
            {
                CaseLabel = -1;
                FallThrough = false;
        }
        }

        /// <summary>
        /// Splits a sequence of statements into a set of cases
        /// </summary>
        /// <returns>The number of new fragments.</returns>
        /// <param name="statement">The statement(s) to split.</param>
        /// <param name="collected">The currently collected statements.</param>
        /// <param name="fragments">The currently collected fragments.</param>
        private int SplitStatement(Statement statement, List<Statement> collected, List<List<Statement>> fragments)
        {
            if (statement is BlockStatement)
            {
                var newCount = 0;
                foreach (var n in ((BlockStatement)statement).Statements)
                    newCount += SplitStatement(n, collected, fragments);

                return newCount;
            }
            else if (statement is WhileStatement)
                return SplitStatement((WhileStatement)statement, collected, fragments);
            else if (statement is ForStatement)
                return SplitStatement((ForStatement)statement, collected, fragments);
            else if (statement is IfElseStatement)
                return SplitStatement((IfElseStatement)statement, collected, fragments);
            else if (statement is SwitchStatement)
                return SplitStatement((SwitchStatement)statement, collected, fragments);
            else if (statement is ExpressionStatement && ((ExpressionStatement)statement).Expression is AwaitExpression)
            {
                if (collected.Count == 0)
                    collected.Add(new EmptyStatement());
                EndFragment(collected, fragments, fragments.Count + 1, false);
                return 1;
            }
            else
            {
                collected.Add(statement);
                return 0;
            }
        }

        /// <summary>
        /// Examines a statement and determines if all branches end with a control flow statement
        /// </summary>
        /// <returns><c>true</c>, if control flow was handled, <c>false</c> otherwise.</returns>
        /// <param name="statement">The statement to examine.</param>
        private bool HandlesControlFlow(AST.Statement statement)
        {
            if (statement is CaseGotoStatement || (statement is ExpressionStatement && ((ExpressionStatement)statement).Expression is AwaitExpression))
                return true;
            else if (statement is BlockStatement)
                return HandlesControlFlow(((BlockStatement)statement).Statements.Last());
            else if (statement is IfElseStatement)
                return HandlesControlFlow(((IfElseStatement)statement).TrueStatement) && HandlesControlFlow(((IfElseStatement)statement).FalseStatement);
            else
                return false;

        }

        /// <summary>
        /// Splits a <see cref="SwitchStatement"/> into a set of cases
        /// </summary>
        /// <returns>The number of new fragments.</returns>
        /// <param name="statement">The statement(s) to split.</param>
        /// <param name="collected">The currently collected statements.</param>
        /// <param name="fragments">The currently collected fragments.</param>
        private int SplitStatement(SwitchStatement statement, List<Statement> collected, List<List<Statement>> fragments)
        {
            // If there are no awaits inside, just treat it like a regular code block
            if (!statement.All().OfType<AwaitExpression>().Any())
            {
                collected.Add(statement);
                return 0;
            }

            var selflabel = fragments.Count;
            var trailings = new List<List<Statement>>();
            var cases = new List<Tuple<Expression[], Statement[]>>();
            foreach (var c in statement.Cases)
            {
                bool found = false;
                var initial = new List<Statement>();
                var trailing = new List<Statement>();
                for (int i = 0; i < c.Item2.Length; i++)
                {
                    if (found)
                    {
                        if (!(c.Item2[i] is BreakStatement))
                            trailing.Add(c.Item2[i]);
                        else // Trailing cases should not have break statements, and all following statements should be ignored
                            break;
                    }
                else
                    {
                        if (c.Item2[i] is ExpressionStatement && ((ExpressionStatement)(c.Item2[i])).Expression is AwaitExpression)
                        { // Stop adding, and inject a goto statement to rest of the block
                            found = true;
                            initial.Add(new CaseGotoStatement(-1, false));
                            initial.Add(new BreakStatement());
            }
                        else
                        { // Add all statements up until await statement
                            if (c.Item2[i] is BreakStatement)
                                initial.Add(new CaseGotoStatement(selflabel, true));
                            initial.Add(c.Item2[i]);
                            if (c.Item2[i] is BreakStatement)
                                break;
                        }
                    }
                }
                cases.Add(new Tuple<Expression[], Statement[]>(c.Item1, initial.ToArray()));
                trailings.Add(trailing);
            }
            statement.Cases = cases.ToArray();

            collected.Add(statement);
            EndFragment(collected, fragments, -1, false);

            for (int i = 0; i < statement.Cases.Length; i++)
            {
                var c = statement.Cases[i].Item2.OfType<CaseGotoStatement>().First();
                if (trailings[i].Count > 0)
                {
                    c.CaseLabel = fragments.Count;
                    trailings[i].Select(x => SplitStatement(x, collected, fragments)).ToList();
                    EndFragment(collected, fragments, selflabel, true);
            }
                else
            {
                    c.CaseLabel = selflabel;
            }
            }

            for (int i = selflabel; i < fragments.Count; i++)
                fragments[i].SelectMany(x =>
                    x.All()
                        .OfType<CaseGotoStatement>()
                        .Where(y => y.CaseLabel == selflabel)
                        .Select(y => y.CaseLabel = fragments.Count)
                    ).ToList();

            return 0; // TODO parenting?
        }

        /// <summary>
        /// Splits a <see cref="IfElseStatement"/> into a set of cases
        /// </summary>
        /// <returns>The number of new fragments.</returns>
        /// <param name="statement">The statement(s) to split.</param>
        /// <param name="collected">The currently collected statements.</param>
        /// <param name="fragments">The currently collected fragments.</param>
        private int SplitStatement(IfElseStatement statement, List<Statement> collected, List<List<Statement>> fragments)
        {
            // If there are no awaits inside, just treat it like a regular code block
            if (!statement.All().OfType<AwaitExpression>().Any())
            {
                collected.Add(statement);
                return 0;
            }

            // Build the if statement with the condition
            var conditionlabel = fragments.Count;
            var cnd = statement.Condition.Clone();
            var ifs = new IfElseStatement(cnd, new EmptyStatement(), new EmptyStatement());
            collected.Add(ifs);
            EndFragment(collected, fragments, -1, true);

            // Build the 'true' branch
            var truelabel = fragments.Count;
            var true_count = SplitStatement(statement.TrueStatement, collected, fragments);
            EndFragment(collected, fragments, fragments.Count + 1, true);

            // Build the 'false' branch - if any
            var falselabel = fragments.Count;
            var false_count = SplitStatement(statement.FalseStatement, collected, fragments);
            if (false_count == 0)
                collected.Clear();
            else
            {
                EndFragment(collected, fragments, fragments.Count + 1, true);
                // Update all the CaseGotoStatements in the true branch, to point to after the false branch
                for (int i = truelabel; i < falselabel; i++)
                    fragments[i].SelectMany(x =>
                            x.All()
                                .OfType<CaseGotoStatement>()
                                .Where(y => y.CaseLabel == falselabel)
                        )
                        .Select(x => x.CaseLabel = fragments.Count)
                        .ToList();
            }

            // Set the initial if statement to point to the two new branches
            ifs.TrueStatement = new CaseGotoStatement(truelabel, true);
            ifs.FalseStatement = new CaseGotoStatement(falselabel, true);
            ifs.UpdateParents();

            return fragments.Count - conditionlabel;
        }

        /// <summary>
        /// Splits a <see cref="WhileStatement"/> into a set of cases
        /// </summary>
        /// <returns>The number of new fragments.</returns>
        /// <param name="statement">The statement(s) to split.</param>
        /// <param name="collected">The currently collected statements.</param>
        /// <param name="fragments">The currently collected fragments.</param>
        private int SplitStatement(WhileStatement statement, List<Statement> collected, List<List<Statement>> fragments)
        {
            if (!AllBranchesHasAwait(statement.Body))
                throw new WhileWithoutAwaitException(
                    $"Cannot process a while statement without await calls in the body. Note: for loops are transformed into while loops. Note note: All branches must have an await call.");

            EndFragment(collected, fragments, fragments.Count + 1, true);
            var selflabel = fragments.Count;

            var ifs = new IfElseStatement(statement.Condition,
                statement.Body, new EmptyStatement())
                { Parent = statement.Parent };
            ifs.UpdateParents();

            var rewired = SplitStatement(ifs, collected, fragments);
            var trailfragment = fragments.Last();
            var exitlabel = fragments.Count;

            for (int i = selflabel+1; i < fragments.Count; i++)
            {
                fragments[i]
                    .SelectMany(x => x.All())
                    .OfType<CaseGotoStatement>()
                    .Select(x => x.CaseLabel = x.CaseLabel == exitlabel ? selflabel : x.CaseLabel)
                    .ToList();
            }
            ifs.FalseStatement = new CaseGotoStatement(exitlabel, true);
            ifs.UpdateParents();

            // Cannot fallthrough backwards through states. Copy state machine, where this occurs
            for (int i = selflabel; i < fragments.Count; i++)
                fragments[i] = fragments[i].Select(x =>
                    ReplaceBackGotoStatements(fragments, x, i, i)).ToList();

            return fragments.Count - selflabel;
        }

        /// <summary>
        /// Splits a <see cref="ForStatement"/> into a set of cases
        /// </summary>
        /// <returns>The number of new fragments.</returns>
        /// <param name="statement">The statement(s) to split.</param>
        /// <param name="collected">The currently collected statements.</param>
        /// <param name="fragments">The currently collected fragments.</param>
        private int SplitStatement(ForStatement statement, List<Statement> collected, List<List<Statement>> fragments)
        {
            if (!statement.All().OfType<AwaitExpression>().Any())
            {
                try
                {
                    statement.GetStaticForLoopValues();

                    // If this is a basic static loop, just treat it as a normal
                    collected.Add(statement);
                    return 0;
                }
                catch
                {
                    throw new Exception($"Cannot process a non-static for loop without await calls in the body");
                }
            }

            collected.Add(new ExpressionStatement(
                new AssignmentExpression(
                        new IdentifierExpression(statement.LoopIndex), statement.Initializer)));
            EndFragment(collected, fragments, fragments.Count + 1, true);

            var body = new List<Statement>();
            var variables = new Variable[0];

            if (statement.LoopBody is BlockStatement)
            {
                body.AddRange(((BlockStatement)statement.LoopBody).Statements);
                variables = ((BlockStatement)statement.LoopBody).Variables;
            }
            else
                body.Add(statement.LoopBody);

            // NOTE: This does not work if we support break/continue statements
            body.Add(new ExpressionStatement(statement.Increment));

            var whs = new WhileStatement(statement.Condition, new BlockStatement(body.ToArray(), variables));
            return SplitStatement(whs, collected, fragments);
        }

        /// <summary>
        /// Wraps the currently collected instructions into a list of fragments
        /// </summary>
        /// <param name="collected">The currently collected statements.</param>
        /// <param name="fragments">The currently collected fragments.</param>
        /// <param name="gotoTarget">The goto target, or -1 if no target is injected</param>
        /// <param name="fallThrough"><c>true</c> if the goto is a fallthrough element</param>
        private void EndFragment(List<Statement> collected, List<List<Statement>> fragments, int gotoTarget, bool fallThrough)
        {
            if (collected.Count > 0)
            {
                if (gotoTarget >= 0)
                    collected.Add(new CaseGotoStatement(gotoTarget, fallThrough));

                if (collected.Count > 0)
                    fragments.Add(new List<Statement>(collected));

                collected.Clear();
            }
        }


        /// <summary>
        /// Splits a sequence of statements into a set of cases
        /// </summary>
        /// <returns>The number of new fragments.</returns>
        /// <param name="statements">The statments to split</param>
        /// <param name="collected">The currently collected statements.</param>
        /// <param name="fragments">The currently collected fragments.</param>
        private int SplitStatement(IEnumerable<Statement> statements, List<Statement> collected, List<List<Statement>> fragments)
        {
            var newFragments = 0;
            foreach (var s in statements)
            {
                if (s is ExpressionStatement && ((ExpressionStatement)s).Expression is AwaitExpression)
                {
                    newFragments++;
                    EndFragment(collected, fragments, fragments.Count, false);
                }
                else
                {
                    collected.Add(s);
                }
            }

            return newFragments;
        }

        /// <summary>
        /// Combines zero or more statements into a new statement
        /// </summary>
        /// <returns>The block statement.</returns>
        /// <param name="statements">The statements to group.</param>
        private Statement ToBlockStatement(IEnumerable<Statement> statements)
        {
            if (statements == null || statements.Count() == 0)
                return new EmptyStatement();
            if (statements.Count() == 1)
                return statements.First();

            return new BlockStatement(statements.ToArray(), new Variable[0]);
        }

        /// <summary>
        /// Splits the statements into fragments.
        /// </summary>
        /// <returns>The fragments.</returns>
        /// <param name="statements">The statements to split.</param>
        private List<List<Statement>> SplitIntoFragments(IEnumerable<Statement> statements)
        {
            var fragments = new List<List<Statement>>();
            var collected = new List<Statement>();

            foreach (var s in statements)
                SplitStatement(s, collected, fragments);

            if (collected.Count > 0)
                EndFragment(collected, fragments, fragments.Count + 1, false);

            return fragments;
        }

        /// <summary>
        /// Create a list of statements, where each case is guarded by an if statement
        /// </summary>
        /// <returns>The fragments with label guards.</returns>
        /// <param name="fragments">The input fragments.</param>
        /// <param name="statelabel">The current state variable.</param>
        /// <param name="enumfields">The enum values for each state.</param>
        private List<Statement> WrapFragmentsWithLabels(List<List<Statement>> fragments, DataElement statelabel, DataElement[] enumfields)
        {
            var res = new List<Statement>();

            for (var i = 0; i < fragments.Count; i++)
            {
                res.Add(new IfElseStatement(
                    new BinaryOperatorExpression(
                        new IdentifierExpression(statelabel),
                        SyntaxKind.EqualsEqualsToken,
                        new PrimitiveExpression(enumfields[i].GetTarget().Name, enumfields[i].MSCAType)
                    )
                    { SourceResultType = enumfields[0].MSCAType.LoadType(typeof(bool)) },
                    ToBlockStatement(fragments[i]),
                    new EmptyStatement()
                ));
            }

            return res;
        }

        /// <summary>
        /// Wraps the cases in a switch statement
        /// </summary>
        /// <returns>The fragments with label guards.</returns>
        /// <param name="fragments">The input fragments.</param>
        /// <param name="statelabel">The current state variable.</param>
        /// <param name="enumfields">The enum values for each state.</param>
        private Statement CreateSwitchStatement(List<List<Statement>> fragments, DataElement statelabel, DataElement[] enumfields)
        {
            var res = new List<Tuple<Expression[], Statement[]>>();

            for (var i = 0; i < fragments.Count; i++)
            {
                res.Add(new Tuple<Expression[], Statement[]>(
                    new Expression[] { new PrimitiveExpression(enumfields[i].GetTarget().Name, enumfields[i].MSCAType) },
                    fragments[i].ToArray()
                ));
            }

            return new SwitchStatement(
                new IdentifierExpression(statelabel),
                res.ToArray()
            );
        }

        /// <summary>
        /// Removes all <see cref="CaseGotoStatement"/>'s and inserts appropriate variables updates instead
        /// </summary>
        /// <returns>The statements without goto-statemtns.</returns>
        /// <param name="statement">The statements to update.</param>
        /// <param name="currentstate">The variable for the current state.</param>
        /// <param name="nextstate">The variable for the next clock state.</param>
        /// <param name="enumfields">The enum values for each state.</param>
        private Statement ReplaceGotoWithVariables(Statement statement, DataElement currentstate, DataElement nextstate, DataElement[] enumfields)
        {
            CaseGotoStatement current;
            while ((current = statement.All().OfType<CaseGotoStatement>().FirstOrDefault()) != null)
            {
                var fallThrough = current.FallThrough && current.CaseLabel < enumfields.Length;

                current.ReplaceWith(
                    new ExpressionStatement(
                        new AssignmentExpression(
                            new IdentifierExpression(fallThrough ? currentstate : nextstate),
                            new PrimitiveExpression(enumfields[current.CaseLabel % enumfields.Length].GetTarget().Name, enumfields[0].MSCAType)
                        )
                    )
                );
            }

            return statement;
        }

        private Statement ReplaceBackGotoStatements(List<List<Statement>> fragments, Statement statement, int start, int current)
        {
            switch (statement)
            {
                case BlockStatement s:
                    s.Statements = s.Statements.Select(x =>
                        ReplaceBackGotoStatements(fragments, x, start, current))
                        .ToArray();
                    return s;
                case CaseGotoStatement s:
                    // Do not inject if it tries to go to itself
                    if (s.FallThrough && (s.CaseLabel == current || s.CaseLabel == start))
                    {
                        return new CommentStatement($"FSM_RunState := State{s.CaseLabel}") { Parent = s.Parent };
                    }
                    // Only inject when it is a back jump
                    if (s.FallThrough && s.CaseLabel < start)
                    {
                        var statements = new List<Statement>();
                        statements.Add(new CommentStatement($"FSM_RunState := State{s.CaseLabel}"));
                        statements.AddRange(fragments[s.CaseLabel].Select(x => x.Clone()));
                        var inject = ToBlockStatement(statements);
                        inject.Parent = s.Parent;
                        inject.UpdateParents();
                        inject = ReplaceBackGotoStatements(fragments, inject, start, s.CaseLabel);
                        return inject;
                    }
                    break;
                case IfElseStatement s:
                    s.TrueStatement = ReplaceBackGotoStatements(fragments, s.TrueStatement, start, current);
                    s.FalseStatement = ReplaceBackGotoStatements(fragments, s.FalseStatement, start, current);
                    return s;
                case SwitchStatement s:
                    s.Cases = s.Cases.Select(tuple =>
                    {
                        var t = new Tuple<Expression[], Statement[]>(
                            tuple.Item1,
                            tuple.Item2.Select(x =>
                                ReplaceBackGotoStatements(fragments, x, start, current))
                                .ToArray());
                        return t;
                    }).ToArray();
                    s.UpdateParents();
                    return s;
            }
            return statement;
        }


        /// <summary>
        /// Applies the transformation
        /// </summary>
        /// <returns>The transformed item.</returns>
        /// <param name="item">The item to visit.</param>
        public ASTItem Transform(ASTItem item)
        {
            return item;
            // TODO hænger igen sammen med at skulle bygge ny syntax :(
            /*if (!(item is AST.Method) || !((Method)item).IsStateMachine)
                return item;

            var method = item as AST.Method;
            if (!method.All().OfType<AwaitExpression>().Any())
                return item;

            var enumname = "FSM_" + method.Name + "_State";

            // Construct an enum type that matches the desired states
            var enumtype = new Mono.Cecil.TypeDefinition("", enumname, Mono.Cecil.TypeAttributes.Public | Mono.Cecil.TypeAttributes.AutoClass | Mono.Cecil.TypeAttributes.AnsiClass | Mono.Cecil.TypeAttributes.Sealed, method.SourceMethod.Module.ImportReference(typeof(System.Enum)))
            {
                IsSealed = true,
            };

            enumtype.DeclaringType = method.SourceMethod.DeclaringType;
            enumtype.Fields.Add(new Mono.Cecil.FieldDefinition($"value__", Mono.Cecil.FieldAttributes.Public | Mono.Cecil.FieldAttributes.SpecialName | Mono.Cecil.FieldAttributes.RTSpecialName, method.SourceMethod.Module.ImportReference(typeof(int))));

            var statenametemplate = $"{enumtype.DeclaringType.FullName}_{enumname}_State";

            var fragments = SplitIntoFragments(method.Statements);

            var statecount = fragments.Count;
            var enumfields = new Mono.Cecil.FieldDefinition[statecount];

            // Add each of the states to the type
            for (var i = 0; i < statecount; i++)
                enumtype.Fields.Add(enumfields[i] = new Mono.Cecil.FieldDefinition($"State{i}", Mono.Cecil.FieldAttributes.Public | Mono.Cecil.FieldAttributes.Static | Mono.Cecil.FieldAttributes.Literal, enumtype)
                {
                    Constant = i
                });

            // The variable being updated internally in the method
            var run_state_var = new AST.Variable("FSM_RunState", enumfields[0])
            {
                CecilType = enumtype,
                DefaultValue = 0,
                Source = new Mono.Cecil.ParameterDefinition("FSM_RunState", Mono.Cecil.ParameterAttributes.None, enumtype)
            };

            // The current state used in the state machine process
            var current_state_signal = new AST.Signal("FSM_CurrentState", enumfields[0])
            {
                CecilType = enumtype,
                DefaultValue = 0,
                Source = new Mono.Cecil.ParameterDefinition("FSM_CurrentState", Mono.Cecil.ParameterAttributes.None, enumtype)
            };

            // The next state that is propagated to
            var next_state_signal = new AST.Signal("FSM_NextState", enumfields[0])
            {
                CecilType = enumtype,
                DefaultValue = 0,
                Source = new Mono.Cecil.ParameterDefinition("FSM_NextState", Mono.Cecil.ParameterAttributes.None, enumtype)
            };

            // Construct a state-machine method, that will be rendered as a process
            var stateMachineProcess = new AST.Method()
            {
                Name = "FSM_" + method.Name + "_Method",
                Parameters = new AST.Parameter[0],
                ReturnVariable = null,
                Parent = method.Parent,
                Variables = method.AllVariables.Concat(new Variable[] { run_state_var }).ToArray(),
                AllVariables =  method.AllVariables.Concat(new Variable[] { }).ToArray(),
                IsStateMachine = true
            };

            var enumdataitems = enumfields.Select(x => new Constant(x.Name, x) { CecilType = enumtype }).ToArray();

            var isSimpleStatePossible = fragments.SelectMany(x => x.SelectMany(y => y.All().OfType<CaseGotoStatement>())).All(x => !x.FallThrough);

            List<Statement> cases;

            // If we have no fallthrough states, we build a switch
            if (isSimpleStatePossible)
            {
                cases = new List<Statement>(new[] {
                    new EmptyStatement(),
                    CreateSwitchStatement(fragments, current_state_signal, enumdataitems)
                });
            }
            // Otherwise, we build an if-based state machine
            else
            {
                cases = WrapFragmentsWithLabels(fragments, run_state_var, enumdataitems);
                cases.Insert(0, new ExpressionStatement(
                    new AssignmentExpression(
                        new IdentifierExpression(run_state_var),
                        new IdentifierExpression(current_state_signal)
                    )
                ));
            }

            stateMachineProcess.Statements = cases.ToArray();
            foreach (var v in stateMachineProcess.Statements)
            {
                v.Parent = stateMachineProcess;
                v.UpdateParents();
                ReplaceGotoWithVariables(v, run_state_var, next_state_signal, enumdataitems);
            }

            method.Statements = new Statement[] {
                new ExpressionStatement(
                    new AssignmentExpression(
                        new IdentifierExpression(current_state_signal),
                        new IdentifierExpression(next_state_signal)
                    )
                ) { Parent = method }
            };

            method.IsStateMachine = false;


            var proc = method.GetNearestParent<Process>();
            proc.Methods = proc.Methods.Concat(new[] { stateMachineProcess }).ToArray();

            // Move variables into the shared area
            proc.InternalDataElements =
                proc.InternalDataElements
                .Union(new[] {
                    current_state_signal,
                    next_state_signal
                })
                //.Union(method.AllVariables)
                .ToArray();

            proc.SharedVariables = proc.SharedVariables.Union(method.AllVariables).ToArray();
            method.Variables = new Variable[0];
            method.AllVariables = new Variable[0];

            return stateMachineProcess;
            */
        }
    }
}
