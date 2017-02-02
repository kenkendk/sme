using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.Decompiler;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;
using SME.Render.Transpiler;
using SME.Render.Transpiler.ILConvert;
using SME.Render.VHDL.ILConvert.AugmentedExpression;

namespace SME.Render.VHDL.ILConvert
{
	public class VHDLConverter : SME.Render.Transpiler.ILConvert.Converter<IVHDLExpression, VHDLTypeDescriptor>
	{
		public VHDLConverter(IProcess process, GlobalInformation<VHDLTypeDescriptor> globalInformation, int indentation = 0)
			: this(process.GetType(), globalInformation, indentation)
		{

		}

		public VHDLConverter(Type process, GlobalInformation<VHDLTypeDescriptor> globalInformation, int indentation = 0)
			: this(LoadType(process), globalInformation, indentation)
		{
		}

		public VHDLConverter(TypeDefinition process, GlobalInformation<VHDLTypeDescriptor> globalInformation, int indentation = 0)
			: base(process, globalInformation, indentation)
		{
		}

		public static bool SUPPORTS_VHDL_2008 = false;

		public VHDLGlobalInformation Information { get { return (VHDLGlobalInformation)m_globalInformation; } }



		public override IEnumerable<string> RenderMethod(int indentation)
		{			
			var method = Information.ToValidName(m_methoddef.Name);

			if (!m_methoddef.IsStatic)
				throw new Exception("Non-static member functions are not yet supported");

			var returntype = m_methoddef.ReturnType.FullName == "System.Void" ? null : Information.ToValidName(Information.VHDLType(m_methoddef.ReturnType));

			var margs = string.Join("; ",
				from n in m_methoddef.Parameters
				let inoutargstr = n.GetVHDLInOut()

				select string.Format(
					"{0}{1}: {2} {3}",
					string.Equals(inoutargstr, "in", StringComparison.OrdinalIgnoreCase) ? "constant " : "",
					Information.ToValidName(n.Name),
					inoutargstr,
					n.GetAttribute<RangeAttribute>() != null
                    	? Information.ToValidName(m_methoddef.Name + "_" + n.Name + "_type")
						: Information.ToValidName(Information.VHDLTypes.GetVHDLType(n).ToString())
				));

			var indent = new string(' ', indentation);

			if (string.IsNullOrWhiteSpace(returntype))
				yield return string.Format("{0}procedure {1}({2}) {3} is", indent, method, margs, returntype);
			else
				yield return string.Format("{0}pure function {1}({2}) return {3} is", indent, method, margs, returntype);

			foreach (var n in m_localVariables)
			{
				var decl = string.Format("{0}    variable {1}: {2}", indent, Information.ToValidName(n.Key), n.Value.Item2.ToSafeVHDLName());
				var assign = "";

				yield return decl + assign + ";";
			}

			yield return indent + "begin";

			foreach (var line in this.Body)
				yield return line;

			yield return string.Format("{0}end {1};", indent, method);
		}


		public override IVHDLExpression WrapConverted(IVHDLExpression s, VHDLTypeDescriptor target, bool fromCast = false)
		{
			if (s.VHDLType == target)
				return s;

			if (!s.VHDLType.IsStdLogicVector && !s.VHDLType.IsUnsigned && !s.VHDLType.IsSigned && s.VHDLType.IsArray && target.IsArray && Information.VHDLTypes.GetByName(s.VHDLType.ElementName) == Information.VHDLTypes.GetByName(target.ElementName))
				return s;

			var targetlengthstr = string.IsNullOrWhiteSpace(target.Alias) ? target.Length.ToString() : target.Alias + "'length";

			if (target == VHDLTypes.SYSTEM_BOOL)
			{
				if (string.Equals("STD_LOGIC", s.VHDLType.Name, StringComparison.OrdinalIgnoreCase))
					return s;

				if (s.VHDLType.IsNumeric || s.VHDLType.IsStdLogicVector)
				{
					return ResolveExpression(
						new BinaryOperatorExpression(
							s.Expression.Clone(),
							BinaryOperatorType.InEquality,
							new PrimitiveExpression(0)
						)
					);
				}
				else if (s.VHDLType == VHDLTypes.BOOL)
				{
					return ResolveExpression(
						new ConditionalExpression(
							s.Expression.Clone(),
							new PrimitiveExpression(true),
							new PrimitiveExpression(false)
						)
					);

				}
				else
					throw new Exception(string.Format("Unexpected conversion from {0} to {1}", s.VHDLType, target));
			}
			else if (s.VHDLType == VHDLTypes.INTEGER && (target.IsStdLogicVector || target.IsNumeric))
			{
				if (target.IsSigned && target.IsNumeric)
					return new VHDLConvertedExpression(s, target, string.Format("TO_SIGNED({0}, {1})", "{0}", targetlengthstr));
				else if (target.IsUnsigned && target.IsNumeric)
					return new VHDLConvertedExpression(s, target, string.Format("TO_UNSIGNED({0}, {1})", "{0}", targetlengthstr));
				else if (target.IsStdLogicVector)
					return new VHDLConvertedExpression(s, target, string.Format("STD_LOGIC_VECTOR(TO_UNSIGNED({0}, {1}))", "{0}", targetlengthstr));
				else
					throw new Exception(string.Format("Unexpected conversion from {0} to {1}", s.VHDLType, target));
			}
			else if (target.IsNumeric)
			{
				if (s.VHDLType.IsStdLogicVector || s.VHDLType.IsSigned || s.VHDLType.IsUnsigned)
				{
					var str = "{0}";
					var resized = false;
					string tmpvar = null;
					if (target.Length != s.VHDLType.Length)
					{
						if (s.VHDLType.IsVHDLSigned || s.VHDLType.IsVHDLUnsigned)
						{
							resized = true;
							str = string.Format("resize({0}, {1})", str, targetlengthstr);
						}
						else if (target.Length > s.VHDLType.Length)
						{
							// This must be a variable as bit concatenation is only allowed in assignment statements:
							// http://stackoverflow.com/questions/209458/concatenating-bits-in-vhdl

							// TODO: Not correct ResolvedSourceType, should be target
							tmpvar = RegisterTemporaryVariable(s.ResolvedSourceType, Information.VHDLTypes.GetStdLogicVector(target.Length));
							m_sb.PrependLine(string.Format("{0} := \"{1}\" & {2};", tmpvar, new string('0', target.Length - s.VHDLType.Length), s.ResolvedString));

							resized = true;
						}
					}

					if (s.VHDLType.IsVHDLSigned != target.IsSigned || s.VHDLType.IsVHDLUnsigned != target.IsUnsigned)
						str = string.Format("{1}({0})", str, target.IsSigned ? "SIGNED" : "UNSIGNED");

					if (target.Length != s.VHDLType.Length && !resized)
						str = string.Format("resize({0}, {1})", str, targetlengthstr);

					if (tmpvar != null)
						s = ResolveExpression(new IdentifierExpression(tmpvar));
					return new VHDLConvertedExpression(WrapIfComposite(s), target, str);
				}


				/*if (s.VHDLType.IsStdLogicVector && target.IsSigned)
					return new VHDLConvertedExpression(s, target, "SIGNED({0})");
				else if (s.VHDLType.IsStdLogicVector && target.IsUnsigned)
					return new VHDLConvertedExpression(s, target, "UNSIGNED({0})");
				else*/
				throw new Exception(string.Format("Unexpected conversion from {0} to {1}", s.VHDLType, target));
			}
			else if (target.IsStdLogicVector)
			{
				if (s.VHDLType.IsNumeric)
				{
					if (s.VHDLType.Length == target.Length)
						return new VHDLConvertedExpression(s, target, "STD_LOGIC_VECTOR({0})");
					else
					{
						if (!fromCast)
							Console.WriteLine("WARN: Incompatible array lengths, from {0} to {1}", s.VHDLType, target);
						//throw new Exception(string.Format("Incompatible array lengths, from {0} to {1}", s.VHDLType, target));

						return new VHDLConvertedExpression(s, target, string.Format("STD_LOGIC_VECTOR(resize({0}, {1}))", "{0}", targetlengthstr));
					}

				}
				else if (s.VHDLType.IsStdLogicVector)
				{
					if (target.Length == s.VHDLType.Length)
						return new VHDLConvertedExpression(s, target, "{0}");

					if (!fromCast)
						Console.WriteLine("WARN: Incompatible array lengths, from {0} to {1}", s.VHDLType, target);
					//throw new Exception(string.Format("Incompatible array lengths, from {0} to {1}", s.VHDLType, target));

					if (target.Length < s.VHDLType.Length)
					{
						// We cannot select bits from a typecast
						// TODO: Dirty to rely on the string, there are likely other cases that need the same wrapping
						if (s.ResolvedString.StartsWith("STD_LOGIC_VECTOR(", StringComparison.OrdinalIgnoreCase))
						{
							var tmp = RegisterTemporaryVariable(s.ResolvedSourceType, s.VHDLType);
							m_sb.PrependLine(string.Format("{0} := {1};", tmp, s.ResolvedString));

							return new VHDLConvertedExpression(ResolveExpression(new IdentifierExpression(tmp)), target, string.Format("{0}({1} downto 0)", "{0}", target.Length - 1));
						}

						return new VHDLConvertedExpression(s, target, string.Format("{0}({1} downto 0)", "{0}", target.Length - 1));

					}
					else if (s.VHDLType.IsSigned)
						return new VHDLConvertedExpression(s, target, string.Format("STD_LOGIC_VECTOR(resize(SIGNED({0}), {1}))", "{0}", targetlengthstr));
					else if (s.VHDLType.IsUnsigned)
						return new VHDLConvertedExpression(s, target, string.Format("STD_LOGIC_VECTOR(resize(UNSIGNED({0}), {1}))", "{0}", targetlengthstr));
					else
					{
						// TODO: Not correct ResolvedSourceType, should be target
						var tmp = RegisterTemporaryVariable(s.ResolvedSourceType, target);
						m_sb.PrependLine(string.Format("{0} := \"{1}\" & {2};", tmp, new string('0', target.Length - s.VHDLType.Length), s.ResolvedString));

						return ResolveExpression(new IdentifierExpression(tmp));

						// This must be a variable as bit concatenation is only allowed in assignment statements:
						// http://stackoverflow.com/questions/209458/concatenating-bits-in-vhdl

						//return new VHDLConvertedExpression(s, target, string.Format("\"{1}\" & {0}", "{0}", new string('0', target.Length - s.VHDLType.Length)), true);

					}

				}
				else if (s.VHDLType.IsSigned || s.VHDLType.IsUnsigned)
				{
					if (target.Length == s.VHDLType.Length)
						return new VHDLConvertedExpression(s, target, string.Format("STD_LOGIC_VECTOR({0})", "{0}"));
					else
						return new VHDLConvertedExpression(s, target, string.Format("STD_LOGIC_VECTOR(resize({0}, {1}))", "{0}", targetlengthstr));
				}
				else
					throw new Exception(string.Format("Unexpected conversion from {0} to {1}", s.VHDLType.Name, target.Name));
			}
			else if (target == VHDLTypes.INTEGER && (s.VHDLType.IsStdLogicVector || s.VHDLType.IsNumeric))
			{
				if (s.VHDLType.IsNumeric)
					return new VHDLConvertedExpression(s, target, "TO_INTEGER({0})");

				if (s.VHDLType.IsSigned)
					return new VHDLConvertedExpression(s, target, "TO_INTEGER(SIGNED({0}))");
				else
					return new VHDLConvertedExpression(s, target, "TO_INTEGER(UNSIGNED({0}))");
			}
			else if (target == VHDLTypes.BOOL && s.VHDLType == VHDLTypes.SYSTEM_BOOL)
			{
				return new VHDLConvertedExpression(s, target, "{0} = '1'", true);
			}
			else if ((target.IsSigned || target.IsUnsigned) && s.VHDLType.IsStdLogicVector)
			{
				if (target.Length == s.VHDLType.Length)
					return new VHDLConvertedExpression(s, target, string.Format("{1}({0})", "{0}", target.IsSigned ? "SIGNED" : "UNSIGNED"));
				else
					return new VHDLConvertedExpression(s, target, string.Format("resize({1}({0}), {2})", "{0}", target.IsSigned ? "SIGNED" : "UNSIGNED", targetlengthstr));
			}
			else if ((target.IsSigned || target.IsUnsigned) && s.VHDLType == VHDLTypes.INTEGER)
			{
				if (target.IsSigned)
					return new VHDLConvertedExpression(s, target, string.Format("TO_SIGNED({0}, {1})", "{0}", target.Length));
				else if (target.IsUnsigned)
					return new VHDLConvertedExpression(s, target, string.Format("TO_UNSIGNED({0}, {1})", "{0}", target.Length));
				else
					throw new Exception("Unexpected case");
			}
			else
				throw new Exception(string.Format("Unexpected target type: {0} for source: {1}", target, s.VHDLType));
		}

		public override IVHDLExpression WrapIfComposite(IVHDLExpression s)
		{
			if (s is VHDLIndexerExpression || s is VHDLMemberReferenceExpression || s is VHDLPrimitiveExpression || s is VHDLIdentifierExpression || s is VHDLIndexerExpression || s is VHDLInvocationExpression || s is VHDLParenthesizedExpression || s is VHDLCastExpression)
				return s;
			else
			{
				if (s is VHDLConvertedExpression && !(s as VHDLConvertedExpression).NeedsWrapping)
					return s;

				return new VHDLConvertedExpression(s, s.VHDLType, "({0})");
			}
		}

		public override IVHDLExpression ResolveExpression(Expression s)
		{
			if (s is AssignmentExpression)
				return new VHDLAssignmentExpression(this, s as AssignmentExpression);
			else if (s is IdentifierExpression)
				return new VHDLIdentifierExpression(this, s as IdentifierExpression);
			else if (s is MemberReferenceExpression)
				return new VHDLMemberReferenceExpression(this, s as MemberReferenceExpression);
			else if (s is PrimitiveExpression)
				return new VHDLPrimitiveExpression(this, s as PrimitiveExpression);
			else if (s is BinaryOperatorExpression)
				return new VHDLBinaryOperatorExpression(this, s as BinaryOperatorExpression);
			else if (s is UnaryOperatorExpression)
				return new VHDLUnaryOperatorExpression(this, s as UnaryOperatorExpression);
			else if (s is IndexerExpression)
				return new VHDLIndexerExpression(this, s as IndexerExpression);
			else if (s is CastExpression)
				return new VHDLCastExpression(this, s as CastExpression);
			else if (s is ConditionalExpression)
				return new VHDLConditionalExpression(this, s as ConditionalExpression);
			else if (s is InvocationExpression)
			{
				var si = s as InvocationExpression;
				var mt = si.Target as MemberReferenceExpression;

				// Catch common translations
				if (mt != null && (s as InvocationExpression).Arguments.Count == 1)
				{
					var mtm = new VHDLMemberReferenceExpression(this, mt);
					if (mt.MemberName == "op_Implicit" || mt.MemberName == "op_Explicit")
						return ResolveExpression(new CastExpression(AstType.Create(mtm.ResolvedSourceType.FullName), si.Arguments.First().Clone()));
					else if (mt.MemberName == "op_Increment")
						return ResolveExpression(new UnaryOperatorExpression(UnaryOperatorType.Increment, si.Arguments.First().Clone()));
					else if (mt.MemberName == "op_Decrement")
						return ResolveExpression(new UnaryOperatorExpression(UnaryOperatorType.Decrement, si.Arguments.First().Clone()));
				}

				return new VHDLInvocationExpression(this, s as InvocationExpression);
			}
			else if (s is ParenthesizedExpression)
				return new VHDLParenthesizedExpression(this, s as ParenthesizedExpression);
			else if (s is NullReferenceExpression)
				return new VHDLEmptyExpression(this, s as NullReferenceExpression);
			else if (s is ArrayCreateExpression)
				return new VHDLArrayCreateExpression(this, s as ArrayCreateExpression);
			else if (s is CheckedExpression)
				return new VHDLCheckedExpression(this, s as CheckedExpression);
			else if (s is UncheckedExpression)
				return new VHDLUncheckedExpression(this, s as UncheckedExpression);
			else if (s == Expression.Null)
				return new VHDLEmptyExpression(this, null);
			else
				throw new Exception(string.Format("Unsupported expression: {0} ({1})", s, s.GetType().FullName));
		}

		public override MemberItem ResolveIdentifierItem(string name)
		{
			return new VHDLIdentifierExpression(this, new IdentifierExpression(name)).ResolvedItem;
		}

		public override string ToSafeName(VHDLTypeDescriptor type)
		{
			return type.ToSafeVHDLName();
		}

		public static string GetDefaultInitializer(VHDLGlobalInformation info, TypeReference vartype, IMemberDefinition member)
		{
			VHDLTypeDescriptor vhdltype;
			TypeReference tr = vartype;

			if (member != null)
			{
				vhdltype = info.VHDLTypes.GetVHDLType(member, tr);
			}
			else
			{
				vhdltype = info.VHDLTypes.GetVHDLType(tr);
			}

			var b = "{0}";
			while (vhdltype.IsArray)
			{
				b = string.Format("(others => {0})", b);
				vhdltype = info.VHDLTypes.GetByName(vhdltype.ElementName);
			}

			if (vhdltype.IsStdLogic)
				return string.Format(b, "'0'");
			else
				return null;
			// TODO: GetDefaultValue(vhdltype);
		}

		public override string GetDefaultInitializer(TypeReference vartype, IMemberDefinition member)
		{
			return GetDefaultInitializer(Information, vartype, member);

		}

		public override string ResolveResetStatement(Expression target, object initialvalue = null)
		{
			var tg = ResolveExpression(target);
			var itemtype = tg.ResolvedSourceType;
			initialvalue = initialvalue ?? GetDefaultValue(itemtype);

			if (initialvalue == null)
			{
				var assignoperator = IsVariableExpression(tg) ? ":=" : "<=";

				string definit = null;
				var tgtstr = tg.ResolvedString;
				if (tg is VHDLMemberReferenceExpression)
				{
					var mr = (tg as VHDLMemberReferenceExpression).Member;
					if (mr.GetAttribute<IgnoreAttribute>() != null)
						return "-- " + target.ToString();


					if (mr.DeclaringType.IsBusType() && !this.IsClockedProcess)
					{
						if (mr.GetAttribute<InternalBusAttribute>() != null)
							tgtstr = "next_" + tg.ResolvedString;
						else // if ((s.Left as MemberReferenceExpression).Target is IdentifierExpression)
						{
							mr = ResolveMemberReference((target as MemberReferenceExpression).Target);
							if (mr.GetAttribute<InternalBusAttribute>() != null)
								tgtstr = "next_" + tg.ResolvedString;
						}
					}

					definit = GetDefaultInitializer(Information, itemtype, mr.Item);
				}

				if (string.IsNullOrWhiteSpace(definit))
					definit = GetDefaultInitializer(Information, itemtype, null);

				if (definit == null)
					return string.Format("-- {0} {1} ???", tgtstr, assignoperator);
				else
					return string.Format("{0} {1} {2}", tgtstr, assignoperator, definit);
			}
			else
			{
				return ResolveExpression(
					new AssignmentExpression(
						target,
						initialvalue is Expression ? (initialvalue as Expression).Clone() : new PrimitiveExpression(initialvalue)
					)
				).ResolvedString;
			}
		}

		public override bool IsVariableExpression(IVHDLExpression s)
		{
			var name = s is VHDLIdentifierExpression ? (s as VHDLIdentifierExpression).Identifier : null;
			if (name != null && m_localRenames.ContainsKey(name))
				name = m_localRenames[name];

			if ((s is VHDLMemberReferenceExpression) && (s as VHDLMemberReferenceExpression).Member.IsVariable)
				return true;
			else if (s is VHDLIdentifierExpression && (m_localVariables.ContainsKey(name) || m_classVariables.ContainsKey(name)))
				return true;
			else if (s is VHDLIndexerExpression)
				return IsVariableExpression((s as VHDLIndexerExpression).Target);
			else
				return false;
		}

		public override bool IsConstantReference(IVHDLExpression m)
		{
			if (!(m is VHDLMemberReferenceExpression))
				throw new Exception("Unexpected call to IsConstantReference");

			var s = m as VHDLMemberReferenceExpression;

			if (s.Member.Item is FieldDefinition)
			{
				var ft = s.Member.Item as FieldDefinition;
				if (m_globalInformation.Constants.ContainsKey(ft))
					return true;

				if (ft.IsLiteral || (ft.IsStatic && ft.IsInitOnly))
					return true;
			}

			return false;
		}

		public override object GetInitialValueForReset(MemberItem signal, string varname)
		{
			var itemtype = signal.ItemType;
			var vhdltype = m_globalInformation.GetOutputType(signal.Item, signal.ItemType);
			object initialvalue = base.GetInitialValueForReset(signal, varname);

			if (initialvalue == null && m_fieldInitializers.ContainsKey(signal.Name) && (vhdltype.IsSystemType || vhdltype.IsArray))
			{
				initialvalue = m_fieldInitializers[signal.Name];
				if (vhdltype.IsArray && !(initialvalue is ArrayCreateExpression))
					initialvalue = null;
			}

			return initialvalue;
		}

		public override void EmitForLoopBegin(string loopvariable, int startvalue, int endvalue)
		{
			m_sb.AppendFormat("for {0} in {1} to {2} loop", loopvariable, startvalue, endvalue - 1);
			m_sb.AppendLine();
		}

		public override void EmitForLoopEnd(string loopvariable, int startvalue, int endvalue)
		{
			m_sb.AppendLine("end loop;");			
		}

		public override void OutputSwitchStatement(SwitchStatement s)
		{
			var exp = ResolveExpression(s.Expression);

			m_sb.AppendFormat("case {0} is", exp.ResolvedString);
			m_sb.AppendLine();

			m_sb.Indentation += 4;
			var hasOthers = false;
			foreach (var b in s.SwitchSections)
			{
				if (b.CaseLabels.Count() == 1 && b.CaseLabels.First().Expression == Expression.Null)
				{
					hasOthers = true;
					m_sb.AppendFormat("when others =>");
				}
				else
					m_sb.AppendFormat("when {0} =>", string.Join(" | ", b.CaseLabels.Select(x => WrapConverted(ResolveExpression(x.Expression), exp.VHDLType).ResolvedString)));
				m_sb.AppendLine();
				m_sb.Indentation += 4;
				foreach (var ss in b.Statements)
					if (!(ss is BreakStatement))
						OutputStatement(ss);
				m_sb.Indentation -= 4;
			}

			if (!hasOthers)
			{
				m_sb.AppendFormat("when others =>");
				m_sb.AppendLine();
			}

			m_sb.Indentation -= 4;
			m_sb.AppendFormat("end case;");
			m_sb.AppendLine();
		}

		public override void OutputIfElseStatement(IfElseStatement s)
		{
			var condition = WrapConverted(ResolveExpression(s.Condition), VHDLTypes.BOOL);

			m_sb.AppendFormat("if {0} then", condition.ResolvedString);
			m_sb.AppendLine();
			m_sb.Indentation += 4;
			OutputStatement(s.TrueStatement);
			m_sb.Indentation -= 4;
			if (!s.FalseStatement.IsZero() && !(s.FalseStatement is EmptyStatement) && (s.FalseStatement != Statement.Null))
			{
				m_sb.Append("else");
				m_sb.AppendLine();
				m_sb.Indentation += 4;
				OutputStatement(s.FalseStatement);
				m_sb.Indentation -= 4;
			}
			m_sb.Append("end if;");
			m_sb.AppendLine();
		}

		public override IEnumerable<string> GetTypeDefinitions()
		{
			foreach (var m in m_fieldInitializers)
			{
				var vhdlexpr = ResolveExpression(new IdentifierExpression(m.Key));
				var resolvedItem = ResolveIdentifierItem(m.Key); //vhdlexpr.ResolvedItem;

				var val = m.Value;

				if (val is IMemberDefinition)
				{
					var mr = val as IMemberDefinition;
					if (mr is FieldDefinition && m_globalInformation.Constants.ContainsKey(mr as FieldDefinition))
						val = mr.ToValidName(Information, mr.DeclaringType, null);
					else if (mr.DeclaringType == m_typedef)
						val = m_globalInformation.ToValidName(mr.Name);
					else
						val = mr.ToValidName(Information, m_typedef, null);

					val = string.Format("TO_INTEGER(UNSIGNED({0}))", val);
				}

				var vhdltype = resolvedItem.ItemType;

				if (vhdltype.IsArray)
				{
					if (val is ArrayCreateExpression)
						val = (val as ArrayCreateExpression).Initializer.Children.Count();

					vhdltype = vhdltype.GetElementType();

					var vlt = Information.VHDLTypes.GetVHDLType(vhdltype);
					if (vlt.IsSystemType)
					{
						yield return string.Format("subtype {0} is {2}_ARRAY(0 to {1} - 1)", m_globalInformation.ToValidName(m.Key + "_type"), val, vlt.ToString());
					}
					else
					{
						yield return string.Format("type {0} is array(0 to {1} - 1) of {2}", m_globalInformation.ToValidName(m.Key + "_type"), val, vlt.ToString());
					}
				}
			}

			foreach (var m in m_compiledMethods)
			{
				foreach (var p in m.Key.Parameters)
				{
					if (p.ParameterType.IsArray)
					{
						var argrange = p.GetAttribute<RangeAttribute>();
						var vhdltype = Information.VHDLTypes.GetVHDLType(p.ParameterType.GetElementType());
						if (vhdltype.IsSystemType)
						{
							if (argrange != null)
								yield return string.Format("subtype {0} is {3}_ARRAY({1} to {2} - 1)", m_globalInformation.ToValidName(m.Key.Name + "_" + p.Name + "_type"), argrange.ConstructorArguments.Count == 2 ? (int)argrange.ConstructorArguments.First().Value : 0, (int)argrange.ConstructorArguments.Last().Value, vhdltype.ToString());
							continue;
						}

						if (argrange == null)
							yield return string.Format("type {0} is array(natural range <>) of {1}", m_globalInformation.ToValidName(m.Key.Name + "_" + p.Name + "_type"), vhdltype.ToString());
						else
							yield return string.Format("type {0} is array({1} to {2} - 1) of {3}", m_globalInformation.ToValidName(m.Key.Name + "_" + p.Name + "_type"), argrange.ConstructorArguments.Count == 2 ? (int)argrange.ConstructorArguments.First().Value : 0, (int)argrange.ConstructorArguments.Last().Value, vhdltype.ToString());
					}
				}
			}
		}
	}
}
