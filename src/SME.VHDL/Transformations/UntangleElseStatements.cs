using System;
using SME.AST;
using SME.AST.Transform;

namespace SME.VHDL.Transformations
{
    public class UntangleElseStatements : IASTTransform
    {
		/// <summary>
		/// The render state.
		/// </summary>
		private readonly RenderState State;
		/// <summary>
		/// The method being transformed
		/// </summary>
		private readonly Method Method;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SME.VHDL.Transformations.UntangleElseStatements"/> class.
		/// </summary>
		/// <param name="state">The render state.</param>
		/// <param name="method">The method being rendered.</param>
		public UntangleElseStatements(RenderState state, Method method)
		{
			State = state;
			Method = method;
		}

        /// <summary>
        /// Applies the transformation
        /// </summary>
        /// <returns>The transformed item.</returns>
        /// <param name="item">The item to visit.</param>
        public ASTItem Transform(ASTItem item)
        {
            if (item is ReturnStatement && Method.Parent is AST.Process && ((AST.Process)Method.Parent).MainMethod == Method)
            {
                Console.WriteLine();
            }

            return item;
        }
	}
}
