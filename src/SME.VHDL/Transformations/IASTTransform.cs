using System;
namespace SME.VHDL.Transformations
{
	/// <summary>
	/// Interface for performing a transformation on the AST
	/// </summary>
	public interface IASTTransform
	{
		/// <summary>
		/// Applies the transformation
		/// </summary>
		/// <returns>The transformed item, if this is not the input item, the transformation will sequence will restart.</returns>
		/// <param name="item">The item to visit.</param>
		AST.ASTItem Transform(AST.ASTItem item);
	}
}
