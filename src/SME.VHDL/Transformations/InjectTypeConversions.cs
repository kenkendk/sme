using System;
using System.Collections.Generic;
using ICSharpCode.NRefactory.CSharp;
using SME.AST;

namespace SME.VHDL.Transformations
{
	/// <summary>
	/// Injects VHDL type conversions to ensure the output
	/// is valid VHDL
	/// </summary>
	public class InjectTypeConversions : IASTTransform
	{
		/// <summary>
		/// The render state
		/// </summary>
		private readonly RenderState State;
		/// <summary>
		/// The method being compiled
		/// </summary>
		private readonly Method Method;

		/// <summary>
		/// Lookup table with expressions already processed
		/// </summary>
		private readonly Dictionary<ASTItem, string> m_done = new Dictionary<ASTItem, string>();

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SME.VHDL.Transformations.InjectTypeConversions"/> class.
		/// </summary>
		/// <param name="state">The render state.</param>
		/// <param name="method">The method being rendered.</param>
		public InjectTypeConversions(RenderState state, Method method)
		{
			State = state;
			Method = method;
		}

		/// <summary>
		/// Applies the transformation
		/// </summary>
		/// <returns>The transformed item.</returns>
		/// <param name="el">The item to visit.</param>
		public ASTItem Transform(ASTItem el)
		{
			if (m_done.ContainsKey(el))
				return el;
			
			var res = el;
			if (el is AST.BinaryOperatorExpression)
			{
				var boe = ((AST.BinaryOperatorExpression)el);
				var tvhdl = State.VHDLType(boe);

				var le = boe.Left;
				var re = boe.Right;

				var le_type = State.VHDLType(le);
				var re_type = State.VHDLType(re);

				if (le is AST.PrimitiveExpression && !(re is AST.PrimitiveExpression) && boe.Operator != BinaryOperatorType.ShiftLeft && boe.Operator != BinaryOperatorType.ShiftRight)
					tvhdl = re_type;
				else
					tvhdl = le_type;

				// If we have a constant, see if that is a larger type
				if (le is AST.PrimitiveExpression)
					le_type = State.TypeScope.GetVHDLType(((AST.PrimitiveExpression)le).Value.GetType());
				if (re is AST.PrimitiveExpression && boe.Operator != BinaryOperatorType.ShiftLeft && boe.Operator != BinaryOperatorType.ShiftRight)
					re_type = State.TypeScope.GetVHDLType(((AST.PrimitiveExpression)re).Value.GetType());

				// Pick the largest side, if any
				if (le_type.Length > tvhdl.Length)
					tvhdl = le_type;
				else if (re_type.Length > tvhdl.Length)
					tvhdl = re_type;


				if ((boe.Operator.IsArithmeticOperator() || boe.Operator.IsCompareOperator()))
				{
					if (tvhdl != VHDLTypes.INTEGER)
						tvhdl = State.TypeScope.NumericEquivalent(tvhdl, false) ?? tvhdl;
				}
				else if (boe.Operator == BinaryOperatorType.ShiftLeft || boe.Operator == BinaryOperatorType.ShiftRight)
					tvhdl = State.TypeScope.NumericEquivalent(tvhdl);
				else if (boe.Operator.IsBitwiseOperator())
					tvhdl = State.TypeScope.SystemEquivalent(tvhdl);


				var lhstype = boe.Operator.IsLogicalOperator() ? VHDLTypes.BOOL : tvhdl;
				var rhstype = boe.Operator.IsLogicalOperator() ? VHDLTypes.BOOL : tvhdl;

				// Overrides for special types
				switch (boe.Operator)
				{
				case BinaryOperatorType.ShiftLeft:
				case BinaryOperatorType.ShiftRight:
					rhstype = VHDLTypes.INTEGER;
					break;
				}

				var newleft = VHDLTypeConversion.ConvertExpression(State, Method, boe.Left, lhstype, false);
				var newright = VHDLTypeConversion.ConvertExpression(State, Method, boe.Right, rhstype, false);

				if (boe.Operator.IsLogicalOperator() || boe.Operator.IsCompareOperator())
					State.TypeLookup[boe] = VHDLTypes.BOOL;
				else
					State.TypeLookup[boe] = tvhdl;

				newleft.Parent = boe;
				newright.Parent = boe;

				if (boe.Operator == BinaryOperatorType.Multiply && tvhdl != VHDLTypes.INTEGER)
				{
					VHDLTypeConversion.WrapExpression(State, boe, string.Format("resize({0}, {1})", "{0}", tvhdl.Length), tvhdl);
					m_done[boe] = string.Empty;
					return null;
				}

				if (newleft != le || newright != re)
					return null;
			}
			else if (el is AST.AssignmentExpression)
			{
				var ase = ((AST.AssignmentExpression)el);
				var tvhdl = State.VHDLType(ase.Left);

				var r = ase.Right;
				ase.SourceResultType = ase.Left.SourceResultType;
				var n = VHDLTypeConversion.ConvertExpression(State, Method, ase.Right, tvhdl, false);
				State.TypeLookup[ase] = State.TypeLookup[n] = tvhdl;
				if (n != r)
					return null;
			}
			else if (el is AST.CastExpression)
			{
				var cse = ((AST.CastExpression)el);
				var tvhdl = State.VHDLType(cse);
				res = cse.ReplaceWith(
					VHDLTypeConversion.ConvertExpression(State, Method, cse.Expression, tvhdl, true)
				);
				State.TypeLookup[cse] = State.TypeLookup[res] = tvhdl;
			}
			else if (el is AST.IndexerExpression)
			{
				var ie = ((AST.IndexerExpression)el);
				var tvhdl = VHDLTypes.INTEGER;
				var r = ie.IndexExpression;
				var n = VHDLTypeConversion.ConvertExpression(State, Method, ie.IndexExpression, tvhdl, false);
				State.TypeLookup[n] = tvhdl;
				State.TypeLookup[ie] = State.TypeScope.GetByName(State.VHDLType(ie.Target).ElementName);
				if (n != r)
					return null;
			}
			else if (el is AST.IfElseStatement)
			{
				var ies = el as AST.IfElseStatement;
				var tvhdl = VHDLTypes.BOOL;
				var r = ies.Condition;
				var n = VHDLTypeConversion.ConvertExpression(State, Method, ies.Condition, tvhdl, false);
				State.TypeLookup[n] = tvhdl;
				if (n != r)
					return null;
			}

			return res;
		}
	}
}
