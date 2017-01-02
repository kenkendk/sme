using System;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;

namespace SME.Render.VHDL.ILConvert.AugmentedExpression
{
	public class VHDLBinaryOperatorExpression : VHDLTypedExpression<BinaryOperatorExpression>
	{
		public VHDLBinaryOperatorExpression(Converter converter, BinaryOperatorExpression expression)
			: base(converter, expression)
		{
		}

		private IVHDLExpression m_lhs;
		private IVHDLExpression m_rhs;

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

		public override TypeReference ResolvedSourceType
		{
			get
			{
				if (m_resolvedSourceType == null)
					ResolveLhsAndRhs();

				return m_resolvedSourceType;
			}
		}

		public override VHDLTypeDescriptor VHDLType
		{
			get
			{
				if (m_vhdlType == null)
					ResolveLhsAndRhs();
				return m_vhdlType;
			}
		}
			
		private void ResolveLhsAndRhs()
		{
			var re = Expression.Right;
			var le = Expression.Left;

			var iscastexpr_right = Expression.Right is CastExpression;
			var iscastexpr_left = Expression.Left is CastExpression;
			/*if (iscastexpr_right)
				re = (Expression.Right as CastExpression).Expression;
			if (iscastexpr_left)
				le = (Expression.Left as CastExpression).Expression;*/

			var lhs = Converter.ResolveExpression(le);
			var rhs = Converter.ResolveExpression(re);

			if (le is PrimitiveExpression && !(re is PrimitiveExpression) && Expression.Operator != BinaryOperatorType.ShiftLeft && Expression.Operator != BinaryOperatorType.ShiftRight)
			{
				m_vhdlType = rhs.VHDLType;
				m_resolvedSourceType = rhs.ResolvedSourceType;
			}
			else
			{
				m_vhdlType = lhs.VHDLType;
				m_resolvedSourceType = lhs.ResolvedSourceType;
			}

			if ((Expression.Operator.IsArithmeticOperator() || Expression.Operator.IsCompareOperator()))
			{
				if (m_vhdlType != VHDLTypes.INTEGER)
				{
					m_vhdlType = Converter.Information.VHDLTypes.NumericEquivalent(m_vhdlType, false) ?? m_vhdlType;
				}
			}
			else if (Expression.Operator == BinaryOperatorType.ShiftLeft || Expression.Operator == BinaryOperatorType.ShiftRight)
				m_vhdlType = Converter.Information.VHDLTypes.NumericEquivalent(m_vhdlType);
			else if (Expression.Operator.IsBitwiseOperator())
				m_vhdlType = Converter.Information.VHDLTypes.SystemEquivalent(m_vhdlType);
				
			
			var lhstype = Expression.Operator.IsLogicalOperator() ? VHDLTypes.BOOL : m_vhdlType;
			var rhstype = Expression.Operator.IsLogicalOperator() ? VHDLTypes.BOOL : m_vhdlType;

			// Overrides for special types
			switch (Expression.Operator)
			{
				case BinaryOperatorType.ShiftLeft:
				case BinaryOperatorType.ShiftRight:
					rhstype = VHDLTypes.INTEGER;
					break;
			}

			m_lhs = Converter.WrapConverted(Converter.WrapIfComposite(lhs), lhstype);
			m_rhs = Converter.WrapIfComposite(Converter.WrapConverted(Converter.WrapIfComposite(rhs), rhstype, iscastexpr_right || iscastexpr_left));

			if (Expression.Operator.IsCompareOperator() || Expression.Operator.IsLogicalOperator())
			{
				m_vhdlType = VHDLTypes.BOOL;
				m_resolvedSourceType = Converter.ImportType<bool>();
			}
		}

		protected override string ResolveToString()
		{
			var op = Expression.Operator.ToVHDL();

			if (Expression.Operator == BinaryOperatorType.ShiftLeft)
				return string.Format("shift_left({0}, {1})", Left.ResolvedString, Right.ResolvedString);
			else if (Expression.Operator == BinaryOperatorType.ShiftRight)
				return string.Format("shift_right({0}, {1})", Left.ResolvedString, Right.ResolvedString);

			var res = string.Format("{0} {1} {2}", Converter.WrapIfComposite(Left).ResolvedString, op, Converter.WrapIfComposite(Right).ResolvedString);

			// We get a doubling of bits, lets clip it
			if (Expression.Operator == BinaryOperatorType.Multiply && (VHDLType.IsNumeric || VHDLType.IsSystemType))
				res = string.Format("resize({0}, {1})", res, string.IsNullOrWhiteSpace(VHDLType.Alias) ? VHDLType.Length.ToString() : VHDLType.Alias + "'length");

			return res;
		}
	}
}

