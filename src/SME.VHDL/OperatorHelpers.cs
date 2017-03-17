using System;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;
using SME.AST;

namespace SME.VHDL
{
	public static class OperatorHelpers
	{
		public static string ToVHDL(this AssignmentOperatorType op)
		{
			switch (op)
			{
			case AssignmentOperatorType.Assign:
				return "<=";
			case AssignmentOperatorType.Add:
				return "+";
			case AssignmentOperatorType.Subtract:
				return "-";
			case AssignmentOperatorType.Multiply:
				return "*";
			case AssignmentOperatorType.Divide:
				return "/";
			case AssignmentOperatorType.Modulus:
				return "mod";
			case AssignmentOperatorType.ShiftLeft:
				return "ssl";
			case AssignmentOperatorType.ShiftRight:
				return "srl";
			case AssignmentOperatorType.BitwiseAnd:
				return "and";
			case AssignmentOperatorType.BitwiseOr:
				return "or";
			case AssignmentOperatorType.ExclusiveOr:
				return "xor";
			case AssignmentOperatorType.Any:
			default:
				return string.Format("({0})", op.ToString());
			}
		}

		public static string ToVHDL(this OperatorType op)
		{
			switch (op)
			{
			case OperatorType.LogicalNot:
				return "not";
			case OperatorType.Addition:
				return "+";
			case OperatorType.Subtraction:
				return "-";
			case OperatorType.UnaryPlus:
				return "+";
			case OperatorType.UnaryNegation:
				return "-";
			case OperatorType.Multiply:
				return "*";
			case OperatorType.Division:
				return "/";
			case OperatorType.Modulus:
				return "%";
			case OperatorType.BitwiseAnd:
				return "and";
			case OperatorType.BitwiseOr:
				return "or";
			case OperatorType.ExclusiveOr:
				return "xor";
			case OperatorType.LeftShift:
				return "sll";
			case OperatorType.RightShift:
				return "slr";
			case OperatorType.Equality:
				return "=";
			case OperatorType.Inequality:
				return "/=";
			case OperatorType.GreaterThan:
				return ">";
			case OperatorType.LessThan:
				return "<";
			case OperatorType.GreaterThanOrEqual:
				return ">=";
			case OperatorType.LessThanOrEqual:
				return "<=";
			case OperatorType.OnesComplement:
			case OperatorType.Increment:
			case OperatorType.Decrement:
			case OperatorType.True:
			case OperatorType.False:
			case OperatorType.Implicit:
			case OperatorType.Explicit:
			default:
				return string.Format("({0}}", op.ToString());
			}
		}

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

		/// <summary>
		/// Gets a string indicating if the method parameter is an in or out parameter
		/// </summary>
		/// <returns>The parameter direction string.</returns>
		/// <param name="n">The parameter to examine.</param>
		public static string GetVHDLInOut(this ParameterDefinition n)
		{
			var inarg = (n.Attributes & ParameterAttributes.Out) != ParameterAttributes.Out;
			var outarg = (n.Attributes & ParameterAttributes.Out) == ParameterAttributes.Out && (n.Attributes & ParameterAttributes.In) != ParameterAttributes.In;
			var inoutarg = (n.Attributes & ParameterAttributes.In) == ParameterAttributes.In && (n.Attributes & ParameterAttributes.Out) == ParameterAttributes.Out;
			var inoutoverride = (n.Attributes & ParameterAttributes.Out) == ParameterAttributes.Out || (n.Attributes & ParameterAttributes.In) == ParameterAttributes.In;
			var isarray = n.ParameterType.IsArray;
			var argrange = n.GetAttribute<RangeAttribute>();
			return inoutarg || (isarray && !inoutoverride) ? "inout" : (inarg ? "in" : "out");
		}
	}
}
