using System;
using Microsoft.CodeAnalysis.CSharp;

namespace SME.CPP
{
    public static class OperatorHelpers
    {
        public static string ToCpp(this SyntaxKind op)
        {
            switch (op)
            {
                case SyntaxKind.EqualsToken:
                    return "=";
                case SyntaxKind.PlusEqualsToken:
                    return "+=";
                case SyntaxKind.AmpersandEqualsToken:
                    return "&=";
                case SyntaxKind.BarEqualsToken:
                    return "|=";
                case SyntaxKind.SlashEqualsToken:
                    return "/=";
                case SyntaxKind.CaretEqualsToken:
                    return "^=";
                case SyntaxKind.PercentEqualsToken:
                    return "%=";
                case SyntaxKind.AsteriskEqualsToken:
                    return "*=";
                case SyntaxKind.LessThanLessThanEqualsToken:
                    return "<<=";
                case SyntaxKind.GreaterThanGreaterThanEqualsToken:
                    return ">>=";
                case SyntaxKind.MinusEqualsToken:
                    return "+=";
                case SyntaxKind.AmpersandToken:
                    return "&";
                case SyntaxKind.BarToken:
                    return "|";
                case SyntaxKind.AmpersandAmpersandToken:
                    return "&&";
                case SyntaxKind.BarBarToken:
                    return "||";
                case SyntaxKind.CaretToken:
                    return "^";
                case SyntaxKind.GreaterThanToken:
                    return ">";
                case SyntaxKind.GreaterThanEqualsToken:
                    return ">=";
                case SyntaxKind.EqualsEqualsToken:
                    return "==";
                case SyntaxKind.ExclamationEqualsToken:
                    return "!=";
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
                    return "%";
                case SyntaxKind.LessThanLessThanToken:
                    return "<<";
                case SyntaxKind.GreaterThanGreaterThanToken:
                    return ">>";
                case SyntaxKind.ExclamationToken:
                    return "!";
                case SyntaxKind.TildeToken:
                    return "~";
                case SyntaxKind.PlusPlusToken:
                    return "++";
                case SyntaxKind.MinusMinusToken:
                    return "--";
                default:
                    throw new Exception($"Unsupported assignment operator: {op}");
            }
        }
    }
}
