using System;
using Microsoft.CodeAnalysis.CSharp;

namespace SME.VHDL
{
    /// <summary>
    /// Helper class for converting C# operators to VHDL operators.
    /// </summary>
    public static class OperatorHelpers
    {
        /// <summary>
        /// Converts the given C# operator to the corresponding VHDL operator as a string.
        /// </summary>
        /// <param name="op">The given C# operator.</param>
        public static string ToVHDL(this SyntaxKind op)
        {
            switch (op)
            {
            case SyntaxKind.AmpersandToken:
                return "and";
            case SyntaxKind.BarToken:
                return "or";
            case SyntaxKind.AmpersandAmpersandToken:
                return "and";
            case SyntaxKind.BarBarToken:
                return "or";
            case SyntaxKind.CaretToken:
                return "xor";
            case SyntaxKind.GreaterThanToken:
                return ">";
            case SyntaxKind.GreaterThanEqualsToken:
                return ">=";
            case SyntaxKind.EqualsEqualsToken:
                return "=";
            case SyntaxKind.ExclamationEqualsToken:
                return "/=";
            case SyntaxKind.LessThanToken:
                return "<";
            case SyntaxKind.LessThanEqualsToken:
                return "<=";
            case SyntaxKind.PlusToken:
                return "+";
            case SyntaxKind.MinusToken:
                return "-";
            case SyntaxKind.AsteriskToken:
                return "*";
            case SyntaxKind.SlashToken:
                return "/";
            case SyntaxKind.PercentToken:
                return "mod";
            case SyntaxKind.LessThanLessThanToken:
                return "sll";
            case SyntaxKind.GreaterThanGreaterThanToken:
                return "srl";
            case SyntaxKind.ExclamationToken:
                return "not";
            case SyntaxKind.TildeToken:
                return "not";
            case SyntaxKind.PlusPlusToken:
                return "++";
            case SyntaxKind.MinusMinusToken:
                return "--";
            default:
                return string.Format("({0})", op);
            }
        }
    }
}
