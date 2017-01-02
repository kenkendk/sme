using System;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;

namespace SME.Render.VHDL.ILConvert.AugmentedExpression
{
	public class VHDLUnaryOperatorExpression : VHDLTypedExpression<UnaryOperatorExpression>
	{
		public VHDLUnaryOperatorExpression(Converter converter, UnaryOperatorExpression expression)
			: base(converter, expression)
		{
		}

		private IVHDLExpression m_operand;

		public IVHDLExpression Operand
		{
			get
			{
				if (m_operand == null)
					m_operand = Converter.ResolveExpression(Expression.Expression);

				return m_operand;
			}
		}


		public override TypeReference ResolvedSourceType
		{
			get
			{
				if (m_resolvedSourceType == null)
					m_resolvedSourceType = Operand.ResolvedSourceType;
				
				return m_resolvedSourceType;
			}
		}

		public override VHDLTypeDescriptor VHDLType
		{
			get
			{
				if (Expression.Operator == UnaryOperatorType.Not && Operand.VHDLType == VHDLTypes.SYSTEM_BOOL)
					return VHDLTypes.BOOL;
				else
					return Operand.VHDLType;
			}
		}

		protected override string ResolveToString()
		{
			var op = Expression.Operator.ToVHDL();

			if (Expression.Operator == UnaryOperatorType.Not && Operand.VHDLType == VHDLTypes.SYSTEM_BOOL)
				return string.Format("{0} = '0'", Operand.ResolvedString);


			var one = new PrimitiveExpression(1);
			var expisvariable = IsVariableExpression(Operand);

			IVHDLExpression rhs = null;
			if (Expression.Operator == UnaryOperatorType.PostIncrement || Expression.Operator == UnaryOperatorType.Increment)
				rhs = Converter.ResolveExpression(new BinaryOperatorExpression(Expression.Expression.Clone(), BinaryOperatorType.Add, one));
			else if (Expression.Operator == UnaryOperatorType.PostDecrement || Expression.Operator == UnaryOperatorType.Decrement)
				rhs = Converter.ResolveExpression(new BinaryOperatorExpression(Expression.Expression.Clone(), BinaryOperatorType.Subtract, one));
			else
				rhs = Operand;

			rhs = Converter.WrapConverted(rhs, Operand.VHDLType);

			var assignOperator = expisvariable ? ":=" : "<=";

			if (Operand is VHDLMemberReferenceExpression)
				Converter.RegisterSignalWrite((VHDLMemberReferenceExpression)Operand, true);

			switch (Expression.Operator)
			{
				case UnaryOperatorType.PostIncrement:
					Converter.PostPendline("{0} {1} {2};", Operand.ResolvedString, assignOperator, rhs.ResolvedString);
					if (Expression.Parent is ExpressionStatement)
						return "";
					else
						return Operand.ResolvedString;

				case UnaryOperatorType.PostDecrement:
					Converter.PostPendline("{0} {1} {2};", Operand.ResolvedString, assignOperator, rhs.ResolvedString);
					if (Expression.Parent is ExpressionStatement)
						return "";
					else
						return Operand.ResolvedString;

				case UnaryOperatorType.Increment:
					if (!(Expression.Parent is AssignmentExpression) || Converter.ResolveExpression((Expression.Parent as AssignmentExpression).Left).Expression.ToString() != Operand.Expression.ToString())
						Converter.PrePendline("{0} {1} {2};", Operand.ResolvedString, assignOperator, rhs.ResolvedString);
					return rhs.ResolvedString;
				case UnaryOperatorType.Decrement:
					if (!(Expression.Parent is AssignmentExpression) || Converter.ResolveExpression((Expression.Parent as AssignmentExpression).Left).Expression.ToString() != Operand.Expression.ToString())
						Converter.PrePendline("{0} {1} {2};", Operand.ResolvedString, assignOperator, rhs.ResolvedString);
					return rhs.ResolvedString;
				default:
					return string.Format("{0} {1}", op, Operand.ResolvedString);
			}
		}
	}
}

