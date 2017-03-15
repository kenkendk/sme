using System;
using SME.AST;

namespace SME.VHDL.Transformations
{
	/// <summary>
	/// This transformation ensures that all expressions and data elements have a default VHDL type associated
	/// </summary>
	public class AssignVhdlType : IASTTransform
	{
		/// <summary>
		/// The render state
		/// </summary>
		private readonly RenderState State;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SME.VHDL.Transformations.AssignVhdlType"/> class.
		/// </summary>
		/// <param name="state">The render state.</param>
		public AssignVhdlType(RenderState state)
		{
			State = state;
		}

		/// <summary>
		/// Applies the transformation
		/// </summary>
		/// <returns>The transformed item.</returns>
		/// <param name="el">The item to visit.</param>
		public ASTItem Transform(ASTItem el)
		{
			if (el is AST.DataElement)
			{
				// Force loop variable as integers
				if (el.Parent is ForStatement)
				{
					var fs = el.Parent as ForStatement;
					if (fs.LoopIndex == el)
					{
						State.TypeLookup[el] = VHDLTypes.INTEGER;
						return el;
					}
				}

				State.VHDLType(el as AST.DataElement);
			}

			if (el is AST.EmptyExpression) 
				return el;

			if (el is AST.Expression)
				State.VHDLType(el as AST.Expression);


			return el;
		}
	}
}
