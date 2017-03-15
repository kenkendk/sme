using System;
using SME.AST;

namespace SME.VHDL.CustomNodes
{
	/// <summary>
	/// An expression that converts the inner expression
	/// </summary>
	public class ConversionExpression : CustomExpression
	{
		/// <summary>
		/// The expression being wrapped
		/// </summary>
		public Expression Expression;

		/// <summary>
		/// The template used to wrap the expression
		/// </summary>
		public string WrappingTemplate;

		/// <summary>
		/// Gets the child expression.
		/// </summary>
		public override Expression[] Children { get { return new[] { Expression }; } }
	}
}
