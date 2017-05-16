using System;
using ICSharpCode.NRefactory.CSharp;

namespace SME.CPP
{
	public static class OperatorHelpers
	{
        public static string ToCpp(this AssignmentOperatorType op)
        {
            switch (op)
            {
                case AssignmentOperatorType.Assign:
                    return "=";
                case AssignmentOperatorType.Add:
                    return "+=";
                case AssignmentOperatorType.BitwiseAnd:
                    return "&=";
                case AssignmentOperatorType.BitwiseOr:
                    return "|=";
                case AssignmentOperatorType.Divide:
                    return "/=";
                case AssignmentOperatorType.ExclusiveOr:
                    return "^=";
                case AssignmentOperatorType.Modulus:
                    return "%=";
                case AssignmentOperatorType.Multiply:
                    return "*=";
                case AssignmentOperatorType.ShiftLeft:
                    return "<<=";
                case AssignmentOperatorType.ShiftRight:
                    return ">>=";
                case AssignmentOperatorType.Subtract:
                    return "+=";
                default:
                    throw new Exception($"Unsupported assignment operator: {op}");
            }
        }

		public static string ToCpp(this BinaryOperatorType op)
		{
			switch (op)
			{
			case BinaryOperatorType.BitwiseAnd:
				return "&";
			case BinaryOperatorType.BitwiseOr:
				return "|";
			case BinaryOperatorType.ConditionalAnd:
				return "&&";
			case BinaryOperatorType.ConditionalOr:
				return "||";
			case BinaryOperatorType.ExclusiveOr:
				return "^";
			case BinaryOperatorType.GreaterThan:
				return ">";
			case BinaryOperatorType.GreaterThanOrEqual:
				return ">=";
			case BinaryOperatorType.Equality:
				return "==";
			case BinaryOperatorType.InEquality:
				return "!=";
			case BinaryOperatorType.LessThan:
				return "<";
			case BinaryOperatorType.LessThanOrEqual:
				return "<=";
			case BinaryOperatorType.Add:
				return "+";
			case BinaryOperatorType.Subtract:
				return "-";
			case BinaryOperatorType.Multiply:
				return "*";
			case BinaryOperatorType.Divide:
				return "/";
			case BinaryOperatorType.Modulus:
				return "%";
			case BinaryOperatorType.ShiftLeft:
				return "<<";
			case BinaryOperatorType.ShiftRight:
				return ">>";
			case BinaryOperatorType.Any:
			case BinaryOperatorType.NullCoalescing:
			default:
				return string.Format("({0})", op);
			}
		}

		public static string ToCpp(this UnaryOperatorType op)
		{
			switch (op)
			{
			case UnaryOperatorType.Not:
				return "!";
			case UnaryOperatorType.BitNot:
				return "~";
			case UnaryOperatorType.Minus:
				return "-";
			case UnaryOperatorType.Plus:
				return "+";
			case UnaryOperatorType.Increment:
				return "++";
			case UnaryOperatorType.Decrement:
				return "--";
			case UnaryOperatorType.PostIncrement:
				return "++";
			case UnaryOperatorType.PostDecrement:
				return "--";
			case UnaryOperatorType.Dereference:
			case UnaryOperatorType.AddressOf:
			case UnaryOperatorType.Any:
			case UnaryOperatorType.Await:
			default:
				return string.Format("({0})", op);
			}
		}
	}
}
