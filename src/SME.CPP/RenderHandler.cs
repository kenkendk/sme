using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using SME.AST;

namespace SME.CPP
{
    public class RenderHandler
    {
        private readonly CppTypeScope m_typeScope;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SME.CPP.RenderHandler"/> class.
        /// </summary>
        /// <param name="typeScope">The type scope to use.</param>
        public RenderHandler(CppTypeScope typeScope)
        {
            m_typeScope = typeScope;
        }

        /// <summary>
        /// Emits variable declarations, if any
        /// </summary>
        /// <returns>The variables.</returns>
        /// <param name="variables">Variables.</param>
        /// <param name="indentation">Indentation.</param>
        public IEnumerable<string> DeclareVariables(IEnumerable<Variable> variables, int indentation)
        {
            if (variables != null && variables.Any())
            {
                var indent = new string(' ', indentation);

                foreach (var n in variables)
                    yield return $"{indent}{m_typeScope.GetType(n).Name} {n.Name}{GetInitializer(n)};";

                yield return "";
            }
        }

        /// <summary>
        /// Renders all the statements in a method as VHDL
        /// </summary>
        /// <returns>The statements in the method.</returns>
        /// <param name="method">The method to render.</param>
        public IEnumerable<string> RenderMethod(AST.Method method)
        {
            if (method == null || method.Ignore)
                yield break;

            foreach (var n in DeclareVariables(method.Variables, 0))
                yield return n;

            foreach (var n in method.Statements.SelectMany(x => RenderStatement(method, x, 0)))
                yield return n;
        }

        /// <summary>
        /// Renders a single statement with the given indentation
        /// </summary>
        /// <returns>The VHDL lines in the statement.</returns>
        /// <param name="method">The method the statement belongs to.</param>
        /// <param name="statement">The statement to render.</param>
        /// <param name="indentation">The indentation to use.</param>
        private IEnumerable<string> RenderStatement(AST.Method method, AST.Statement statement, int indentation)
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
            else if (statement is AST.EmptyStatement)
                return new string[0];
            else
                throw new Exception($"Unuspported statement type: {statement.GetType().FullName}");
        }

        /// <summary>
        /// Renders a single ForStatement with the given indentation
        /// </summary>
        /// <returns>The VHDL lines in the statement.</returns>
        /// <param name="method">The method the statement belongs to.</param>
        /// <param name="s">The statement to render.</param>
        /// <param name="indentation">The indentation to use.</param>
        private IEnumerable<string> RenderStatement(AST.Method method, AST.ForStatement s, int indentation)
        {
            var endval = (int)s.EndValue.DefaultValue;

            var indent = new string(' ', indentation);

			var incr = 1;
			var defincr = s.Increment.DefaultValue;
			if (defincr is AST.Constant)
				incr = (int)((Constant)(defincr)).DefaultValue;
			else
				incr = (int)s.Increment.DefaultValue;

            yield return $"{indent}for (size_t {s.LoopIndex.Name} = {s.StartValue.DefaultValue}; {s.LoopIndex.Name} < {endval}; {s.LoopIndex.Name} += {incr}) {{";

            foreach (var n in RenderStatement(method, s.LoopBody, indentation + 4))
                yield return n;

            yield return $"{indent}}}";
        }

        /// <summary>
        /// Renders a single ReturnStatement with the given indentation
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
            yield return $"{indent}return {method.ReturnVariable.Name};";
        }

        /// <summary>
        /// Renders a single BlockStatement with the given indentation
        /// </summary>
        /// <returns>The VHDL lines in the statement.</returns>
        /// <param name="method">The method the statement belongs to.</param>
        /// <param name="s">The statement to render.</param>
        /// <param name="indentation">The indentation to use.</param>
        private IEnumerable<string> RenderStatement(AST.Method method, AST.BlockStatement s, int indentation)
        {
            foreach (var n in DeclareVariables(s.Variables, indentation))
                yield return n;

            foreach (var n in s.Statements)
                foreach (var x in RenderStatement(method, n, indentation))
                    yield return x;
        }

        /// <summary>
        /// Renders a single SwitchStatement with the given indentation
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

            yield return $"{indent}switch({RenderExpression(s.SwitchExpression)}) {{";

            var hasOthers = false;
            foreach (var c in s.Cases)
            {
                if (c.Item1.Length == 1 && c.Item1.First() is EmptyExpression)
                {
                    hasOthers = true;
                    yield return $"{indent2}default:";
                }
                else
                {
                    foreach (var cs in c.Item1.Select(x => RenderExpression(x)))
                        yield return $"{indent2}case {cs}:";
                }

                foreach (var ss in c.Item2.SelectMany(x => RenderStatement(method, x, indentation)))
                    yield return ss;

                yield return $"{indent2}break;";
            }

            if (!hasOthers)
                yield return $"{indent2}default:";

            yield return $"{indent}}}";
        }

        /// <summary>
        /// Renders a single IfElseStatement with the given indentation
        /// </summary>
        /// <returns>The VHDL lines in the statement.</returns>
        /// <param name="method">The method the statement belongs to.</param>
        /// <param name="s">The statement to render.</param>
        /// <param name="indentation">The indentation to use.</param>
        private IEnumerable<string> RenderStatement(AST.Method method, AST.IfElseStatement s, int indentation)
        {
            var indent = new string(' ', indentation);

            yield return $"{indent}if ({RenderExpression(s.Condition)}) {{";

            foreach (var e in RenderStatement(method, s.TrueStatement, indentation + 4))
                yield return e;

            if (s.FalseStatement != null && !(s.FalseStatement is EmptyStatement))
            {
                yield return $"{indent}}} else {{";
                foreach (var e in RenderStatement(method, s.FalseStatement, indentation + 4))
                    yield return e;
            }

            yield return $"{indent}}}";
        }

        /// <summary>
        /// Renders a single ExpressionStatement with the given indentation
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
        /// Renders a single CommentStatement with the given indentation
        /// </summary>
        /// <returns>The VHDL lines in the statement.</returns>
        /// <param name="method">The method the statement belongs to.</param>
        /// <param name="s">The statement to render.</param>
        /// <param name="indentation">The indentation to use.</param>
        private IEnumerable<string> RenderStatement(AST.Method method, AST.CommentStatement s, int indentation)
        {
            var indent = new string(' ', indentation);
            foreach (var c in (s.Message ?? string.Empty).Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
                yield return $"{indent}// {s.Message}";
        }


        /// <summary>
        /// Renders a single expression to VHDL
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
            else
                throw new Exception($"Unsupported expression type {expression.GetType().FullName}");
        }

        /// <summary>
        /// Renders a single ArrayCreateExpression to VHDL
        /// </summary>
        /// <returns>The VHDL equivalent of the expression.</returns>
        /// <param name="e">The expression to render</param>
        private string RenderExpression(AST.ArrayCreateExpression e)
        {
            return "{" + string.Join(", ", e.ElementExpressions.Select(x => RenderExpression(x))) + "}";
        }

        /// <summary>
        /// Renders a single EmptyArrayCreateExpression to VHDL
        /// </summary>
        /// <returns>The VHDL equivalent of the expression.</returns>
        /// <param name="e">The expression to render</param>
        private string RenderExpression(AST.EmptyArrayCreateExpression e)
        {
            return "{ }";
        }

        /// <summary>
        /// Renders a single AssignmentExpression to VHDL
        /// </summary>
        /// <returns>The VHDL equivalent of the expression.</returns>
        /// <param name="e">The expression to render</param>
        private string RenderExpression(AST.AssignmentExpression e)
        {
            DataElement target;

            if (e.Left is AST.MemberReferenceExpression)
                target = (e.Left as MemberReferenceExpression).Target;
            else if (e.Left is AST.IdentifierExpression)
                target = (e.Left as IdentifierExpression).Target;
            else if (e.Left is AST.IndexerExpression)
                target = (e.Left as IndexerExpression).Target;
            else
                throw new Exception("Unexpected assignment target");

            var prefix = string.Empty;

            if (e.Right is PrimitiveExpression && ((PrimitiveExpression)e.Right).Value == null)
                return string.Format("// {0}{1} {2} ???", prefix, RenderExpression(e.Left), "=");

            if (target.Parent is AST.Bus)
            {
                var lx = RenderExpression(e.Left);
                // Remove the trailing parenthesis
                var tg = lx.Substring(0, lx.Length - 1);

                // If we are writing a bus-array, arguments are provided as method parameters
                if (target.CecilType.IsFixedArrayType())
                    tg += ", ";

                if (e.Operator != ICSharpCode.NRefactory.CSharp.AssignmentOperatorType.Assign)
                    return string.Format("{0}{1}{2}{3})", tg, lx, e.Operator.ToBinaryOperator().ToCpp(), RenderExpression(e.Right));
                else
                    return string.Format("{0}{1})", tg, RenderExpression(e.Right));
            }
            else
            {
                return string.Format("{0} {1} {2}", RenderExpression(e.Left), e.Operator.ToCpp(), RenderExpression(e.Right));
            }
        }

        /// <summary>
        /// Renders a single ArrayCreateExpression to VHDL
        /// </summary>
        /// <returns>The VHDL equivalent of the expression.</returns>
        /// <param name="e">The expression to render</param>
        private string RenderExpression(AST.BinaryOperatorExpression e)
        {
            return string.Format("{0} {1} {2}", RenderExpression(e.Left), e.Operator.ToCpp(), RenderExpression(e.Right));
        }

        /// <summary>
        /// Renders a single CastExpression to VHDL
        /// </summary>
        /// <returns>The VHDL equivalent of the expression.</returns>
        /// <param name="e">The expression to render</param>
        private string RenderExpression(AST.CastExpression e)
        {
            return string.Format("({0}){1}", m_typeScope.GetType(e.SourceResultType).Name, RenderExpression(e.Expression));
        }

        /// <summary>
        /// Renders a single CheckedExpression to VHDL
        /// </summary>
        /// <returns>The VHDL equivalent of the expression.</returns>
        /// <param name="e">The expression to render</param>
        private string RenderExpression(AST.CheckedExpression e)
        {
            return RenderExpression(e.Expression);
        }

        /// <summary>
        /// Renders a single ConditionalExpression to VHDL
        /// </summary>
        /// <returns>The VHDL equivalent of the expression.</returns>
        /// <param name="e">The expression to render</param>
        private string RenderExpression(AST.ConditionalExpression e)
        {
            return string.Format("{0} ? {1} : {2}", RenderExpression(e.TrueExpression), RenderExpression(e.ConditionExpression), RenderExpression(e.FalseExpression));
        }

        /// <summary>
        /// Renders a single EmptyExpression to VHDL
        /// </summary>
        /// <returns>The VHDL equivalent of the expression.</returns>
        /// <param name="e">The expression to render</param>
        private string RenderExpression(AST.EmptyExpression e)
        {
            return string.Empty;
        }

        /// <summary>
        /// Renders a single IdentifierExpression to VHDL
        /// </summary>
        /// <returns>The VHDL equivalent of the expression.</returns>
        /// <param name="e">The expression to render</param>
        private string RenderExpression(AST.IdentifierExpression e)
        {
            return e.Target.Name;
        }

        /// <summary>
        /// Renders a single IndexerExpression to VHDL
        /// </summary>
        /// <returns>The VHDL equivalent of the expression.</returns>
        /// <param name="e">The expression to render</param>
        private string RenderExpression(AST.IndexerExpression e)
        {
            var res = RenderExpression(e.TargetExpression);
            if (e.Target.Parent is AST.Bus)
                return $"{res.Substring(0, res.Length - 1)}{RenderExpression(e.IndexExpression)})";
            else
                return $"{res}[{RenderExpression(e.IndexExpression)}]";
        }

        /// <summary>
        /// Renders a single InvocationExpression to VHDL
        /// </summary>
        /// <returns>The VHDL equivalent of the expression.</returns>
        /// <param name="e">The expression to render</param>
        private string RenderExpression(AST.InvocationExpression e)
        {
            return RenderExpression(e.TargetExpression) + "(" + string.Join(", ", e.ArgumentExpressions.Select(x => RenderExpression(x))) + ")";
        }

        /// <summary>
        /// Renders a single MemberReferenceExpression to VHDL
        /// </summary>
        /// <returns>The VHDL equivalent of the expression.</returns>
        /// <param name="e">The expression to render</param>
        private string RenderExpression(AST.MemberReferenceExpression e)
        {
            if (e.Target.Parent is AST.Bus)
				return $"bus_{e.Target.Parent.Name}->{e.Target.Name}()";
            
			else if (e.Target is AST.Constant)
            {
                var ce = e.Target as AST.Constant;

                if (ce.ArrayLengthSource != null)
                    return "size_" + ((Constant)e.Target).ArrayLengthSource.Name;

                if (ce.CecilType != null && ce.CecilType.Resolve().IsEnum)
                {
                    if (ce.DefaultValue is FieldDefinition)
                        return Naming.ToValidName(ce.CecilType.FullName + "." + ((FieldDefinition)ce.DefaultValue).Name);
                }
            }

            if (string.IsNullOrEmpty(e.Target.Name))
                throw new Exception($"Cannot emit empty expression: {e.SourceExpression}");

            if (e.Target.Parent is Variable && !string.IsNullOrEmpty(e.Target.Parent.Name))
                return e.Target.Parent.Name + "." + e.Target.Name;

            return e.Target.Name;
        }

        /// <summary>
        /// Renders a single MethodReferenceExpression to VHDL
        /// </summary>
        /// <returns>The VHDL equivalent of the expression.</returns>
        /// <param name="e">The expression to render</param>
        private string RenderExpression(AST.MethodReferenceExpression e)
        {
            if (string.IsNullOrEmpty(e.Target.Name))
                throw new Exception($"Cannot emit empty expression: {e.SourceExpression}");

            return e.Target.Name;
        }

        /// <summary>
        /// Renders a single ParenthesizedExpression to VHDL
        /// </summary>
        /// <returns>The VHDL equivalent of the expression.</returns>
        /// <param name="e">The expression to render</param>
        private string RenderExpression(AST.ParenthesizedExpression e)
        {
            return string.Format("({0})", RenderExpression(e.Expression));
        }

        /// <summary>
        /// Renders a single PrimitiveExpression to VHDL
        /// </summary>
        /// <returns>The VHDL equivalent of the expression.</returns>
        /// <param name="e">The expression to render</param>
        private string RenderExpression(AST.PrimitiveExpression e)
        {
            if (e.SourceResultType.IsSameTypeReference(typeof(bool)))
            {
                return ((bool)e.Value) ? "true" : "false";
            }
            else if (e.SourceResultType.Resolve().IsEnum)
            {
                return Naming.ToValidName(e.SourceResultType.FullName + "." + e.Value.ToString());
            }
            else
            {
                return e.Value.ToString();
            }
        }

        /// <summary>
        /// Renders a single UnaryOperatorExpression to VHDL
        /// </summary>
        /// <returns>The VHDL equivalent of the expression.</returns>
        /// <param name="e">The expression to render</param>
        private string RenderExpression(AST.UnaryOperatorExpression e)
        {
            return string.Format("{0} {1}", e.Operator.ToCpp(), RenderExpression(e.Operand));
        }

        /// <summary>
        /// Renders a single UncheckedExpression to VHDL
        /// </summary>
        /// <returns>The VHDL equivalent of the expression.</returns>
        /// <param name="e">The expression to render</param>
        private string RenderExpression(AST.UncheckedExpression e)
        {
            return RenderExpression(e.Expression);
        }

		/// <summary>
		/// Gets the initializer statement, if any.
		/// </summary>
		/// <returns>The initializer.</returns>
		/// <param name="element">The variable to get the initializer for.</param>
		public string GetInitializer(AST.DataElement element)
		{
			if (element.DefaultValue == null)
				return string.Empty;

			if (element.DefaultValue is AST.ArrayCreateExpression)
			{
				var asexp = (AST.ArrayCreateExpression)element.DefaultValue;

				var nae = new ArrayCreateExpression()
				{
					SourceExpression = asexp.SourceExpression,
					SourceResultType = asexp.SourceResultType,
				};

				nae.ElementExpressions = asexp.ElementExpressions
					.Select(x => new PrimitiveExpression()
					{
						SourceExpression = x.SourceExpression,
						SourceResultType = x.SourceResultType,
						Parent = nae,
						Value = ((PrimitiveExpression)x).Value
					}).Cast<Expression>().ToArray();

				return " = " + RenderExpression(nae);

			}
			else if (element.DefaultValue is ICSharpCode.NRefactory.CSharp.AstNode)
			{
				var eltype = Type.GetType(element.CecilType.FullName);
				var defaultvalue = eltype != null && element.CecilType.IsValueType ? Activator.CreateInstance(eltype) : null;

				return " = " + RenderExpression(new AST.PrimitiveExpression()
				{
					Value = defaultvalue,
					SourceResultType = element.CecilType
				});
			}
			else if (element.DefaultValue is AST.EmptyArrayCreateExpression)
			{
				var ese = element.DefaultValue as AST.EmptyArrayCreateExpression;
				return " = " + RenderExpression(new AST.EmptyArrayCreateExpression()
				{
					SizeExpression = ese.SizeExpression.Clone(),
					SourceExpression = ese.SourceExpression,
					SourceResultType = ese.SourceResultType
				});

			}
			else if (element.CecilType.IsArrayType() && element.DefaultValue == null)
			{
				return " = " + RenderExpression(new EmptyArrayCreateExpression()
				{
					SourceExpression = null,
					SourceResultType = element.CecilType,
					SizeExpression = new MemberReferenceExpression()
					{
						Name = element.Name,
						SourceExpression = null,
						SourceResultType = element.CecilType,
						Target = element
					}
				});
			}
			else
			{
				return " = " + RenderExpression(new AST.PrimitiveExpression()
				{
					Value = element.DefaultValue,
					SourceResultType = element.CecilType
				});
			}
		}
    }
}
