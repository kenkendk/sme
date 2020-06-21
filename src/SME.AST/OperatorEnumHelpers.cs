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

