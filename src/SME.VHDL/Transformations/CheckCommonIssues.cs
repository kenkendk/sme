using System;
using SME.AST;

namespace SME.VHDL.Transformations
{
	/// <summary>
	/// Checks that all items are working as expected
	/// </summary>
	public class CheckCommonIssues : IASTTransform
	{
		/// <summary>
		/// Applies the transformation
		/// </summary>
		/// <returns>The transformed item.</returns>
		/// <param name="el">The item to visit.</param>
		public ASTItem Transform(ASTItem el)
		{
			if (el is AST.DataElement)
			{
				var de = el as AST.DataElement;
				if (de.DefaultValue == null)
					throw new MissingMethodException("All elements should have a default value assigned");
			}

			return el;
		}
	}
}
