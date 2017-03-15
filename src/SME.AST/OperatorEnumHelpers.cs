using System;
using ICSharpCode.NRefactory.CSharp;

namespace SME.AST
{
	public static class OperatorEnumHelpers
	{
		public static BinaryOperatorType ToBinaryOperator(this AssignmentOperatorType op)
		{
			switch(op)
			{
				case AssignmentOperatorType.Add:
					return BinaryOperatorType.Add;
				case AssignmentOperatorType.Subtract:
					return BinaryOperatorType.Subtract;
				case AssignmentOperatorType.Multiply:
					return BinaryOperatorType.Modulus;
				case AssignmentOperatorType.Divide:
					return BinaryOperatorType.Divide;
				case AssignmentOperatorType.Modulus:
					return BinaryOperatorType.Modulus;
				case AssignmentOperatorType.ShiftLeft:
					return BinaryOperatorType.ShiftLeft;
				case AssignmentOperatorType.ShiftRight:
					return BinaryOperatorType.ShiftRight;
				case AssignmentOperatorType.BitwiseAnd:
					return BinaryOperatorType.BitwiseAnd;
				case AssignmentOperatorType.BitwiseOr:
					return BinaryOperatorType.BitwiseOr;
				case AssignmentOperatorType.ExclusiveOr:
					return BinaryOperatorType.ExclusiveOr;
				case AssignmentOperatorType.Assign:
				case AssignmentOperatorType.Any:
				default:
					throw new Exception(string.Format("Cannot convert assignment operator {0} to BinaryOperator", op));
			}
		}

		public static bool IsLogicalOperator(this BinaryOperatorType self)
		{
			switch (self)
			{
				case BinaryOperatorType.ConditionalAnd:
				case BinaryOperatorType.ConditionalOr:
					return true;
			}

			return false;
		}

		public static bool IsCompareOperator(this BinaryOperatorType self)
		{
			switch (self)
			{
				case BinaryOperatorType.GreaterThan:
				case BinaryOperatorType.GreaterThanOrEqual:
				case BinaryOperatorType.Equality:
				case BinaryOperatorType.InEquality:
				case BinaryOperatorType.LessThan:
				case BinaryOperatorType.LessThanOrEqual:
					return true;
			}

			return false;
		}

		public static bool IsBitwiseOperator(this BinaryOperatorType self)
		{
			switch (self)
			{
				case BinaryOperatorType.BitwiseAnd:
				case BinaryOperatorType.BitwiseOr:
				case BinaryOperatorType.ShiftLeft:
				case BinaryOperatorType.ShiftRight:
				case BinaryOperatorType.ExclusiveOr:
					return true;
			}

			return false;
		}

		public static bool IsArithmeticOperator(this BinaryOperatorType self)
		{
			switch(self)
			{
				case BinaryOperatorType.Add:
				case BinaryOperatorType.Subtract:
				case BinaryOperatorType.Multiply:
				case BinaryOperatorType.Divide:
				case BinaryOperatorType.Modulus:
					return true;
			}

			return false;
		}
	}
}

