using System;
using System.Linq;
using SME;
using SME.AST;

namespace SME.VHDL.Transformations
{
	public class RemoveTrailingBreakStatement : IASTTransform
	{
		/// <summary>
		/// Applies the transformation
		/// </summary>
		/// <returns>The transformed item.</returns>
		/// <param name="item">The item to visit.</param>
		public ASTItem Transform(ASTItem item)
		{
			var bs = item as AST.BreakStatement;
			if (bs == null)
				return item;

			if (bs.Parent is SwitchStatement)
			{
				var ss = bs.Parent as SwitchStatement;

				foreach (var cs in ss.Cases)
					if (cs.Item2[cs.Item2.Length - 1] == bs)
					{
						var es = new EmptyStatement()
						{ 
							SourceStatement = bs.SourceStatement 
						};

						bs.ReplaceWith(es);

						return es;
					}
			}

			return bs;
		}
	}
}
