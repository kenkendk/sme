using System;
using SME.AST;

namespace SME.VHDL.Transformations
{
	/// <summary>
	/// If the output does not support VHDL 2008,
	/// this will transform conditionals into an
	/// <seealso cref="IfElseStatement"/>.
	/// </summary>
	public class RemoveConditionals : IASTTransform
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
		/// Rewrites conditions based on booleans without adding a temporary variable
		/// </summary>
		public bool CompressLogicalAssignments = true;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SME.VHDL.Transformations.RemoveConditionals"/> class.
		/// </summary>
		/// <param name="state">The render state.</param>
		/// <param name="method">The method being rendered.</param>
		public RemoveConditionals(RenderState state, Method method)
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
			if (State.SUPPORTS_VHDL_2008)
				return el;

			if (el is ConditionalExpression)
			{
				var ce = el as ConditionalExpression;

				if (CompressLogicalAssignments)
				{
					var pe = ce.Parent;
					if (pe is ParenthesizedExpression)
						pe = pe.Parent;

					// See if we can avoid introducing a temporary variable
					// when the conditional target is a boolean assignment
					if (pe is AssignmentExpression)
					{
						var ase = pe as AssignmentExpression;
						var tvhdl = State.VHDLType(ase.Left);
						var svhdl = State.VHDLType(ce);

						if (tvhdl == VHDLTypes.SYSTEM_BOOL && svhdl == VHDLTypes.SYSTEM_BOOL && ase.Operator == ICSharpCode.NRefactory.CSharp.AssignmentOperatorType.Assign)
						{
							var tg = ase.Left.GetTarget();
							CreateIfElse(ce, tg);

							ase.ReplaceWith(new EmptyExpression()
							{
								Parent = ase.Parent,
								SourceExpression = ase.SourceExpression,
								SourceResultType = null
							});

							return null;
						}
					}
				}

				var tmp = State.RegisterTemporaryVariable(Method, ce.SourceResultType);
				State.TypeLookup[tmp] = State.VHDLType(ce);

				CreateIfElse(ce, tmp);

				var iex = new IdentifierExpression()
				{
					Name = tmp.Name,
					SourceExpression = ce.SourceExpression,
					SourceResultType = ce.SourceResultType,
					Target = tmp,
				};

				State.TypeLookup[iex] = State.VHDLType(tmp);
				ce.ReplaceWith(iex);

				return iex;
			}

			return el;
		}

		private IfElseStatement CreateIfElse(ConditionalExpression sourceExp, DataElement target)
		{
			Expression targetExp;
			if (target is BusSignal)
			{
				targetExp = new MemberReferenceExpression()
				{
					SourceExpression = sourceExp.SourceExpression,
					SourceResultType = target.CecilType,
					Target = target
				};
			}
			else
			{
				targetExp = new IdentifierExpression()
				{
					SourceExpression = sourceExp.SourceExpression,
					SourceResultType = target.CecilType,
					Target = target					
				};
			}

			var ies = new IfElseStatement()
			{
				Condition = sourceExp.ConditionExpression,
				TrueStatement = new ExpressionStatement()
				{
					SourceStatement = sourceExp.SourceExpression.Clone(),
					Expression = new AssignmentExpression()
					{
						SourceResultType = targetExp.SourceResultType,
						SourceExpression = sourceExp.SourceExpression,
						Left = targetExp,
						Right = sourceExp.TrueExpression
					}
				},
				FalseStatement = new ExpressionStatement()
				{
					SourceStatement = sourceExp.SourceExpression.Clone(),
					Expression = new AssignmentExpression()
					{
						SourceResultType = targetExp.SourceResultType,
						SourceExpression = sourceExp.SourceExpression,
						Left = targetExp,
						Right = sourceExp.FalseExpression
					}
				},
				SourceStatement = sourceExp.SourceExpression.Clone()
			};
						   
			sourceExp.PrependStatement(ies);
			ies.UpdateParents();

			return ies;
		}
	}
}
