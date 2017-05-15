using System;
using System.Linq;
using SME;
using SME.AST;

namespace SME.AST.Transform
{
    /// <summary>
    /// Removes break statements that are the last line of a switch case
    /// </summary>
	public class RemoveTrailingBreakStatement : IASTTransform
	{
		/// <summary>
		/// Applies the transformation
		/// </summary>
		/// <returns>The transformed item.</returns>
		/// <param name="item">The item to visit.</param>
		public virtual ASTItem Transform(ASTItem item)
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
