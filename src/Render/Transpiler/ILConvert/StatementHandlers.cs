using System;
using System.Linq;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.Decompiler;
using ICSharpCode.NRefactory.TypeSystem;
using Mono.Cecil;

namespace SME.Render.Transpiler.ILConvert
{
	public abstract partial class Converter<TExpression, TTypeClass>
		where TExpression : IAugmentedExpression
		where TTypeClass : class
	{
		protected string ReturnVariable { get; set; }

		public virtual void OutputStatement(AstNode n)
		{
			if (n is ExpressionStatement)
				OutputStatement(n as ExpressionStatement);
			else if (n is IfElseStatement)
				OutputIfElseStatement(n as IfElseStatement);
			else if (n is BlockStatement)
				OutputBlockStatement(n as BlockStatement);
			else if (n is VariableDeclarationStatement)
				OutputVariableStatement(n as VariableDeclarationStatement);
			else if (n is SwitchStatement)
				OutputSwitchStatement(n as SwitchStatement);
			else if (n is ReturnStatement)
				OutputReturnStatement(n as ReturnStatement);
			else if (n is ForStatement)
				OutputForStatement(n as ForStatement);
			else if (n is CheckedStatement)
			{
				Console.WriteLine("Warning: \"checked\" is not supported and will be ignored for statement: {0}", n);
				OutputStatement((n as CheckedStatement).Body);
			}
			else if (n is UncheckedStatement)
				OutputStatement((n as UncheckedStatement).Body);
			else			
				throw new Exception(string.Format("Unsupported statement: {0} ({1})", n, n.GetType().FullName));
		}


		public int ResolveArrayLengthOrPrimitive(Expression src)
		{
			if (src is PrimitiveExpression)
				try
				{
					return Convert.ToInt32((src as PrimitiveExpression).Value);
				}
				catch (Exception ex)
				{
					throw new Exception(string.Format("Unable to resolve as a constant value: {0}", src), ex);
				}

			var ex_left = src as MemberReferenceExpression;
			if (ex_left.MemberName != "Length")
				throw new Exception(string.Format("Only plain style for loops supported: {0}", src));

			var member = ResolveMemberReference(ex_left.Target);
			if (member.ItemType.IsFixedArrayType())
				return member.GetFixedArrayLength();

			var value = m_fieldInitializers[member.Name];

			if (value is ArrayCreateExpression)
				return (value as ArrayCreateExpression).Initializer.Children.Count();

			if (value is IMemberReference)
			{
				try
				{
					var mr = value as IMemberDefinition;
					if (mr is FieldDefinition && m_globalInformation.Constants.ContainsKey(mr as FieldDefinition))
						return ResolveArrayLengthOrPrimitive(new PrimitiveExpression(m_globalInformation.Constants[mr as FieldDefinition]));
					else if (mr.DeclaringType == m_typedef && m_fieldInitializers.ContainsKey(mr.Name))
						return ResolveArrayLengthOrPrimitive(new PrimitiveExpression(m_fieldInitializers[mr.Name]));
				}
				catch (Exception ex)
				{
					throw new Exception(string.Format("Unable to resolve as a constant value: {0}", src), ex);
				}
			}

			try
			{
				return Convert.ToInt32(value);
			}
			catch (Exception ex)
			{
				throw new Exception(string.Format("Unable to resolve as a constant value: {0}", src), ex);
			}
		}

		public virtual void OutputForStatement(ForStatement f)
		{
			if (f.Initializers.Count != 1)
				throw new Exception(string.Format("Only plain style for loops supported: {0}", f));

			if (f.Iterators.Count != 1)
				throw new Exception(string.Format("Only plain style for loops supported: {0}", f));
			

			var init = f.Initializers.First() as VariableDeclarationStatement;
			if (init == null)
				throw new Exception(string.Format("Only plain style for loops supported: {0}", f));

			if (init.Variables.Count != 1)
				throw new Exception(string.Format("Only plain style for loops supported: {0}", f));

			var name = init.Variables.First().Name;
			var initial = init.Variables.First().Initializer as PrimitiveExpression;

			if (initial == null || !(initial.Value is int))
				throw new Exception(string.Format("Only plain style for loops supported: {0}", f));

			var startvalue = (int)initial.Value;

			var cond = f.Condition as BinaryOperatorExpression;
			if (cond == null)
				throw new Exception(string.Format("Only plain style for loops supported: {0}", f));

			if (cond.Operator != BinaryOperatorType.LessThan)
				throw new Exception(string.Format("Only plain style for loops supported: {0}", f));

			var condleft = cond.Left as IdentifierExpression;
			var condright = cond.Right as PrimitiveExpression;

			// Handling cases where the upper limit is the length of an array
			if (condright == null)
			{
				// Some plus/minus expression
				if (cond.Right is BinaryOperatorExpression)
				{
					var binop = cond.Right as BinaryOperatorExpression;

					var leftval = ResolveArrayLengthOrPrimitive(binop.Left);
					var righval = ResolveArrayLengthOrPrimitive(binop.Right);

					if (binop.Operator == BinaryOperatorType.Add)
						condright = new PrimitiveExpression(leftval + righval);
					else if (binop.Operator == BinaryOperatorType.Subtract)
						condright = new PrimitiveExpression(leftval - righval);
					else
						throw new Exception(string.Format("Only add and subtract operations are supported in for loop bounds: {0}", f));
				}
				// Plain limit
				else if (cond.Right is IdentifierExpression || cond.Right is MemberReferenceExpression)
				{
					condright = new PrimitiveExpression(ResolveArrayLengthOrPrimitive(cond.Right));
				}
			}

			if (condleft == null || condright == null || !(condright.Value is int) || condleft.Identifier != name)
				throw new Exception(string.Format("Only plain style for loops supported: {0}", f));

			var endvalue = (int)condright.Value;
			PrimitiveExpression increment = null;

			var itr = f.Iterators.First() as ExpressionStatement;
			if (itr == null || !(itr.Expression is UnaryOperatorExpression))
			{
				var ae = itr == null ? null : itr.Expression as AssignmentExpression;

				if (ae != null && ae.Left is IdentifierExpression && (ae.Left as IdentifierExpression).Identifier == name && ae.Right is PrimitiveExpression)
				{
					// Support for increments like "i += 2"
					increment = ae.Right as PrimitiveExpression;
					itr = new ExpressionStatement(
						new UnaryOperatorExpression(
							UnaryOperatorType.PostIncrement,
							ae.Left.Clone()
						)
					);

					endvalue /= (int)Convert.ChangeType((ae.Right as PrimitiveExpression).Value, typeof(int));
				}
				else
					throw new Exception(string.Format("Only plain style for loops supported: {0}", f));
			}

			var itre = itr.Expression as UnaryOperatorExpression;
			var itro = itre.Expression as IdentifierExpression;

			if (itro == null || itre.Operator != UnaryOperatorType.PostIncrement || itro.Identifier != name)
				throw new Exception(string.Format("Only plain style for loops supported: {0}", f));

			EmitForLoopBegin(m_globalInformation.ToValidName(name), startvalue, endvalue);
			m_sb.Indentation += 4;

			// Store any existing value so we can put it back
			Tuple<TypeReference, TTypeClass> prev;
			m_localVariables.TryGetValue(name, out prev);
			string tmpreg = null;
			string prevrename = null;
			var inttype = m_asm.MainModule.Import(typeof(int));

			m_localVariables[name] = new Tuple<TypeReference, TTypeClass>(m_asm.MainModule.Import(typeof(int)), m_globalInformation.GetNativeIntegerType());

			if (increment != null)
			{
				tmpreg = RegisterTemporaryVariable(m_asm.MainModule.Import(typeof(int)), m_globalInformation.GetNativeIntegerType());
				OutputStatement(
					new ExpressionStatement(
						new AssignmentExpression(
							new IdentifierExpression(m_globalInformation.ToValidName(tmpreg)),
							new BinaryOperatorExpression(
								new IdentifierExpression(m_globalInformation.ToValidName(name)),
								BinaryOperatorType.Multiply,
								increment.Clone()
							)
						)
					)
				);

				m_localRenames.TryGetValue(name, out prevrename);
				m_localRenames[name] = tmpreg;
			}

			OutputStatement(f.EmbeddedStatement);

			// Restore any previous value
			m_localVariables.Remove(name);
			if (prev != null)
				m_localVariables[name] = prev;

			if (tmpreg != null)
			{
				m_localRenames.Remove(name);
				if (prevrename != null)
					m_localRenames[name] = prevrename;
			}

			m_sb.Indentation -= 4;
			EmitForLoopEnd(m_globalInformation.ToValidName(name), startvalue, endvalue);
		}


		public abstract void EmitForLoopBegin(string loopvariable, int startvalue, int endvalue);
		public abstract void EmitForLoopEnd(string loopvariable, int startvalue, int endvalue);

		public virtual void OutputReturnStatement(ReturnStatement r)
		{
			OutputStatement(new ExpressionStatement(
				new AssignmentExpression(
					new IdentifierExpression(ReturnVariable), r.Expression.Clone()
				)
			));

			m_sb.AppendFormat("return {0};", ReturnVariable);
			m_sb.AppendLine();
		}

		public virtual void OutputBlockStatement(BlockStatement b)
		{
			foreach (var n in b.Children)
				OutputStatement(n);
		}

		public abstract void OutputSwitchStatement(SwitchStatement s);

		public abstract void OutputIfElseStatement(IfElseStatement s);

		private void OutputStatement(ExpressionStatement s)
		{
			if (typeof(ExpressionStatement) == s.GetType())
			{
				var e = ResolveExpression(s.Expression);
				if (!string.IsNullOrWhiteSpace(e.ResolvedString))
					m_sb.AppendLine(e.ResolvedString + ";");
				else
					m_sb.Flush();
			}
			else
				throw new Exception(string.Format("Unsupported expression statement: {0} ({1})", s, s.GetType().FullName));
		}


		private void OutputVariableStatement(VariableDeclarationStatement s)
		{
			var vartype = ResolveType(s.Type);
			if (vartype.IsBusType())
			{
				foreach (var n in s.Variables)
				{
					if (n.Initializer is MemberReferenceExpression)
					{
						m_busVariableMap[n.Name] = ResolveIdentifierItem((n.Initializer as MemberReferenceExpression).MemberName); // new VHDLMemberReferenceExpression(this, n.Initializer as MemberReferenceExpression).Member;
					}
					else
					{
						var match = m_typedef.Fields.Where(x => x.FieldType.Resolve() == vartype).FirstOrDefault();
						if (match != null)
							m_busVariableMap[n.Name] = new MemberItem(n.Initializer, m_globalInformation.StoreType(match), match.DeclaringType);
						else
							Console.WriteLine("Unable to determine what bus is assigned to variable {0}", n.Name);
					}
				}
			}
			else
			{
				foreach (var n in s.Variables)
				{
					m_localVariables.Add(n.Name, new Tuple<TypeReference, TTypeClass>(vartype, m_globalInformation.GetOutputType((TypeReference)vartype)));
					if (!n.Initializer.IsNull)
						OutputStatement(new ExpressionStatement(new AssignmentExpression(new IdentifierExpression(n.Name), n.Initializer.Clone())));
				}
			}
		}
	}
}

