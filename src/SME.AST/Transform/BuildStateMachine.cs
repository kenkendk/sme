using System;
using System.Collections.Generic;
using System.Linq;

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

            // Split into cases that has await, and regular cases
            var regulars = new List<Tuple<Expression[], Statement[]>>();
            var specials = new List<Tuple<Expression[], Statement[]>>();
            foreach (var c in statement.Cases)
            {
                if (c.Item2.SelectMany(x => x.All().OfType<AwaitExpression>()).Any())
                    specials.Add(c);
                else
                    regulars.Add(c);
            }

            if (specials.Count == 0)
                throw new Exception("Unexpected number of specials");

            IfElseStatement prev = null;
            IfElseStatement first = null;
            while (specials.Count > 0)
            {
                // Build each comparison statement
                var conds = specials.First().Item1.Select(x => new BinaryOperatorExpression(
                    statement.SwitchExpression,
                    ICSharpCode.Decompiler.CSharp.Syntax.BinaryOperatorType.Equality,
                    x
                )
                { SourceResultType = statement.SwitchExpression.SourceResultType.Module.ImportReference(typeof(bool)) });

                // Then condense into a single statement
                Expression cond;
                if (specials.First().Item1.Length == 1)
                    cond = conds.First();
                else
                    cond = conds.Aggregate((a, b) => new BinaryOperatorExpression(a, ICSharpCode.Decompiler.CSharp.Syntax.BinaryOperatorType.ConditionalOr, b) { SourceResultType = statement.SwitchExpression.SourceResultType.Module.ImportReference(typeof(bool)) });

                var ifs = new IfElseStatement(cond, ToBlockStatement(specials.First().Item2.Where(x => !(x is BreakStatement))), new EmptyStatement());
                if (first == null)
                    first = ifs;                
                if (prev != null)
                    prev.FalseStatement = ifs;
                prev = ifs;
                specials.RemoveAt(0);
            }

            if (regulars.Count > 0)
            {
                prev.FalseStatement = statement;
                statement.Cases = regulars.ToArray();
            }
            first.UpdateParents();

            return SplitStatement(first, collected, fragments);
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

            var conditionlabel = fragments.Count;
            EndFragment(collected, fragments, fragments.Count + 1, true);

            var ifs = new IfElseStatement(statement.Condition, new EmptyStatement(), new EmptyStatement());
            collected.Add(ifs);

            EndFragment(collected, fragments, -1, true);

            var truelabel = fragments.Count;
            var truestatements = SplitStatement(statement.TrueStatement, collected, fragments);

            EndFragment(collected, fragments, fragments.Count + 1, true);
            var falselabel = fragments.Count;

            if (statement.FalseStatement is EmptyStatement)
            {
                // Move all targets one up, as we remove an empty case
                for (var i = truelabel; i < falselabel; i++)
                    foreach (var c in fragments[i])
                        foreach (var cx in c.All().OfType<CaseGotoStatement>())
                            cx.CaseLabel = cx.CaseLabel - 1;

                ifs.TrueStatement = ToBlockStatement(fragments[truelabel]);
                ifs.FalseStatement = new CaseGotoStatement(falselabel - 1, true);
                ifs.UpdateParents();

                fragments.RemoveAt(truelabel);
            }
            else
            {
                // Keep a list of all targets that point out of the current block
                var topatch = new List<CaseGotoStatement>();
                for (var i = truelabel; i < fragments.Count; i++)
                    foreach (var c in fragments[i])
                        foreach (var cx in c.All().OfType<CaseGotoStatement>())
                            if (cx.CaseLabel == fragments.Count)
                                topatch.Add(cx);

                var falsestatements = SplitStatement(statement.FalseStatement, collected, fragments);
                EndFragment(collected, fragments, fragments.Count + 1, true);
                var endlabel = fragments.Count;

                // Move all targets up, as we remove two fragments
                // (the inital fragments for true/false are embedded in
                //   the if-else-statement )
                for (var i = truelabel; i < fragments.Count; i++)
                    foreach (var c in fragments[i])
                        foreach (var cx in c.All().OfType<CaseGotoStatement>())
                            cx.CaseLabel = cx.CaseLabel - (i >= falselabel ? 2 : 1);

                // Fix those that point to the next instruction,
                // so they skip the false fragments
                foreach (var s in topatch)
                    s.CaseLabel = endlabel - 2;

                ifs.TrueStatement = ToBlockStatement(fragments[truelabel]);
                ifs.FalseStatement = ToBlockStatement(fragments[falselabel]);
                ifs.UpdateParents();

                fragments.RemoveAt(falselabel);
                fragments.RemoveAt(truelabel);
            }

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
                throw new WhileWithoutAwaitException($"Cannot process a while statement without await calls in the body. Note: for loops are transformed into while loops. Note note: All branches must have an await call.");

            EndFragment(collected, fragments, fragments.Count + 1, true);
            var selflabel = fragments.Count;

            var ifs = new IfElseStatement(statement.Condition, statement.Body, new EmptyStatement()) { Parent = statement.Parent };
            ifs.UpdateParents();

            var rewired = SplitStatement(ifs, collected, fragments);
            var trailfragment = fragments.Last();
            var exitlabel = fragments.Count;

            // If the while loop ends with await, we can just re-wire the last label
            if (trailfragment.Count == 1 && trailfragment.First() is IfElseStatement && ((IfElseStatement)trailfragment.First()).Condition == statement.Condition)
            {
                foreach (var s in fragments.Last().SelectMany(x => x.All().OfType<CaseGotoStatement>()).Where(x => x.CaseLabel == exitlabel && !x.FallThrough))
                    s.CaseLabel = selflabel;
            }
            else
            {
                // If we have additional code after the await, 
                // we need to move the last fragment up before the while loop,
                // so the code can fall-through, into the while loop
                foreach (var s in fragments.SelectMany(x => x).SelectMany(x => x.All().OfType<CaseGotoStatement>()))
                {
                    if (s.CaseLabel == exitlabel)
                    {
                        // Inject the loop redirect to the loop start,
                        // but don't redirect the loop exit

                        // NOTE: Does not work if the loop has continue statements
                        // which is currently not supported
                        if (s == trailfragment.Last())
                            s.CaseLabel = selflabel + 1;
                    }
                    // Point the last fragment upwards before the loop
                    else if (s.CaseLabel == fragments.Count - 1)
                        s.CaseLabel = selflabel;
                    // Point downwards, skipping the injected state
                    else if (s.CaseLabel >= selflabel)
                        s.CaseLabel = s.CaseLabel + 1;
                }
                            
                // Move the fragments around
                fragments.RemoveAt(fragments.Count - 1);
                fragments.Insert(selflabel, trailfragment);
            }

            // Cannot fallthrough backwards through states. Copy state machine, where this occurs
            for (int i = 0; i < fragments.Count; i++)
            {
                var cases = fragments[i].SelectMany(x => 
                    x.All()
                        .OfType<CaseGotoStatement>()
                        .Where(y => y.FallThrough && y.CaseLabel < i));
                while (cases.Any())
                {
                    cases = cases.SelectMany(
                        x => 
                        { // TODO crash med at parent bliver null? 
                            x.ReplaceWith(new BlockStatement(fragments[x.CaseLabel].ToArray(), null));
                            return fragments[x.CaseLabel].SelectMany(y => 
                                y.All()
                                    .OfType<CaseGotoStatement>()
                                    .Where(z => z.FallThrough && z.CaseLabel < i));
                        }
                    );
                }
            }

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
                        ICSharpCode.Decompiler.CSharp.Syntax.BinaryOperatorType.Equality,
                        new PrimitiveExpression(enumfields[i].GetTarget().Name, enumfields[i].CecilType)
                    )
                    { SourceResultType = enumfields[0].CecilType.Module.ImportReference(typeof(bool)) },
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
                    new Expression[] { new PrimitiveExpression(enumfields[i].GetTarget().Name, enumfields[i].CecilType) },
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
                            new PrimitiveExpression(enumfields[current.CaseLabel % enumfields.Length].GetTarget().Name, enumfields[0].CecilType)
                        )
                    )
                );
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
            if (!(item is AST.Method) || !((Method)item).IsStateMachine)
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
        }
    }
}
