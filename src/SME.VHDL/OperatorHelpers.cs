using System;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;
using SME.AST;

namespace SME.VHDL
{
	public static class OperatorHelpers
	{
		public static string ToVHDL(this BinaryOperatorType op)
		{
			switch (op)
			{
			case BinaryOperatorType.BitwiseAnd:
				return "and";
			case BinaryOperatorType.BitwiseOr:
				return "or";
			case BinaryOperatorType.ConditionalAnd:
				return "and";
			case BinaryOperatorType.ConditionalOr:
				return "or";
			case BinaryOperatorType.ExclusiveOr:
				return "xor";
			case BinaryOperatorType.GreaterThan:
				return ">";
			case BinaryOperatorType.GreaterThanOrEqual:
				return ">=";
			case BinaryOperatorType.Equality:
				return "=";
			case BinaryOperatorType.InEquality:
				return "/=";
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
				return "mod";
			case BinaryOperatorType.ShiftLeft:
				return "sll";
			case BinaryOperatorType.ShiftRight:
				return "srl";
			case BinaryOperatorType.Any:
			case BinaryOperatorType.NullCoalescing:
			default:
				return string.Format("({0})", op);
			}
		}

		public static string ToVHDL(this UnaryOperatorType op)
		{
			switch (op)
			{
			case UnaryOperatorType.Not:
				return "not";
			case UnaryOperatorType.BitNot:
				return "not";
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
