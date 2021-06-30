using System;
using Microsoft.CodeAnalysis.CSharp;

namespace SME.AST
{
    /// <summary>
    /// Static helper class containing methods for converting operators.
    /// <summary>
    public static class OperatorEnumHelpers
    {
        /// <summary>
        /// Converts a given composite operator into the corresponding binary operator.
        /// </summary>
        /// <param name="op">The composite operator to convert.</param>
        public static SyntaxKind ToBinaryOperator(this SyntaxKind op)
        {
            switch(op)
            {
                case SyntaxKind.PlusEqualsToken: // +=
                    return SyntaxKind.PlusToken;
                case SyntaxKind.MinusEqualsToken: // -=
                    return SyntaxKind.MinusToken;
                case SyntaxKind.AsteriskEqualsToken: // *=
                    return SyntaxKind.AsteriskToken;
                case SyntaxKind.SlashEqualsToken: // /=
                    return SyntaxKind.SlashToken;
                case SyntaxKind.PercentEqualsToken: // %=
                    return SyntaxKind.PercentToken;
                case SyntaxKind.LessThanLessThanEqualsToken: // <<=
                    return SyntaxKind.LessThanLessThanToken;
                case SyntaxKind.GreaterThanGreaterThanEqualsToken: // >>=
                    return SyntaxKind.GreaterThanGreaterThanToken;
                case SyntaxKind.AmpersandEqualsToken: // &=
                    return SyntaxKind.AmpersandToken;
                case SyntaxKind.BarEqualsToken: // |=
                    return SyntaxKind.BarToken;
                case SyntaxKind.CaretEqualsToken: // ^=
                    return SyntaxKind.CaretToken;
                default:
                    throw new Exception(string.Format("Cannot convert assignment operator {0} to BinaryOperator", op));
            }
        }

        /// <summary>
        /// Returns true, if the given operator is a logical operator.
        /// </summary>
        /// <param name="self">The given operator.</param>
        public static bool IsLogicalOperator(this SyntaxKind self)
        {
            switch (self)
            {
                case SyntaxKind.AmpersandAmpersandToken: // &&
                case SyntaxKind.BarBarToken: // ||
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true, if the given operator is a comparison operator.
        /// </summary>
        /// <param name="self">The given operator.</param>
        public static bool IsCompareOperator(this SyntaxKind self)
        {
            switch (self)
            {
                case SyntaxKind.GreaterThanToken: // >
                case SyntaxKind.GreaterThanEqualsToken: // >=
                case SyntaxKind.EqualsEqualsToken: // ==
                case SyntaxKind.ExclamationEqualsToken: // !=
                case SyntaxKind.LessThanToken: // <
                case SyntaxKind.LessThanEqualsToken: // <=
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true, if the given operator is a bitwise operator.
        /// </summary>
        /// <param name="self">The given operator.</param>
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

        /// <summary>
        /// Returns true, if the given operator is an arithmetic operator.
        /// </summary>
        /// <param name="self">The given operator.</param>
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

