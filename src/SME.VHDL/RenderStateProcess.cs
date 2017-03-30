using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using SME.AST;

namespace SME.VHDL
{
	/// <summary>
	/// The render state for a process
	/// </summary>
	public class RenderStateProcess
	{
		/// <summary>
		/// The parent render state
		/// </summary>
		public readonly RenderState Parent;

		/// <summary>
		/// The process used in this render state
		/// </summary>
		public readonly AST.Process Process;


		/// <summary>
		/// A lookup associating an AST node with a VHDL type
		/// </summary>
		public readonly Dictionary<ASTItem, VHDLType> TypeLookup;

		/// <summary>
		/// A list of type conversion strings used to output the right wrapping for expressions
		/// </summary>
		public readonly Dictionary<Expression, string> ConversionTemplates = new Dictionary<Expression, string>();

		/// <summary>
		/// The type scope used to resolve VHDL types
		/// </summary>
		public readonly VHDLTypeScope TypeScope;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SME.VHDL.RenderStateProcess"/> class.
		/// </summary>
		/// <param name="parent">The parent render state.</param>
		/// <param name="process">The process to render.</param>
		public RenderStateProcess(RenderState parent, AST.Process process)
		{
			Parent = parent;
			Process = process;
			TypeLookup = parent.TypeLookup;
			TypeScope = parent.TypeScope;
		}

		/// <summary>
		/// Returns the VHDL type for a data element
		/// </summary>
		/// <returns>The VHDL type.</returns>
		/// <param name="element">The element to get the type for.</param>
		public VHDLType VHDLType(AST.DataElement element)
		{
			return Parent.VHDLType(element);
		}

		/// <summary>
		/// Returns the VHDL type for an expression
		/// </summary>
		/// <returns>The VHDL type.</returns>
		/// <param name="element">The expression to get the type for.</param>
		public VHDLType VHDLType(AST.Expression element)
		{
			return Parent.VHDLType(element);
		}

		/// <summary>
		/// Gets the default value for an item, expressed as a VHDL expression
		/// </summary>
		/// <returns>The default value.</returns>
		/// <param name="element">The element to get the default value for.</param>
		public string DefaultValue(AST.DataElement element)
		{
			return Parent.DefaultValue(element);
		}

		/// <summary>
		/// Returns all signals written to a bus from within this process
		/// </summary>
		/// <returns>The written signals.</returns>
		/// <param name="bus">The bus to get the signals for.</param>
		public IEnumerable<BusSignal> WrittenSignals(AST.Bus bus)
		{
			return Parent.WrittenSignals(Process, bus);
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
					.Union(
						Process
						.SharedVariables
						.OfType<DataElement>()
					)
					.Union(
						methods
						.SelectMany(x => x
									.Parameters
									.OfType<DataElement>()
									.Union(
										x
										.Variables
										.OfType<DataElement>()
									   )
						            .Union(
							            new DataElement[] { x.ReturnVariable }.Where(y => y != null)
						           	)
								   )
						
					)
					.Distinct();


				foreach (var v in allitems)
				{
					if (v is AST.Parameter)
						continue;
					
					if (v.CecilType.IsArrayType())
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
						else
						{
							Console.WriteLine($"Unable to find variable for {v.Name}, ignoring");
							continue;
						}

						var tvhdl = VHDLType(v);
						if (tvhdl.IsSystemType || (tvhdl.IsArray && TypeScope.GetByName(tvhdl.ElementName).IsSystemType))
							yield return $"subtype {v.Name}_type is {VHDLType(v)}(0 to {arraylen - 1})";
						else
							yield return $"type {v.Name}_type is array(0 to {arraylen - 1}) of {VHDLType(v).ElementName}";
					}
				}
			}
		}

		/// <summary>
		/// Gets a value indicating if the process is a component
		/// </summary>
		public bool IsComponent
		{
			get { return Process.SourceInstance is IVHDLComponent; }
		}

		/// <summary>
		/// Gets the signals in the component
		/// </summary>
		public string ComponentSignals
		{
			get
			{
				if (!IsComponent)
					return null;

				return (Process.SourceInstance as IVHDLComponent).SignalRegion(Process.Name, 2);
			}
		}

		/// <summary>
		/// Gets the processes in the component
		/// </summary>
		public string ComponentProcesses
		{
			get
			{
				if (!IsComponent)
					return null;

				return (Process.SourceInstance as IVHDLComponent).ProcessRegion(Process.Name, 4);
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

			if (method != Process.MainMethod)
			{
				var margs = string.Join("; ",
						from n in method.Parameters
                        let inoutargstr = ((ParameterDefinition)n.Source).GetArgumentInOut().ToString().ToLowerInvariant()

						select string.Format(
							"{0}{1}: {2} {3}",
							string.Equals(inoutargstr, "in", StringComparison.OrdinalIgnoreCase) ? "constant " : "",
							n.Name,
							inoutargstr,
							((ParameterDefinition)n.Source).GetAttribute<RangeAttribute>() != null
								? method.Name + "_" + n.Name + "_type"
		                        : Parent.VHDLType(n).ToSafeVHDLName()
						));

				if (method.ReturnVariable == null || method.ReturnVariable.CecilType.IsSameTypeReference(typeof(void)))
					yield return $"procedure {method.Name}({margs}) is";
				else
					yield return $"pure function {method.Name}({margs}) return {Parent.VHDLWrappedTypeName(method.ReturnVariable)} is";

				foreach (var n in method.Variables)
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
			endval--;

			// TODO: The loop variable is a special scope that is not honored here,
			// and could cause issue if there is another variable with the same name
			// it should not happen, as NRecatory assigns new names

			var indent = new string(' ', indentation);
			yield return $"{indent}for {s.LoopIndex.Name} in {s.StartValue.DefaultValue} to {endval} loop";

			var incr = 1;
			var defincr = s.Increment.DefaultValue;
			if (defincr is AST.Constant)
				incr = (int)((Constant)(defincr)).DefaultValue;
			else
				incr = (int)s.Increment.DefaultValue;

			if (incr != 1)
				throw new Exception($"Expected the for loop to have an increment of 1, it has {incr}");
			
			foreach (var n in RenderStatement(method, s.LoopBody, indentation + 4))
					yield return n;

				yield return $"{indent}end loop;";
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

			yield return $"{indent}case {RenderExpression(s.SwitchExpression)} is";

			var hasOthers = false;
			foreach (var c in s.Cases)
			{
				if (c.Item1.Length == 1 && c.Item1.First() is EmptyExpression)
				{
					hasOthers = true;
					yield return $"{indent2}when others =>";
				}
				else
				{
					yield return indent2 + "when " + string.Join(" | ", c.Item1.Select(x => RenderExpression(x))) + " =>";
				}

				foreach (var ss in c.Item2.SelectMany(x => RenderStatement(method, x, indentation)))
					yield return ss;
			}

			if (!hasOthers)
				yield return $"{indent2}when others =>";

			yield return $"{indent}end case;";
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
			yield return $"{indent}-- {s.Message}";
		}

		/// <summary>
		/// Renders a single expression to VHDL
		/// </summary>
		/// <returns>The VHDL equivalent of the expression.</returns>
		/// <param name="expression">The expression to render</param>
		private string RenderExpression(AST.Expression expression)
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
			else if (expression is SME.VHDL.CustomNodes.ConversionExpression)
				return RenderExpression(expression as SME.VHDL.CustomNodes.ConversionExpression);
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
			return "(" + string.Join(", ", e.ElementExpressions.Select(x => RenderExpression(x))) + ")";
		}

		/// <summary>
		/// Renders a single EmptyArrayCreateExpression to VHDL
		/// </summary>
		/// <returns>The VHDL equivalent of the expression.</returns>
		/// <param name="e">The expression to render</param>
		private string RenderExpression(AST.EmptyArrayCreateExpression e)
		{
			var tvhdl = VHDLType(e);
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
			else if (tvhdl.IsNumeric)
				res = string.Format(res, "0");

			return res;
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

			if (target.Parent is AST.Bus)
			{
				var pbus = target.Parent as AST.Bus;
				if (Process.InputBusses.Contains(pbus) && Process.OutputBusses.Contains(pbus))
					prefix = "out_";
				else if (!Process.IsClocked && (pbus.IsClocked || pbus.IsInternal))
					prefix = "next_";
			}

			if (e.Right is PrimitiveExpression && ((PrimitiveExpression)e.Right).Value == null)
				return string.Format("--{0}{1} {2} ???", prefix, RenderExpression(e.Left), target is Signal ? "<=" : ":=");
			
			return string.Format("{0}{1} {2} {3}", prefix, RenderExpression(e.Left), target is Signal ? "<=" : ":=" , RenderExpression(e.Right));
		}

		/// <summary>
		/// Renders a single ArrayCreateExpression to VHDL
		/// </summary>
		/// <returns>The VHDL equivalent of the expression.</returns>
		/// <param name="e">The expression to render</param>
		private string RenderExpression(AST.BinaryOperatorExpression e)
		{
			if (Parent.AVOID_SLL_AND_SRL)
			{
				if (e.Operator == ICSharpCode.NRefactory.CSharp.BinaryOperatorType.ShiftLeft)
					return string.Format("shift_left({0}, {1})", RenderExpression(e.Left), RenderExpression(e.Right));
				else if (e.Operator == ICSharpCode.NRefactory.CSharp.BinaryOperatorType.ShiftRight)
					return string.Format("shift_right({0}, {1})", RenderExpression(e.Left), RenderExpression(e.Right));
			}

			return string.Format("{0} {1} {2}", RenderExpression(e.Left), e.Operator.ToVHDL(), RenderExpression(e.Right));
		}

		/// <summary>
		/// Renders a single CastExpression to VHDL
		/// </summary>
		/// <returns>The VHDL equivalent of the expression.</returns>
		/// <param name="e">The expression to render</param>
		private string RenderExpression(AST.CastExpression e)
		{
			throw new Exception("All cast expressions should be removed");
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
			if (!Parent.SUPPORTS_VHDL_2008)
				throw new Exception("Unexpected conditional found when the output is not VHDL 2008 compatible");
			
			return string.Format("{0} when {1} else {2}", RenderExpression(e.TrueExpression), RenderExpression(e.ConditionExpression), RenderExpression(e.FalseExpression));
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
			return string.Format("{0}({1})", RenderExpression(e.TargetExpression), RenderExpression(e.IndexExpression));
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
				return e.Target.Parent.Name + "_" + e.Target.Name;
			else if (e.Target is AST.Constant)
			{
				var ce = e.Target as AST.Constant;

				if (ce.ArrayLengthSource != null)
					return ((Constant)e.Target).ArrayLengthSource.Name + "_type'LENGTH";

				if (ce.CecilType != null && ce.CecilType.Resolve().IsEnum)
				{
					if (ce.DefaultValue is FieldDefinition)
						return Naming.ToValidName(ce.CecilType.FullName + "_" + ((FieldDefinition)ce.DefaultValue).Name);
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
			var tvhdl = VHDLType(e);
			if (tvhdl == VHDLTypes.BOOL)
			{
				return ((bool)e.Value) ? "true" : "false";
			}
			else if (tvhdl == VHDLTypes.SYSTEM_BOOL)
			{
				return ((bool)e.Value) ? "'1'" : "'0'";
			}
			else if (e.SourceResultType.Resolve().IsEnum)
			{
				return Naming.ToValidName(e.SourceResultType.FullName + "_" + e.Value.ToString());
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
			return string.Format("{0} {1}", e.Operator.ToVHDL(), RenderExpression(e.Operand));
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
		/// Renders a single ConversionExpression to VHDL
		/// </summary>
		/// <returns>The VHDL equivalent of the expression.</returns>
		/// <param name="e">The expression to render</param>
		private string RenderExpression(SME.VHDL.CustomNodes.ConversionExpression e)
		{			
			if (e.Expression is PrimitiveExpression)
			{
				var tvhdl = Parent.VHDLType(e);
				if (tvhdl.IsStdLogicVector)
				{
					var pe = e.Expression as PrimitiveExpression;
					string binstr = null;
					if (pe.Value is ulong)
					{
						var uvalue = (ulong)pe.Value;
						if (uvalue > int.MaxValue)
						{
							binstr =
								Convert.ToString((int)((uvalue >> 32) & 0xffffffff), 2).PadLeft(32, '0') +
								Convert.ToString((int)(uvalue & 0xffffffff), 2).PadLeft(32, '0');
						}
					}
					else if (pe.Value is long)
					{
						var lvalue = (long)pe.Value;
						if (lvalue > int.MaxValue || lvalue < int.MinValue)
						{
							binstr =
								Convert.ToString((int)((lvalue >> 32) & 0xffffffff), 2).PadLeft(32, '0') +
								Convert.ToString((int)(lvalue & 0xffffffff), 2).PadLeft(32, '0');
						}
					}
					else if (pe.Value is uint)
					{
						var uvalue = (uint)pe.Value;
						if (uvalue > int.MaxValue)
						{
							binstr = Convert.ToString((uint)(uvalue & 0xffffffff), 2).PadLeft(32, '0');
						}
					}
					else if (pe.Value is int)
					{
						var ivalue = (int)pe.Value;
						if (tvhdl.IsUnsigned && ivalue < 0)
						{
							binstr = Convert.ToString((int)(ivalue & 0xffffffff), 2).PadLeft(32, '0');
						}
					}

					if (!string.IsNullOrWhiteSpace(binstr))
					{
						if (tvhdl.Length > 0)
							binstr = binstr.PadLeft(tvhdl.Length, '0');
						
						return string.Format("STD_LOGIC_VECTOR'(\"{0}\")", binstr);
					}
				}
			}

			return string.Format(e.WrappingTemplate, RenderExpression(e.Expression));
		}

		/// <summary>
		/// Creates a reset statement for the specified data element
		/// </summary>
		/// <returns>The statement expressing reset of the date element.</returns>
		/// <param name="element">The target element</param>
		private AST.Statement GetResetStatement(DataElement element)
		{
			var exp = new AST.AssignmentExpression()
			{
				Left = new MemberReferenceExpression()
				{
					Name = element.Name,
					Target = element,
					SourceResultType = element.CecilType
				}
			};

			var tvhdl = VHDLType(exp.Left);

			var res = new AST.ExpressionStatement()
			{
				Expression = exp
			};
			exp.Parent = res;

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

				var elvhdl = Parent.TypeScope.GetByName(tvhdl.ElementName);

				for (var i = 0; i < nae.ElementExpressions.Length; i++)
					VHDLTypeConversion.ConvertExpression(Parent, null, nae.ElementExpressions[i], elvhdl, nae.ElementExpressions[i].SourceResultType, false);

				exp.Right = nae;
				Parent.TypeLookup[nae] = tvhdl;
			}
			else if (element.DefaultValue is ICSharpCode.NRefactory.CSharp.AstNode)
			{
				var eltype = Type.GetType(element.CecilType.FullName);
				var defaultvalue = eltype != null && element.CecilType.IsValueType ? Activator.CreateInstance(eltype) : null;

				exp.Right = new AST.PrimitiveExpression()
				{
					Value = defaultvalue,
					Parent = exp,
					SourceResultType = element.CecilType
				};
			}
			else if (element.DefaultValue is AST.EmptyArrayCreateExpression)
			{
				var ese = element.DefaultValue as AST.EmptyArrayCreateExpression;
				exp.Right = new AST.EmptyArrayCreateExpression()
				{
					Parent = exp,
					SizeExpression = ese.SizeExpression.Clone(),
					SourceExpression = ese.SourceExpression,
					SourceResultType = ese.SourceResultType
				};

				Parent.TypeLookup[exp.Right] = tvhdl;
			}
			else if (element.CecilType.IsArrayType() && element.DefaultValue == null)
			{
				exp.Right = new EmptyArrayCreateExpression()
				{
					Parent = exp,
					SourceExpression = null,
					SourceResultType = element.CecilType,
					SizeExpression = new MemberReferenceExpression()
					{
						Name = element.Name,
						SourceExpression = null,
						SourceResultType = element.CecilType,
						Target = element
					}
				};

				if (element.Source is IMemberDefinition)
					Parent.TypeLookup[exp.Right] = Parent.TypeScope.GetVHDLType((IMemberDefinition)element.Source, element.CecilType);
				else if (element.Source is System.Reflection.PropertyInfo)
					Parent.TypeLookup[exp.Right] = Parent.TypeScope.GetVHDLType((System.Reflection.PropertyInfo)element.Source);
			}
			else
			{
				exp.Right = new AST.PrimitiveExpression()
				{
					Value = element.DefaultValue,
					Parent = exp,
					SourceResultType = element.CecilType
				};

				var n = new[] { typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong) };

				if (n.Any(x => exp.Right.SourceResultType.IsSameTypeReference(x)))
					Parent.TypeLookup[exp.Right] = VHDLTypes.INTEGER;
				else if (element.DefaultValue != null && !element.DefaultValue.GetType().IsEnum && element.DefaultValue.GetType() != typeof(bool))
					Parent.TypeLookup[exp.Right] = VHDLTypes.INTEGER;
			}

			res.UpdateParents();
			if (tvhdl.IsArray && !tvhdl.IsNumeric && !tvhdl.IsStdLogicVector && !tvhdl.IsSystemType && (exp.Right is PrimitiveExpression || exp.Right is EmptyArrayCreateExpression))
			{
				
			}
			else
			{
				VHDLTypeConversion.ConvertExpression(Parent, null, exp.Right, tvhdl, element.CecilType, false);
			}
				
			return res;
		}

		/// <summary>
		/// Returns a sequence of all the reset statements emitted when the RST signal goes high
		/// </summary>
		public IEnumerable<string> ProcessResetStaments
		{
			get
			{
				foreach (var bus in Process.OutputBusses.Union(Process.InternalBusses))
					foreach (var signal in WrittenSignals(bus))
						foreach (var s in RenderStatement(null, GetResetStatement(signal), 0))
							yield return s;

				foreach (var signal in Process.SharedSignals)
					if (!(signal.Source is Mono.Cecil.IMemberDefinition) || ((Mono.Cecil.IMemberDefinition)signal.Source).GetAttribute<IgnoreAttribute>() == null)
						foreach (var s in RenderStatement(null, GetResetStatement(signal), 0))
							yield return s;

				foreach (var v in Process.SharedVariables)
					if (!(v.Source is Mono.Cecil.IMemberDefinition) || ((Mono.Cecil.IMemberDefinition)v.Source).GetAttribute<IgnoreAttribute>() == null)
						foreach (var s in RenderStatement(null, GetResetStatement(v), 0))
							yield return s;

				if (Process.MainMethod != null)
				{
					foreach(var variable in Process.MainMethod.Variables)
						foreach (var s in RenderStatement(null, GetResetStatement(variable), 0))
							yield return s;
						
					if (Parent.TemporaryVariables.ContainsKey(Process.MainMethod))
						foreach (var variable in Parent.TemporaryVariables[Process.MainMethod].Values)
							foreach (var s in RenderStatement(null, GetResetStatement(variable), 0))
								yield return s;
				}
			}
		}

		/// <summary>
		/// Returns a sequence of statements to perform when the clock signal rises
		/// </summary>
		public IEnumerable<string> ClockResetStaments
		{
			get
			{
				foreach (var bus in Process.InternalBusses)
					foreach (var signal in bus.Signals)
						foreach (var s in RenderStatement(null, GetResetStatement(signal), 0))
							yield return s;
			}
		}

		/// <summary>
		/// Gets a sequence of the variables found in the process
		/// </summary>
		/// <value>The variables.</value>
		public IEnumerable<Variable> Variables
		{
			get
			{
				foreach (var v in Process.SharedVariables)
					if (!(v.Source is Mono.Cecil.IMemberDefinition) || ((Mono.Cecil.IMemberDefinition)v.Source).GetAttribute<IgnoreAttribute>() == null)
						yield return v;

				if (Process.MainMethod != null)
					foreach (var v in Process.MainMethod.Variables)
						yield return v;

				foreach (var m in Parent.TemporaryVariables)
					if (m.Key == Process.MainMethod)
						foreach (var v in m.Value.Values)
							yield return v;
			}
		}
	}
}
