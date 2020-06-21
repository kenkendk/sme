using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SME.AST
{
	public static class OperatorEnumHelpers
	{
		public static SyntaxKind ToBinaryOperator(this SyntaxKind op)
		{
			switch(op)
			{
				case SyntaxKind.PlusEqualsToken:
					return SyntaxKind.PlusToken;
				case SyntaxKind.MinusEqualsToken:
					return SyntaxKind.MinusToken;
				case SyntaxKind.AsteriskEqualsToken:
					return SyntaxKind.AsteriskToken;
				case SyntaxKind.SlashEqualsToken:
					return SyntaxKind.SlashToken;
				case SyntaxKind.PercentEqualsToken:
					return SyntaxKind.PercentToken;
				case SyntaxKind.LessThanLessThanEqualsToken:
					return SyntaxKind.LessThanLessThanToken;
				case SyntaxKind.GreaterThanGreaterThanEqualsToken:
					return SyntaxKind.GreaterThanGreaterThanToken;
				case SyntaxKind.AmpersandEqualsToken:
					return SyntaxKind.AmpersandToken;
				case SyntaxKind.BarEqualsToken:
					return SyntaxKind.BarToken;
				case SyntaxKind.CaretEqualsToken:
					return SyntaxKind.CaretToken;
				default:
					throw new Exception(string.Format("Cannot convert assignment operator {0} to BinaryOperator", op));
			}
		}

		public static bool IsLogicalOperator(this SyntaxKind self)
		{
			switch (self)
			{
				case SyntaxKind.AmpersandAmpersandToken:
				case SyntaxKind.BarBarToken:
					return true;
			}

			return false;
		}

		public static bool IsCompareOperator(this SyntaxKind self)
		{
			switch (self)
			{
				case SyntaxKind.GreaterThanToken:
				case SyntaxKind.GreaterThanEqualsToken:
				case SyntaxKind.EqualsEqualsToken:
				case SyntaxKind.ExclamationEqualsToken:
				case SyntaxKind.LessThanToken:
				case SyntaxKind.LessThanEqualsToken:
					return true;
			}

			return false;
		}

		public static bool IsBitwiseOperator(this SyntaxKind self)
		{
			switch (self)
			{
				case SyntaxKind.AmpersandToken: // &
				case SyntaxKind.BarToken: // |
				case SyntaxKind.LessThanLessThanToken: // <<
				case SyntaxKind.GreaterThanGreaterThanToken: // >>
				case SyntaxKind.CaretToken: // ^
					return true;
			}

			return false;
		}

		public static bool IsArithmeticOperator(this SyntaxKind self)
		{
			switch(self)
			{
				case SyntaxKind.PlusToken: // +
				case SyntaxKind.MinusToken: // -
				case SyntaxKind.AsteriskToken: // *
				case SyntaxKind.SlashToken: // /
				case SyntaxKind.PercentToken: // %
					return true;
			}

			return false;
		}
	}
}

