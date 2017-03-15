using System;
using SME.AST;

namespace SME.VHDL
{
	public static class VHDLTypeConversion
	{
		public static Expression ConvertExpression(RenderState render, Method method, Expression s, VHDLType target, bool fromCast)
		{
			var svhdl = render.VHDLType(s);

			// Already the real target type, just return it
			if (svhdl == target)
				return s;

			// Stuff we do not care about
			if (!svhdl.IsStdLogicVector && !svhdl.IsUnsigned && !svhdl.IsSigned && svhdl.IsArray && target.IsArray && render.TypeScope.GetByName(svhdl.ElementName) == render.TypeScope.GetByName(target.ElementName))
				return s;

			// Array lengths
			var targetlengthstr = string.IsNullOrWhiteSpace(target.Alias) ? target.Length.ToString() : target.Alias + "'length";

			if (target == VHDLTypes.SYSTEM_BOOL)
			{
				// Boolean to std_logic is fine
				if (string.Equals("STD_LOGIC", svhdl.Name, StringComparison.OrdinalIgnoreCase))
					return s;

				// Source is numeric, and output is bool
				if (svhdl.IsNumeric || svhdl.IsStdLogicVector)
				{
					var zero = new PrimitiveExpression()
					{
						SourceExpression = s.SourceExpression,
						SourceResultType = s.SourceResultType,
						Value = 0
					};

					var eval = new BinaryOperatorExpression()
					{
						Parent = s.Parent,
						Name = s.Name,
						SourceExpression = s.SourceExpression,
						SourceResultType = s.SourceResultType.LoadType(typeof(bool)),
						Left = s,
						Operator = ICSharpCode.NRefactory.CSharp.BinaryOperatorType.InEquality,
						Right = zero
					};

					zero.Parent = eval;

					s.ReplaceWith(eval);
					s.Parent = eval;

					return eval;
				}
				else if (svhdl == VHDLTypes.BOOL)
				{
					var truexp = new PrimitiveExpression()
					{
						Value = true,
						SourceExpression = s.SourceExpression,
						SourceResultType = s.SourceResultType.LoadType(typeof(bool)),
					};

					var falseexp = new PrimitiveExpression()
					{
						Value = false,
						SourceExpression = s.SourceExpression,
						SourceResultType = s.SourceResultType.LoadType(typeof(bool)),
					};

					var eval = new ConditionalExpression()
					{
						ConditionExpression = s,
						TrueExpression = truexp,
						FalseExpression = falseexp,
						SourceExpression = s.SourceExpression,
						SourceResultType = truexp.SourceResultType
					};

					truexp.Parent = eval;
					falseexp.Parent = eval;

					s.ReplaceWith(eval);
					s.Parent = eval;

					return eval;
				}
				else
					throw new Exception(string.Format("Unexpected conversion from {0} to {1}", svhdl, target));
			}
			else if (svhdl == VHDLTypes.INTEGER && (target.IsStdLogicVector || target.IsNumeric))
			{
				if (target.IsSigned && target.IsNumeric)
					return WrapExpression(render, s, string.Format("TO_SIGNED({0}, {1})", "{0}", targetlengthstr), target);
				else if (target.IsUnsigned && target.IsNumeric)
					return WrapExpression(render, s, string.Format("TO_UNSIGNED({0}, {1})", "{0}", targetlengthstr), target);
				else if (target.IsStdLogicVector)
					return WrapExpression(render, s, string.Format("STD_LOGIC_VECTOR(TO_UNSIGNED({0}, {1}))", "{0}", targetlengthstr), target);
				else
					throw new Exception(string.Format("Unexpected conversion from {0} to {1}", svhdl, target));
			}
			else if (target.IsNumeric)
			{				
				if (svhdl.IsStdLogicVector || svhdl.IsSigned || svhdl.IsUnsigned)
				{
					var str = "{0}";
					var resized = false;
					Variable tmpvar = null;
					if (target.Length != svhdl.Length)
					{
						if (svhdl.IsSystemSigned)
						{
							str = string.Format("resize(SIGNED({0}), {1})", str, targetlengthstr);
							svhdl = render.TypeScope.NumericEquivalent(svhdl);
							resized = true;
						}
						else if (svhdl.IsSystemUnsigned)
						{
							str = string.Format("resize(UNSIGNED({0}), {1})", str, targetlengthstr);
							svhdl = render.TypeScope.NumericEquivalent(svhdl);
							resized = true;
						}
						else if (svhdl.IsVHDLSigned || svhdl.IsVHDLUnsigned)
						{
							resized = true;
							str = string.Format("resize({0}, {1})", str, targetlengthstr);
						}
						else if (target.Length > svhdl.Length)
						{
							// This must be a variable as bit concatenation is only allowed in assignment statements:
							// http://stackoverflow.com/questions/209458/concatenating-bits-in-vhdl

							tmpvar = render.RegisterTemporaryVariable(method, s.SourceResultType);
							render.TypeLookup[tmpvar] = target;

							var iexp = new IdentifierExpression()
							{
								Name = tmpvar.Name,
								Target = tmpvar,
								SourceExpression = s.SourceExpression,
								SourceResultType = s.SourceResultType
							};

							if (render.USE_EXPLICIT_CONCATENATION_OPERATOR)
								str = string.Format("IEEE.STD_LOGIC_1164.\"&\"(\"{0}\", {1})", new string('0', target.Length - svhdl.Length), "{0}");
							else
								str = string.Format("\"{0}\" & {1}", new string('0', target.Length - svhdl.Length), "{0}");

							s.ReplaceWith(iexp);

							var asstm = new ExpressionStatement()
							{
								Expression = new AssignmentExpression()
								{
									Left = iexp.Clone(),
									Right = s,
									Operator = ICSharpCode.NRefactory.CSharp.AssignmentOperatorType.Assign,
									SourceExpression = s.SourceExpression,
									SourceResultType = s.SourceResultType
								},
								SourceStatement = s.SourceExpression.Clone()
							};

							s.PrependStatement(asstm);
							asstm.UpdateParents();

							resized = true;
						}
					}

					if (svhdl.IsVHDLSigned != target.IsSigned || svhdl.IsVHDLUnsigned != target.IsUnsigned)
						str = string.Format("{1}({0})", str, target.IsSigned ? "SIGNED" : "UNSIGNED");

					if (target.Length != svhdl.Length && !resized)
						str = string.Format("resize({0}, {1})", str, targetlengthstr);

					return WrapExpression(render, s, str, target);
				}


				/*if (svhdl.IsStdLogicVector && target.IsSigned)
					return new VHDLConvertedExpression(s, target, "SIGNED({0})");
				else if (svhdl.IsStdLogicVector && target.IsUnsigned)
					return new VHDLConvertedExpression(s, target, "UNSIGNED({0})");
				else*/
				throw new Exception(string.Format("Unexpected conversion from {0} to {1}", svhdl, target));
			}
			else if (target.IsStdLogicVector)
			{
				if (svhdl.IsNumeric)
				{
					if (svhdl.Length == target.Length)
					{
						return WrapExpression(render, s, "STD_LOGIC_VECTOR({0})", target);
					}
					else
					{
						if (!fromCast)
							Console.WriteLine("WARN: Incompatible array lengths, from {0} to {1}", svhdl, target);
						//throw new Exception(string.Format("Incompatible array lengths, from {0} to {1}", svhdl, target));

						return WrapExpression(render, s, string.Format("STD_LOGIC_VECTOR(resize({0}, {1}))", "{0}", targetlengthstr), target);
					}
				}
				else if (svhdl.IsStdLogicVector)
				{
					if (target.Length == svhdl.Length)
					{
						render.TypeLookup[s] = target;
						return s;
					}

					if (!fromCast)
						Console.WriteLine("WARN: Incompatible array lengths, from {0} to {1}", svhdl, target);
					//throw new Exception(string.Format("Incompatible array lengths, from {0} to {1}", svhdl, target));

					if (target.Length < svhdl.Length)
					{
						// We cannot select bits from a typecast
						// TODO: Dirty to rely on the string, there are likely other cases that need the same wrapping
						if (svhdl.IsStdLogicVector) //.ToString().StartsWith("STD_LOGIC_VECTOR(", StringComparison.OrdinalIgnoreCase))
						{
							var tmp = render.RegisterTemporaryVariable(method, s.SourceResultType);
							render.TypeLookup[tmp] = target;

							var aleft = new IdentifierExpression()
							{
								Name = tmp.Name,
								SourceExpression = s.SourceExpression,
								SourceResultType = s.SourceResultType
							};

							var aexp = new AssignmentExpression()
							{
								Left = aleft,
								Right = s,
								SourceExpression = s.SourceExpression,
								SourceResultType = s.SourceResultType
							};

							var astm = new ExpressionStatement()
							{
								Expression = aexp,
								Parent = method,
								SourceStatement = s.SourceExpression
							};

							aexp.Parent = astm;
							aleft.Parent = s.Parent = aexp;

							s.PrependStatement(astm);

							var iexp = new IdentifierExpression()
							{
								SourceExpression = s.SourceExpression,
								SourceResultType = s.SourceResultType,
								Target = tmp
							};

							s.ReplaceWith(iexp);

							return WrapExpression(render, s, string.Format("{0}({1} downto 0)", "{0}", target.Length - 1), target);
						}

						return WrapExpression(render, s, string.Format("{0}({1} downto 0)", "{0}", target.Length - 1), target);
					}
					else if (svhdl.IsSigned)
					{
						return WrapExpression(render, s, string.Format("STD_LOGIC_VECTOR(resize(SIGNED({0}), {1}))", "{0}", targetlengthstr), target);
					}
					else if (svhdl.IsUnsigned)
					{
						return WrapExpression(render, s, string.Format("STD_LOGIC_VECTOR(resize(UNSIGNED({0}), {1}))", "{0}", targetlengthstr), target);
					}
					else
					{
						// TODO: Not correct ResolvedSourceType, should be target
						var tmp = render.RegisterTemporaryVariable(method, s.SourceResultType);
						render.TypeLookup[tmp] = target;

						var iexp = new IdentifierExpression()
						{
							Name = tmp.Name,
							Target = tmp,
							SourceExpression = s.SourceExpression,
							SourceResultType = s.SourceResultType
						};

						render.TypeLookup[iexp] = target;

						var wrapped = WrapExpression(render, s, string.Format("{0} := \"{1}\" & {2};", tmp, new string('0', target.Length - svhdl.Length), "{0}"), target);

						s.ReplaceWith(iexp);

						var asstm = new ExpressionStatement()
						{
							Expression = wrapped,
							SourceStatement = s.SourceExpression
						};

						s.PrependStatement(asstm);
						s.Parent = wrapped;
						wrapped.Parent = asstm;

						return iexp;
					}
				}
				else if (svhdl.IsSigned || svhdl.IsUnsigned)
				{
					if (target.Length == svhdl.Length)
						return WrapExpression(render, s, string.Format("STD_LOGIC_VECTOR({0})", "{0}"), target);
					else
						return WrapExpression(render, s, string.Format("STD_LOGIC_VECTOR(resize({0}, {1}))", "{0}", targetlengthstr), target);
				}
				else
					throw new Exception(string.Format("Unexpected conversion from {0} to {1}", svhdl.Name, target.Name));
			}
			else if (target == VHDLTypes.INTEGER && (svhdl.IsStdLogicVector || svhdl.IsNumeric))
			{
				if (svhdl.IsNumeric)
					return WrapExpression(render, s, "TO_INTEGER({0})", target);

				if (svhdl.IsSigned)
					return WrapExpression(render, s, "TO_INTEGER(SIGNED({0}))", target);
				else
					return WrapExpression(render, s, "TO_INTEGER(UNSIGNED({0}))", target);
			}
			else if (target == VHDLTypes.BOOL && svhdl == VHDLTypes.SYSTEM_BOOL)
			{
				return WrapInParenthesis(render, WrapExpression(render, s, "{0} = '1'", target));
			}
			else if ((target.IsSigned || target.IsUnsigned) && svhdl.IsStdLogicVector)
			{
				if (target.Length == svhdl.Length)
					return WrapExpression(render, s, string.Format("{1}({0})", "{0}", target.IsSigned ? "SIGNED" : "UNSIGNED"), target);
				else
					return WrapExpression(render, s, string.Format("resize({1}({0}), {2})", "{0}", target.IsSigned ? "SIGNED" : "UNSIGNED", targetlengthstr), target);
			}
			else if ((target.IsSigned || target.IsUnsigned) && svhdl == VHDLTypes.INTEGER)
			{
				if (target.IsSigned)
					return WrapExpression(render, s, string.Format("TO_SIGNED({0}, {1})", "{0}", target.Length), target);
				else if (target.IsUnsigned)
					return WrapExpression(render, s, string.Format("TO_UNSIGNED({0}, {1})", "{0}", target.Length), target);
				else
					throw new Exception("Unexpected case");
			}
			else
				throw new Exception(string.Format("Unexpected target type: {0} for source: {1}", target, svhdl));
		}

		public static AST.ParenthesizedExpression WrapInParenthesis(RenderState render, Expression expression)
		{
			var self = new AST.ParenthesizedExpression()
			{
				Expression = expression,
				Parent = expression.Parent,
				SourceExpression = expression.SourceExpression,
				SourceResultType = expression.SourceResultType
			};

			expression.ReplaceWith(self);
			expression.Parent = self;

			render.TypeLookup[self] = render.VHDLType(expression);

			return self;
		}

		public static CustomNodes.ConversionExpression WrapExpression(RenderState render, Expression expression, string template, VHDLType vhdltarget)
		{
			if (expression is CastExpression)
				expression = ((CastExpression)expression).Expression;

			var self = new CustomNodes.ConversionExpression() {
				Expression = expression,
				Parent = expression.Parent,
				WrappingTemplate = template,
				SourceExpression = expression.SourceExpression,
				SourceResultType = expression.SourceResultType
			};

			expression.ReplaceWith(self);
			expression.Parent = self;

			render.TypeLookup[self] = vhdltarget;

			return self;
		}

		public static void PrependStatement(this Expression source, Statement target)
		{
			var p = source.Parent;
			while (p != null && !(p is Statement))
				p = p.Parent;

			var stm = p as Statement;
			if (stm == null)
				throw new Exception("Unable to find a parent statement");

			stm.PrependStatement(target);
		}

		public static void PrependStatement(this Statement source, Statement target)
		{
			if (source is BlockStatement)
			{
				var bst = source as BlockStatement;
				var n = new Statement[bst.Statements.Length + 1];
				Array.Copy(bst.Statements, 0, n, 1, bst.Statements.Length);
				n[0] = target;
				target.Parent = bst;
				bst.Statements = n;
				return;
			}
			else
			{
				var blst = new BlockStatement()
				{
					Parent = source.Parent,
					SourceStatement = source.SourceStatement,
					Statements = new Statement[] { target, source }
				};

				source.ReplaceWith(blst);
				target.Parent = source.Parent = blst;
			}
		}

	}
}
