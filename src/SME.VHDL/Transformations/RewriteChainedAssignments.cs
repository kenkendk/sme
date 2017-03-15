using System;
using System.Collections.Generic;
using System.Linq;
using SME.AST;

namespace SME.VHDL.Transformations
{
	/// <summary>
	/// Rewrites statements like &quot;a = b = c = 0;&quot; into a sequence of single assignment expressions
	/// </summary>
	public class RewriteChainedAssignments : IASTTransform
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
		/// Flag to debug assignments, and disable optimizations where the assignments are done with a literal
		/// </summary>
		public bool OptimizeChainedInitializations = true;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SME.VHDL.Transformations.RewriteChainedAssignments"/> class.
		/// </summary>
		/// <param name="state">The render state.</param>
		/// <param name="method">The method being rendered.</param>
		public RewriteChainedAssignments(RenderState state, Method method)
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
			var ase = item as AssignmentExpression;
			if (ase == null)
				return item;

			if (ase.Parent == null || ase.Left is EmptyExpression)
				return item;

			var last = ase.Right.All().LastOrDefault(x => x is AssignmentExpression) as AssignmentExpression;
			var targets = new[] { ase.Left }.Union(ase.Right.All().OfType<AssignmentExpression>().Select(x => x.Left)).ToArray();
			if (last == null)
				return item;

			Func<Expression> assignWith;
			var statements = new List<Statement>();

			// Unwrap the last statement to see if it is really a primitive
			var lastexp = last.Right;
			while(OptimizeChainedInitializations)
			{
				if (lastexp is ParenthesizedExpression)
				{
					lastexp = ((ParenthesizedExpression)lastexp).Expression;
					continue;
				}

				if (lastexp is CustomNodes.ConversionExpression)
				{
					lastexp = ((CustomNodes.ConversionExpression)lastexp).Expression;
					continue;
				}

				break;
			}

			if (OptimizeChainedInitializations && lastexp is PrimitiveExpression)
			{
				assignWith = () => last.Right.Clone();
			}
			else
			{
				var tmp = State.RegisterTemporaryVariable(Method, last.Right.SourceResultType);
				assignWith = () => new IdentifierExpression()
				{
					Name = tmp.Name,
					Target = tmp,
					SourceResultType = tmp.CecilType,
					SourceExpression = ase.SourceExpression,
				};

				var tmpstm = new ExpressionStatement()
				{
					SourceStatement = ase.SourceExpression.Clone(),
					Expression = new AssignmentExpression()
					{
						Operator = ICSharpCode.NRefactory.CSharp.AssignmentOperatorType.Assign,
						Left = assignWith(),
						Right = last.Right,
						SourceExpression = ase.SourceExpression,
						SourceResultType = tmp.CecilType
					}
				};
				statements.Add(tmpstm);
			}

			foreach (var el in targets)
			{
				var s = new ExpressionStatement()
				{
					Expression = new AssignmentExpression()
					{
						Operator = ICSharpCode.NRefactory.CSharp.AssignmentOperatorType.Assign,
						Left = el,
						Right = assignWith(),
						SourceExpression = ase.SourceExpression,
						SourceResultType = el.SourceResultType
					}					
				};
				statements.Add(s);
			}

			var blstm = new BlockStatement()
			{
				Statements = statements.ToArray(),
				SourceStatement = ase.SourceExpression.Clone()
			};

			ase.PrependStatement(blstm);
			if (ase.Parent is ExpressionStatement)
				((Statement)ase.Parent).ReplaceWith(new EmptyStatement());
			else
				ase.ReplaceWith(new EmptyExpression());
			blstm.UpdateParents();

			return null;

			/*

			// If we have another assignment somewhere, break it up 
			var sue = ase.Right.All().FirstOrDefault(x => x is AssignmentExpression) as AssignmentExpression;
			if (sue != null)
			{
				var pstm = new ExpressionStatement()
				{
					Expression = ase.Right,
					SourceStatement = ase.SourceExpression.Clone()
				};

				ase.PrependStatement(pstm);
				ase.Right.Parent = pstm;

				var nase = new AssignmentExpression()
				{
					Operator = ICSharpCode.NRefactory.CSharp.AssignmentOperatorType.Assign,
					Left = ase.Left,
					Right = sue.Left,
					SourceExpression = ase.SourceExpression,
					SourceResultType = ase.SourceResultType
				};

				ase.ReplaceWith(nase);
				nase.Left.Parent = nase.Right.Parent = nase;
			}
			*/
		}
	}
}
