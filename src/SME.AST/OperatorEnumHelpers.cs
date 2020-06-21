using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

		public static bool IsLogicalOperator(this SyntaxToken self)
		{
			switch (self.RawKind)
			{
				case (int)SyntaxKind.AmpersandAmpersandToken:
				case (int)SyntaxKind.BarBarToken:
					return true;
			}

			return false;
		}

		public static bool IsCompareOperator(this SyntaxToken self)
		{
			switch (self.RawKind)
			{
				case (int)SyntaxKind.GreaterThanToken:
				case (int)SyntaxKind.GreaterThanEqualsToken:
				case (int)SyntaxKind.EqualsEqualsToken:
				case (int)SyntaxKind.ExclamationEqualsToken:
				case (int)SyntaxKind.LessThanToken:
				case (int)SyntaxKind.LessThanEqualsToken:
					return true;
			}

			return false;
		}

		public static bool IsBitwiseOperator(this SyntaxToken self)
		{
			switch (self.RawKind)
			{
				case (int)SyntaxKind.AmpersandToken: // &
				case (int)SyntaxKind.BarToken: // |
				case (int)SyntaxKind.LessThanLessThanToken: // <<
				case (int)SyntaxKind.GreaterThanGreaterThanToken: // >>
				case (int)SyntaxKind.CaretToken: // ^
					return true;
			}

			return false;
		}

		public static bool IsArithmeticOperator(this SyntaxToken self)
		{
			switch(self.RawKind)
			{
				case (int)SyntaxKind.PlusToken: // +
				case (int)SyntaxKind.MinusToken: // -
				case (int)SyntaxKind.AsteriskToken: // *
				case (int)SyntaxKind.SlashToken: // /
				case (int)SyntaxKind.PercentToken: // %
					return true;
			}

			return false;
		}
	}
}

