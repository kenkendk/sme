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
		/// Gets or sets the child expression.
		/// </summary>
		public override Expression[] Children 
		{ 
			get { return new[] { Expression }; }
			set
			{
				if (value == null)
					Expression = null;
				else if (value.Length == 1)
					Expression = value[0];
				else
					throw new Exception("Conversion can only have a single child");
			}
		}
	}
}
