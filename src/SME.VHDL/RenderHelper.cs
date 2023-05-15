using System;
using System.Collections.Generic;
using SME.AST;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SME.VHDL
{
    /// <summary>
    /// Helper method that contains all code to emit actual VHDL.
    /// </summary>
    public class RenderHelper
    {
        /// <summary>
        /// The parent render state instance.
        /// </summary>
        public readonly RenderState Parent;

        /// <summary>
        /// The process to use.
        /// </summary>
        public readonly AST.Process Process;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SME.VHDL.RenderHelper"/> class.
        /// </summary>
        /// <param name="parent">The <see cref="RenderState"/> instance to use.</param>
        /// <param name="process">The <see cref="AST.Process"/> instance to use</param>
        public RenderHelper(RenderState parent, AST.Process process)
        {
            Parent = parent;
            Process = process;
        }

        /// <summary>
        /// Renders all the statements in a state machine method as VHDL.
        /// </summary>
        /// <returns>The statements in the method.</returns>
        /// <param name="method">The method to render.</param>
        public IEnumerable<string> RenderStateMachine(AST.Method method, RenderStateProcess rsp)
        {
            var proc = method.GetNearestParent<SME.AST.Process>();
            var statesignal = proc.InternalDataElements[proc.InternalDataElements.Length - 2];
            var runvariable = method.Variables.Last();

            yield return $"{method.Name}: process (RST, FSM_Trigger)";
            foreach(var n in method.Variables.Union(proc.SharedVariables).Where(x => x != runvariable))
            {
                var typename = Parent.VHDLWrappedTypeName(n);
                var arraysize = n.MSCAType.IsArrayType() ? $" (0 to {((Array)n.DefaultValue).Length-1})" : "";
                yield return $"    variable {n.Name}: {typename}{arraysize} := reset_{n.Name};";
            }
            yield return $"    variable {runvariable.Name}: {Parent.VHDLWrappedTypeName(runvariable)} := {RenderExpression(new PrimitiveExpression("State0", runvariable.MSCAType))};";
            yield return $"    variable reentry_guard: std_logic := '0';";
            yield return "begin";

            // The first statement is the state reset call
            foreach (var s in method.Statements.Take(1).SelectMany(x => RenderStatement(method, x, 4)))
                yield return s;

            yield return "    if RST = '1' then";

            foreach (var bus in Process.OutputBusses.Concat(Process.InternalBusses).Distinct())
                foreach (var signal in rsp.WrittenSignals(bus))
                    foreach (var s in RenderStatement(null, Parent.GetResetStatement(signal), 8))
                        yield return s;

            foreach (var variable in method.AllVariables.Union(proc.SharedVariables).Where(x => x != statesignal))
                yield return $"        {variable.Name} := {Naming.ToValidName("reset_" + variable.Name)};";

            yield return "        FIN <= '0';";

            yield return "    elsif reentry_guard /= FSM_Trigger then";
            yield return "        reentry_guard := FSM_Trigger;";

            foreach (var s in method.Statements.Skip(1).SelectMany(x => RenderStatement(method, x, 8)))
                yield return s;

            yield return "        FIN <= RDY;";
            yield return "    end if;";

            yield return "end process;";
        }

        /// <summary>
        /// Renders all the statements in a method as VHDL.
        /// </summary>
        /// <returns>The statements in the method.</returns>
        /// <param name="method">The method to render.</param>
        public IEnumerable<string> RenderMethod(AST.Method method)
        {
            if (method == null || method.Ignore)
                yield break;

            if (method != Process.MainMethod)
            {
                var margs = string.Join("; ",
                        from n in method.Parameters
                        let inoutargstr = ((IParameterSymbol)n.Source).GetArgumentInOut(method.MSCAFlow).ToString().ToLowerInvariant()

                        select string.Format(
                            "{0}{1}: {2} {3}",
                            string.Equals(inoutargstr, "in", StringComparison.OrdinalIgnoreCase) ? "constant " : "",
                            n.Name,
                            inoutargstr,
                            ((IParameterSymbol)n.Source).GetAttribute<RangeAttribute>() != null
                                ? method.Name + "_" + n.Name + "_type"
                                : Parent.VHDLType(n).ToSafeVHDLName()
                        ));

                if (method.ReturnVariable == null || method.ReturnVariable.MSCAType.IsSameTypeReference(typeof(void)))
                    if (margs.Equals(string.Empty))
                        yield return $"procedure {method.Name} is";
                    else
                        yield return $"procedure {method.Name}({margs}) is";
                else
                    yield return $"pure function {method.Name}({margs}) return {Parent.VHDLWrappedTypeName(method.ReturnVariable)} is";

                foreach (var n in method.AllVariables)
                    if (!n.isLoopIndex)
                        yield return $"    variable {n.Name}: {Parent.VHDLWrappedTypeName(n)};";

                foreach (var m in Parent.TemporaryVariables)
                    if (m.Key == method)
                        foreach (var n in m.Value.Values)
                            yield return $"    variable {n.Name}: {Parent.VHDLWrappedTypeName(n)};";

                yield return "begin";
            }

            foreach (var s in method.Statements.SelectMany(x => RenderStatement(method, x, method == Process.MainMethod ? 0 : 4)))
                yield return s;

            if (method != Process.MainMethod)
            {
                yield return $"end {method.Name};";
            }
        }

        /// <summary>
        /// Renders a single statement with the given indentation.
        /// </summary>
        /// <returns>The VHDL lines in the statement.</returns>
        /// <param name="method">The method the statement belongs to.</param>
        /// <param name="statement">The statement to render.</param>
        /// <param name="indentation">The indentation to use.</param>
        public IEnumerable<string> RenderStatement(AST.Method method, AST.Statement statement, int indentation)
        {
            if (statement is AST.ForStatement)
                return RenderStatement(method, statement as AST.ForStatement, indentation);
            else if (statement is AST.ReturnStatement)
                return RenderStatement(method, statement as AST.ReturnStatement, indentation);
            else if (statement is AST.BlockStatement)
                return RenderStatement(method, statement as AST.BlockStatement, indentation);
            else if (statement is AST.SwitchStatement)
                return RenderStatement(method, statement as AST.SwitchStatement, indentation);
            else if (statement is AST.IfElseStatement)
                return RenderStatement(method, statement as AST.IfElseStatement, indentation);
            else if (statement is AST.ExpressionStatement)
                return RenderStatement(method, statement as AST.ExpressionStatement, indentation);
            else if (statement is AST.CommentStatement)
                return RenderStatement(method, statement as AST.CommentStatement, indentation);
            else if (statement is AST.EmptyStatement || statement is AST.LabelStatement)
                return new string[0];
            else
                throw new Exception($"Unuspported statement type: {statement.GetType().FullName}");
        }

        /// <summary>
        /// Renders a single ForStatement with the given indentation.
        /// </summary>
        /// <returns>The VHDL lines in the statement.</returns>
        /// <param name="method">The method the statement belongs to.</param>
        /// <param name="s">The statement to render.</param>
        /// <param name="indentation">The indentation to use.</param>
        private IEnumerable<string> RenderStatement(AST.Method method, AST.ForStatement s, int indentation)
        {
            var (start, endval, incr) = s.GetStaticForLoopValues();

            var indent = new string(' ', indentation);

            if (incr != 1)
                throw new Exception($"Expected the for loop to have an increment of 1, it has {incr}");

            // Fix for using variables for loop ranges, which is legal synthasizable VHDL
            // as long as the variable can be statically computed.
            var cond = s.Condition as BinaryOperatorExpression;
            var right = cond?.Right as MemberReferenceExpression;
            var target = right?.Target as Variable;
            var end = target == null ? $"{endval}" : $"to_integer({target.Name})";

            yield return $"{indent}for {s.LoopIndex.Name} in {start} to {end}-1 loop";

            foreach (var n in RenderStatement(method, s.LoopBody, indentation + 4))
                yield return n;

            yield return $"{indent}end loop;";
        }

        /// <summary>
        /// Renders a single ReturnStatement with the given indentation.
        /// </summary>
        /// <returns>The VHDL lines in the statement.</returns>
        /// <param name="method">The method the statement belongs to.</param>
        /// <param name="s">The statement to render.</param>
        /// <param name="indentation">The indentation to use.</param>
        private IEnumerable<string> RenderStatement(AST.Method method, AST.ReturnStatement s, int indentation)
        {
            if (!(s.ReturnExpression is EmptyExpression))
                throw new Exception("Expected return expression to be empty");

            var indent = new string(' ', indentation);
            yield return $"{indent}return {method.ReturnVariable?.Name};";
        }

        /// <summary>
        /// Renders a single BlockStatement with the given indentation.
        /// </summary>
        /// <returns>The VHDL lines in the statement.</returns>
        /// <param name="method">The method the statement belongs to.</param>
        /// <param name="s">The statement to render.</param>
        /// <param name="indentation">The indentation to use.</param>
        private IEnumerable<string> RenderStatement(AST.Method method, AST.BlockStatement s, int indentation)
        {
            foreach (var n in s.Statements)
                foreach (var x in RenderStatement(method, n, indentation))
                    yield return x;
        }

        /// <summary>
        /// Renders a single SwitchStatement with the given indentation.
        /// </summary>
        /// <returns>The VHDL lines in the statement.</returns>
        /// <param name="method">The method the statement belongs to.</param>
        /// <param name="s">The statement to render.</param>
        /// <param name="indentation">The indentation to use.</param>
        private IEnumerable<string> RenderStatement(AST.Method method, AST.SwitchStatement s, int indentation)
        {
            var indent = new string(' ', indentation);

            indentation += 4;
            var indent2 = new string(' ', indentation);

            indentation += 4;

            yield return $"{indent}case {RenderExpression(s.SwitchExpression)} is";

            var others = new Statement[0];
            foreach (var c in s.Cases)
            {
                if (c.Item1.Length == 1 && c.Item1.First() is EmptyExpression)
                {
                    others = c.Item2;
                    continue;
                }
                else
                {
                    yield return indent2 + "when " + string.Join(" | ", c.Item1.Select(x => RenderExpression(x))) + " =>";
                }

                foreach (var ss in c.Item2.SelectMany(x => RenderStatement(method, x, indentation)))
                    yield return ss;
            }

            yield return $"{indent2}when others =>";
            foreach (var ss in others.SelectMany(x => RenderStatement(method, x, indentation)))
                yield return ss;

            yield return $"{indent}end case;";
        }

        /// <summary>
        /// Renders a single IfElseStatement with the given indentation.
        /// </summary>
        /// <returns>The VHDL lines in the statement.</returns>
        /// <param name="method">The method the statement belongs to.</param>
        /// <param name="s">The statement to render.</param>
        /// <param name="indentation">The indentation to use.</param>
        private IEnumerable<string> RenderStatement(AST.Method method, AST.IfElseStatement s, int indentation)
        {
            var indent = new string(' ', indentation);

            yield return $"{indent}if {RenderExpression(s.Condition)} then";

            foreach (var e in RenderStatement(method, s.TrueStatement, indentation + 4))
                yield return e;

            if (s.FalseStatement != null && !(s.FalseStatement is EmptyStatement))
            {
                yield return $"{indent}else";
                foreach (var e in RenderStatement(method, s.FalseStatement, indentation + 4))
                    yield return e;
            }

            yield return $"{indent}end if;";

        }

        /// <summary>
        /// Renders a single ExpressionStatement with the given indentation.
        /// </summary>
        /// <returns>The VHDL lines in the statement.</returns>
        /// <param name="method">The method the statement belongs to.</param>
        /// <param name="s">The statement to render.</param>
        /// <param name="indentation">The indentation to use.</param>
        private IEnumerable<string> RenderStatement(AST.Method method, AST.ExpressionStatement s, int indentation)
        {
            var indent = new string(' ', indentation);
            var value = RenderExpression(s.Expression);
            if (!string.IsNullOrWhiteSpace(value))
                yield return $"{indent}{value};";
        }

        /// <summary>
        /// Renders a single CommentStatement with the given indentation.
        /// </summary>
        /// <returns>The VHDL lines in the statement.</returns>
        /// <param name="method">The method the statement belongs to.</param>
        /// <param name="s">The statement to render.</param>
        /// <param name="indentation">The indentation to use.</param>
        private IEnumerable<string> RenderStatement(AST.Method method, AST.CommentStatement s, int indentation)
        {
            var indent = new string(' ', indentation);
            foreach(var m in s.Message.Split(new[] { Environment.NewLine.ToString() }, StringSplitOptions.None))
                yield return $"{indent}-- {m}";
        }

        /// <summary>
        /// Renders a single expression to VHDL.
        /// </summary>
        /// <returns>The VHDL equivalent of the expression.</returns>
        /// <param name="expression">The expression to render</param>
        public string RenderExpression(AST.Expression expression)
        {
            if (expression is AST.ArrayCreateExpression)
                return RenderExpression(expression as ArrayCreateExpression);
            else if (expression is AST.EmptyArrayCreateExpression)
                return RenderExpression(expression as EmptyArrayCreateExpression);
            else if (expression is AST.AssignmentExpression)
                return RenderExpression(expression as AssignmentExpression);
            else if (expression is AST.BinaryOperatorExpression)
                return RenderExpression(expression as BinaryOperatorExpression);
            else if (expression is AST.CastExpression)
                return RenderExpression(expression as CastExpression);
            else if (expression is AST.CheckedExpression)
                return RenderExpression(expression as CheckedExpression);
            else if (expression is AST.ConditionalExpression)
                return RenderExpression(expression as ConditionalExpression);
            else if (expression is AST.DirectionExpression)
                return RenderExpression(expression as DirectionExpression);
            else if (expression is AST.EmptyExpression)
                return RenderExpression(expression as EmptyExpression);
            else if (expression is AST.IdentifierExpression)
                return RenderExpression(expression as IdentifierExpression);
            else if (expression is AST.IndexerExpression)
                return RenderExpression(expression as IndexerExpression);
            else if (expression is AST.InvocationExpression)
                return RenderExpression(expression as InvocationExpression);
            else if (expression is AST.MemberReferenceExpression)
                return RenderExpression(expression as MemberReferenceExpression);
            else if (expression is AST.MethodReferenceExpression)
                return RenderExpression(expression as MethodReferenceExpression);
            else if (expression is AST.ParenthesizedExpression)
                return RenderExpression(expression as ParenthesizedExpression);
            else if (expression is AST.PrimitiveExpression)
                return RenderExpression(expression as PrimitiveExpression);
            else if (expression is AST.UnaryOperatorExpression)
                return RenderExpression(expression as UnaryOperatorExpression);
            else if (expression is AST.UncheckedExpression)
                return RenderExpression(expression as UncheckedExpression);
            else if (expression is SME.VHDL.CustomNodes.ConversionExpression)
                return RenderExpression(expression as SME.VHDL.CustomNodes.ConversionExpression);
            else
                throw new Exception($"Unsupported expression type {expression.GetType().FullName}");
        }

        /// <summary>
        /// Helper function that digs out the primitive expression underneath casting and formatting expressions.
        /// </summary>
        /// <returns>The primitive expression or null.</returns>
        /// <param name="e">The expression to unwrap.</param>
        private static AST.PrimitiveExpression UnwrapPrimitive(AST.Expression e)
        {
            if (e is CustomNodes.ConversionExpression)
                e = ((CustomNodes.ConversionExpression)e).Expression;
            if (e is AST.PrimitiveExpression)
                return (AST.PrimitiveExpression)e;

            return null;
        }

        /// <summary>
        /// Renders a single ArrayCreateExpression to VHDL.
        /// </summary>
        /// <returns>The VHDL equivalent of the expression.</returns>
        /// <param name="e">The expression to render.</param>
        private string RenderExpression(AST.ArrayCreateExpression e)
        {
            var last = UnwrapPrimitive(e.ElementExpressions.LastOrDefault());
            if (last != null && last.Value != null)
            {
                var trailing_defaults = e.ElementExpressions.Reverse().TakeWhile(x => last.Value.Equals(UnwrapPrimitive(x)?.Value)).Count();
                if (trailing_defaults != 1)
                    return "(" + string.Join(", ",
                                             e.ElementExpressions
                                             .Take(e.ElementExpressions.Length - trailing_defaults)
                                             .Select(x => RenderExpression(x))
                                             .Concat(new[] { "others => " + RenderExpression(e.ElementExpressions.Last()) })
                                            ) + ")";

            }

            return "(" + string.Join(", ", e.ElementExpressions.Select(x => RenderExpression(x))) + ")";
        }

        /// <summary>
        /// Renders a single EmptyArrayCreateExpression to VHDL.
        /// </summary>
        /// <returns>The VHDL equivalent of the expression.</returns>
        /// <param name="e">The expression to render.</param>
        private string RenderExpression(AST.EmptyArrayCreateExpression e)
        {
            var tvhdl = Parent.VHDLType(e);
            var res = "{0}";
            if (tvhdl.IsArray)
            {
                res = "(others => " + res + ")";
                tvhdl = Parent.TypeScope.GetByName(tvhdl.ElementName);
            }
            if (tvhdl.IsStdLogicVector)
            {
                res = "(others => " + res + ")";
                tvhdl = Parent.TypeScope.GetByName(tvhdl.ElementName);
            }

            if (tvhdl.IsStdLogic)
                res = string.Format(res, "'0'");
            else if (tvhdl.IsSigned || tvhdl.IsUnsigned)
                res = string.Format(res, tvhdl.ToSafeVHDLName().Substring("T_".Length) + "(0)");

            return res;
        }

        /// <summary>
        /// Renders a single AssignmentExpression to VHDL.
        /// </summary>
        /// <returns>The VHDL equivalent of the expression.</returns>
        /// <param name="e">The expression to render.</param>
        private string RenderExpression(AST.AssignmentExpression e)
        {
            ASTItem target;

            if (e.Left is AST.MemberReferenceExpression)
                target = (e.Left as MemberReferenceExpression).Target;
            else if (e.Left is AST.IdentifierExpression)
                target = (e.Left as IdentifierExpression).Target;
            else if (e.Left is AST.IndexerExpression)
                target = (e.Left as IndexerExpression).Target;
            else
                throw new Exception("Unexpected assignment target");

            var prefix = string.Empty;

            if (target.Parent is AST.Bus)
            {
                var pbus = target.Parent as AST.Bus;
                if (Process.InputBusses.Contains(pbus) && Process.OutputBusses.Contains(pbus))
                    prefix = "out_";
                //else if (!Process.IsClocked && (pbus.IsClocked || pbus.IsInternal))
                //    prefix = "next_";
            }

            if (e.Right is PrimitiveExpression && ((PrimitiveExpression)e.Right).Value == null)
                return string.Format("--{0}{1} {2} ???", prefix, RenderExpression(e.Left), target is Signal ? "<=" : ":=");

            return string.Format("{0}{1} {2} {3}", prefix, RenderExpression(e.Left), target is Signal ? "<=" : ":=", RenderExpression(e.Right));
        }

        /// <summary>
        /// Renders a single ArrayCreateExpression to VHDL.
        /// </summary>
        /// <returns>The VHDL equivalent of the expression.</returns>
        /// <param name="e">The expression to render.</param>
        private string RenderExpression(AST.BinaryOperatorExpression e)
        {
            if (Parent.Config.AVOID_SLL_AND_SRL)
            {
                if (e.Operator == SyntaxKind.LessThanLessThanToken)
                    return string.Format("shift_left({0}, {1})", RenderExpression(e.Left), RenderExpression(e.Right));
                else if (e.Operator == SyntaxKind.GreaterThanGreaterThanToken)
                    return string.Format("shift_right({0}, {1})", RenderExpression(e.Left), RenderExpression(e.Right));
            }

            return string.Format("{0} {1} {2}", RenderExpression(e.Left), e.Operator.ToVHDL(), RenderExpression(e.Right));
        }

        /// <summary>
        /// Renders a single CastExpression to VHDL.
        /// </summary>
        /// <returns>The VHDL equivalent of the expression.</returns>
        /// <param name="e">The expression to render.</param>
        private string RenderExpression(AST.CastExpression e)
        {
            throw new Exception("All cast expressions should be removed");
        }

        /// <summary>
        /// Renders a single CheckedExpression to VHDL.
        /// </summary>
        /// <returns>The VHDL equivalent of the expression.</returns>
        /// <param name="e">The expression to render.</param>
        private string RenderExpression(AST.CheckedExpression e)
        {
            return RenderExpression(e.Expression);
        }

        /// <summary>
        /// Renders a single ConditionalExpression to VHDL.
        /// </summary>
        /// <returns>The VHDL equivalent of the expression.</returns>
        /// <param name="e">The expression to render.</param>
        private string RenderExpression(AST.ConditionalExpression e)
        {
            if (!Parent.Config.SUPPORTS_VHDL_2008)
                throw new Exception("Unexpected conditional found when the output is not VHDL 2008 compatible");

            return string.Format("{0} when {1} else {2}", RenderExpression(e.TrueExpression), RenderExpression(e.ConditionExpression), RenderExpression(e.FalseExpression));
        }

        /// <summary>
        /// Renders a single EmptyExpression to VHDL.
        /// </summary>
        /// <returns>The VHDL equivalent of the expression.</returns>
        /// <param name="e">The expression to render.</param>
        private string RenderExpression(AST.EmptyExpression e)
        {
            return string.Empty;
        }

        /// <summary>
        /// Renders a single IdentifierExpression to VHDL.
        /// </summary>
        /// <returns>The VHDL equivalent of the expression.</returns>
        /// <param name="e">The expression to render.</param>
        private string RenderExpression(AST.IdentifierExpression e)
        {
            return e.Target.Name;
        }

        /// <summary>
        /// Renders a single IndexerExpression to VHDL.
        /// </summary>
        /// <returns>The VHDL equivalent of the expression.</returns>
        /// <param name="e">The expression to render.</param>
        private string RenderExpression(AST.IndexerExpression e)
        {
            return string.Format("{0}({1})", RenderExpression(e.TargetExpression), RenderExpression(e.IndexExpression));
        }

        /// <summary>
        /// Renders a single InvocationExpression to VHDL.
        /// </summary>
        /// <returns>The VHDL equivalent of the expression.</returns>
        /// <param name="e">The expression to render.</param>
        private string RenderExpression(AST.InvocationExpression e)
        {
            var method = RenderExpression(e.TargetExpression);
            var args = e.ArgumentExpressions.Select(x => RenderExpression(x));
            if (args.Count() == 0)
                return method;
            else
                return $"{method}({string.Join(", ", args)})";
        }

        /// <summary>
        /// Renders a single MemberReferenceExpression to VHDL.
        /// </summary>
        /// <returns>The VHDL equivalent of the expression.</returns>
        /// <param name="e">The expression to render.</param>
        private string RenderExpression(AST.MemberReferenceExpression e)
        {
            if (e.Target.Parent is AST.Bus)
            {
                var bus = e.Target.Parent as AST.Bus;
                var busname = e.Target.Parent.Name;
                if (Process != null && Process.LocalBusNames.ContainsKey(bus))
                    busname = Process.LocalBusNames[bus];

                var index_render = "";
                if (e.TargetExpression is IndexerExpression)
                    index_render = $"({RenderExpression((e.TargetExpression as IndexerExpression).IndexExpression)})";

                if (Process.IsClocked && Process.Methods.Any(x => x.IsStateMachine) && Process.InputBusses.Contains(bus))
                    return Naming.ToValidName("capture_" + busname + "_" + e.Target.Name) + index_render;

                return Naming.ToValidName(busname + "_" + e.Target.Name) + index_render;

            }
            else if (e.Target is AST.Constant)
            {
                var ce = e.Target as AST.Constant;

                if (ce.ArrayLengthSource != null)
                {
                    var p = ce.ArrayLengthSource.Parent;
                    while (p != null && !(p is AST.Process))
                        p = p.Parent;

                    if (p is AST.Process)
                        return ce.ArrayLengthSource.Name + "'LENGTH";

                    return ce.ArrayLengthSource.Name + "_type'LENGTH";
                }

                if (ce.MSCAType != null && ((INamedTypeSymbol)ce.MSCAType).EnumUnderlyingType != null)
                {
                    if (ce.DefaultValue is IFieldSymbol)
                        return Naming.ToValidName(ce.MSCAType.ToDisplayString() + "_" + ((IFieldSymbol)ce.DefaultValue).Name);
                }
            }

            if (string.IsNullOrEmpty(e.Target.Name))
                throw new Exception($"Cannot emit empty expression: {e.SourceExpression}");

            if (e.Target.Parent is Variable && !string.IsNullOrEmpty(e.Target.Parent.Name))
                return e.Target.Parent.Name + "." + e.Target.Name;

            return Naming.ToValidName(e.Target.Name);
        }

        /// <summary>
        /// Renders a single MethodReferenceExpression to VHDL.
        /// </summary>
        /// <returns>The VHDL equivalent of the expression.</returns>
        /// <param name="e">The expression to render.</param>
        private string RenderExpression(AST.MethodReferenceExpression e)
        {
            if (string.IsNullOrEmpty(e.Target.Name))
                throw new Exception($"Cannot emit empty expression: {e.SourceExpression}");

            return e.Target.Name;
        }

        /// <summary>
        /// Renders a single ParenthesizedExpression to VHDL.
        /// </summary>
        /// <returns>The VHDL equivalent of the expression.</returns>
        /// <param name="e">The expression to render.</param>
        private string RenderExpression(AST.ParenthesizedExpression e)
        {
            return string.Format("({0})", RenderExpression(e.Expression));
        }

        /// <summary>
        /// Renders a single DirectionExpression to VHDL.
        /// </summary>
        /// <returns>The VHDL equivalent of the expression.</returns>
        /// <param name="e">The expression to render.</param>
        private string RenderExpression(AST.DirectionExpression e)
        {
            // Currently just discard the direction, as it should be handled
            // by the function signature in VHDL.
            return RenderExpression(e.Expression);
        }

        /// <summary>
        /// Renders a single PrimitiveExpression to VHDL
        /// </summary>
        /// <returns>The VHDL equivalent of the expression.</returns>
        /// <param name="e">The expression to render.</param>
        private string RenderExpression(AST.PrimitiveExpression e)
        {
            var tvhdl = Parent.VHDLType(e);
            if (tvhdl == VHDLTypes.BOOL)
            {
                return ((bool)e.Value) ? "true" : "false";
            }
            else if (tvhdl == VHDLTypes.SYSTEM_BOOL)
            {
                return ((bool)e.Value) ? "'1'" : "'0'";
            }
            else if (((INamedTypeSymbol)e.SourceResultType).EnumUnderlyingType != null)
            {
                if (e.Value is string)
                    return Naming.ToValidName(e.SourceResultType.ToDisplayString() + "_" + e.Value.ToString());
                else
                    return Naming.ToValidName(e.SourceResultType.ToDisplayString()) + "'VAL(" + e.Value.ToString() + ")";
            }
            else
            {
                return VHDLTypeConversion.GetPrimitiveLiteral(e, tvhdl, Parent);
            }
        }

        /// <summary>
        /// Renders a single UnaryOperatorExpression to VHDL.
        /// </summary>
        /// <returns>The VHDL equivalent of the expression.</returns>
        /// <param name="e">The expression to render.</param>
        private string RenderExpression(AST.UnaryOperatorExpression e)
        {
            return string.Format("{0} {1}", e.Operator.ToVHDL(), RenderExpression(e.Operand));
        }

        /// <summary>
        /// Renders a single UncheckedExpression to VHDL.
        /// </summary>
        /// <returns>The VHDL equivalent of the expression.</returns>
        /// <param name="e">The expression to render.</param>
        private string RenderExpression(AST.UncheckedExpression e)
        {
            return RenderExpression(e.Expression);
        }

        /// <summary>
        /// Renders a single ConversionExpression to VHDL.
        /// </summary>
        /// <returns>The VHDL equivalent of the expression.</returns>
        /// <param name="e">The expression to render.</param>
        private string RenderExpression(SME.VHDL.CustomNodes.ConversionExpression e)
        {
            string innerexp;
            if (e.Expression is PrimitiveExpression)
                innerexp = VHDLTypeConversion.GetPrimitiveLiteral(e.Expression, Parent.TypeLookup[e], Parent);
            else
                innerexp = RenderExpression(e.Expression);
            return string.Format(e.WrappingTemplate, innerexp);
        }

        /// <summary>
        /// Gets all the custom type definitions for this process.
        /// </summary>
        public IEnumerable<string> TypeDefinitions
        {
            get
            {
                var methods = new[] { Process.MainMethod }.Union(Process.Methods ?? new Method[0]).Where(x => x != null && !x.Ignore);

                var allitems = Process
                    .SharedSignals
                    .OfType<DataElement>()
                    .Concat(
                        Process
                        .SharedVariables
                        .OfType<DataElement>()
                    )
                    .Concat(
                        methods
                        .SelectMany(x => x
                                    .Parameters
                                    .OfType<DataElement>()
                                    .Concat(
                                        x
                                        .AllVariables
                                        .OfType<DataElement>()
                                       )
                                    .Concat(
                                        new DataElement[] { x.ReturnVariable }.Where(y => y != null)
                                       )
                                   )
                    )
                    .Concat(
                        Process.SharedConstants
                        .OfType<DataElement>()
                    )
                    .Distinct();


                foreach (var v in allitems)
                {
                    if (v is AST.Parameter)
                        continue;

                    if (v.MSCAType.IsArrayType())
                    {
                        int arraylen;
                        if (v.DefaultValue is EmptyArrayCreateExpression)
                        {
                            var az = ((EmptyArrayCreateExpression)v.DefaultValue).SizeExpression;
                            if (az is PrimitiveExpression)
                                arraylen = (int)((PrimitiveExpression)az).Value;
                            else
                                throw new Exception($"Unable to figure out what length to assign {v.Name} from {az.SourceExpression}");
                        }
                        else if (v.DefaultValue is ArrayCreateExpression)
                            arraylen = ((ArrayCreateExpression)v.DefaultValue).ElementExpressions.Length;
                        else if (v.DefaultValue is Array)
                            arraylen = ((Array)v.DefaultValue).Length;
                        else
                        {
                            Console.WriteLine($"Unable to find variable for {v.Name}, ignoring");
                            continue;
                        }

                        var tvhdl = Parent.VHDLType(v);
                        var telvhdl = Parent.TypeScope.GetByName(tvhdl.ElementName);

                        if (tvhdl.IsSystemType || (tvhdl.IsArray && telvhdl.IsSystemType))
                            yield return $"subtype {Process.Name}_{v.Name}_type is {tvhdl.ToSafeVHDLName()}";
                        else
                            yield return $"type {Process.Name}_{v.Name}_type is array(natural range <>) of {telvhdl.ToSafeVHDLName()}";
                    }
                }
            }
        }
    }
}
