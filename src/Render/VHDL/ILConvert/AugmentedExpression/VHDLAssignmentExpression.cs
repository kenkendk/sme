using System;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;

namespace SME.Render.VHDL.ILConvert.AugmentedExpression
{
	public class VHDLAssignmentExpression : VHDLTypedExpression<AssignmentExpression>
	{
		public VHDLAssignmentExpression(Converter converter, AssignmentExpression expression)
			: base(converter, expression)
		{
		}

		private IVHDLExpression m_lhs;
		private IVHDLExpression m_rhs;

		public override TypeReference ResolvedSourceType
		{
			get
			{
				return Left.ResolvedSourceType;
			}
		}

		public override VHDLTypeDescriptor VHDLType
		{
			get
			{
				return Left.VHDLType;
			}
		}

		public IVHDLExpression Left
		{
			get
			{
				if (m_lhs == null || m_rhs == null)
					ResolveLhsAndRhs();
				return m_lhs;
			}
		}

		public IVHDLExpression Right
		{
			get
			{
				if (m_lhs == null || m_rhs == null)
					ResolveLhsAndRhs();
				return m_rhs;
			}
		}

		private void ResolveLhsAndRhs()
		{
			m_lhs = Converter.ResolveExpression(Expression.Left);
			m_rhs = Converter.ResolveExpression(Expression.Right);
		}

		protected override string ResolveToString()
		{
			// Fix "x = x;" assignments being emitted from CIL
			if (Expression.Left.ToString() == Expression.Right.ToString() && Expression.Parent is ExpressionStatement && Expression.Operator == AssignmentOperatorType.Assign && Expression.Parent.ToString().Trim() == string.Format("{0} = {1};", Expression.Left, Expression.Right))
				return "";

			if (Left is VHDLMemberReferenceExpression)
				Converter.RegisterSignalWrite(Left as VHDLMemberReferenceExpression, true);
			else if (Left is VHDLIndexerExpression && (Left as VHDLIndexerExpression).Target is VHDLMemberReferenceExpression)
				Converter.RegisterSignalWrite((Left as VHDLIndexerExpression).Target as VHDLMemberReferenceExpression, true);


			if (Right is VHDLInvocationExpression && (Right as VHDLInvocationExpression).Target is VHDLMemberReferenceExpression)
			{				
				var member = ((VHDLMemberReferenceExpression)((VHDLInvocationExpression)Right).Target).Member;
				if (member.Item.DeclaringType.IsSameTypeReference(Converter.ImportType<Process>()) || member.GetAttribute<VHDLIgnoreAttribute>() != null)
					return "-- " + Expression.ToString();
			}

			if (Left is VHDLMemberReferenceExpression)
			{
				var member = (Left as VHDLMemberReferenceExpression).Member;

				// If the call is to the base class, or the target is an ignored field, suppress the output
				if (member.Item.DeclaringType.IsSameTypeReference(Converter.ImportType<Process>()) || member.GetAttribute<VHDLIgnoreAttribute>() != null)
					return "-- " + Expression.ToString();
			}


			var tmpright = Right;

			if ((Right is VHDLParenthesizedExpression))
				tmpright = (Right as VHDLParenthesizedExpression).Target;

			// Fix a = b = 1;
			if (tmpright is VHDLAssignmentExpression)
			{
				// Not handling non-simple chained assignments
				if (Expression.Operator == AssignmentOperatorType.Assign && (tmpright as VHDLAssignmentExpression).Expression.Operator == AssignmentOperatorType.Assign)
				{
					var tmpvar = Converter.RegisterTemporaryVariable(Left.ResolvedSourceType, Left.VHDLType);

					Converter.OutputStatement(
						new ExpressionStatement(
							new AssignmentExpression(
								new IdentifierExpression(tmpvar), (tmpright as VHDLAssignmentExpression).Right.Expression.Clone())
						)
					);

					Converter.OutputStatement(
						new ExpressionStatement(
							new AssignmentExpression(
								(tmpright as VHDLAssignmentExpression).Left.Expression.Clone(), new IdentifierExpression(tmpvar))
						)
					);

					tmpright = new VHDLIdentifierExpression(Converter, new IdentifierExpression(tmpvar));
				}
			}


			// Optimizing output if we do not have VHDL conditionals, 
			// but assign to a variable with the ternary operator
			if (tmpright is VHDLConditionalExpression && !Converter.SUPPORTS_VHDL_2008)
			{
				if (tmpright.Expression is ConditionalExpression)
				{
					var conds = tmpright.Expression as ConditionalExpression;

					Converter.OutputIfElseStatement(
						new IfElseStatement(conds.Condition.Clone(),
							new ExpressionStatement(
								new AssignmentExpression(Expression.Left.Clone(), conds.TrueExpression.Clone())
							),
							new ExpressionStatement(
								new AssignmentExpression(Expression.Left.Clone(), conds.FalseExpression.Clone())
							)
						)
					);

					return "";
				}
				else if (tmpright.Expression is BinaryOperatorExpression && (tmpright.Expression as BinaryOperatorExpression).Operator.IsCompareOperator())
				{
					var conds = tmpright.Expression as BinaryOperatorExpression;

					Converter.OutputIfElseStatement(
						new IfElseStatement(conds.Clone(),
							new ExpressionStatement(
								new AssignmentExpression(Expression.Left.Clone(), new PrimitiveExpression(true))
							),
							new ExpressionStatement(
								new AssignmentExpression(Expression.Left.Clone(), new PrimitiveExpression(false))
							)
						)
					);

					return "";
				}
			}
				
			// No need to do assignments with bus items
			if (Left.ResolvedSourceType.IsBusType())
				return "";

			var op = Expression.Operator;
			var rt = tmpright;

			var strippedcast = false;
			if (rt is VHDLCastExpression)
			{
				rt = Converter.ResolveExpression((rt as VHDLCastExpression).Expression.Expression);
				strippedcast = true;
			}

			if (op != AssignmentOperatorType.Assign)
			{
				rt = Converter.ResolveExpression(new BinaryOperatorExpression(Expression.Left.Clone(), op.ToBinaryOperator(), rt.Expression.Clone()));
				op = AssignmentOperatorType.Assign;
			}

			var lhs = Left.ResolvedString;
			if (Left is VHDLMemberReferenceExpression)
			{
				var mr = (Left as VHDLMemberReferenceExpression).Member;
				if (mr.DeclaringType.IsBusType() && !Converter.IsClockedProcess)
				{
					if (mr.DeclaringType.GetAttribute<InternalBusAttribute>() != null)
						lhs = "next_" + lhs;
					else
					{
						var vt = (Left as VHDLMemberReferenceExpression).BusVariableField;
						if (vt != null && vt.GetAttribute<InternalBusAttribute>() != null)
							lhs = "next_" + lhs;
					}
				}
			}
					
			// Optimize assigning boolean expression to boolean type
			if (rt.VHDLType == VHDLTypes.BOOL && rt is VHDLBinaryOperatorExpression && Left.VHDLType == VHDLTypes.SYSTEM_BOOL)
			{
				Converter.OutputIfElseStatement(
					new IfElseStatement(rt.Expression.Clone(),
						new ExpressionStatement(
							new AssignmentExpression(Expression.Left.Clone(), new PrimitiveExpression(true))
						),
						new ExpressionStatement(
							new AssignmentExpression(Expression.Left.Clone(), new PrimitiveExpression(false))
						)
					)
				);

				return "";				
			}

			var isVariableAssignment = IsVariableExpression(Left);

			return string.Format("{0} {1} {2}", lhs, isVariableAssignment ? ":=" : op.ToVHDL(), Converter.WrapConverted(rt, Left.VHDLType, strippedcast).ResolvedString);			
		}
	}
}

