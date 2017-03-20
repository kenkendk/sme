using System;
using System.Collections.Generic;
using SME.AST;

namespace SME.VHDL.Transformations
{
	/// <summary>
	/// Static container for performing all AST transformations in a single place
	/// </summary>
	public static class BuildTransformations
	{
		/// <summary>
		/// Performs all transformations on a the given network
		/// </summary>
		/// <param name="network">The network to transform.</param>
		/// <param name="state">The render state to use for the transform.</param>
		public static void Transform(this AST.Network network, RenderState state)
		{
			var funcs = new IASTTransform[] {
				new AssignNames(),
			};

			var methods = new List<Method>();

			foreach (var n in network.All((el, direction) => {
				if (direction == VisitorState.Enter && el is Method)
				{
					methods.Add(el as Method);
					return false;
				}

				return true;
			} ))
			{
				foreach (var f in funcs)
					f.Transform(n);
			}

			// Pre-transforms are in Pre-Order
			foreach (var m in methods)
				RepeatedApply(new IASTTransform[] {
					new RewriteChainedAssignments(state, m),
				}, () => m.All());

			// Main transforms are  in Post-Order
			foreach(var m in methods)
				RepeatedApply(new IASTTransform[] {
					new RemoveUIntPtrCast(),
					new RemoveDoubleCast(),
					new WrapIfComposite(),
					new RemoveSelfAssignments(),
					new AssignVhdlType(state),
					new RemoveConditionals(state, m),
					new InjectTypeConversions(state, m),
					new RewireCompositeAssignment(),
					new FixForLoopIncrements(state, m),
					new RewireUnaryOperators(state),
				}, () => m.DepthFirstPostOrder());


			// Post transforms are in Pre-order
			foreach (var m in methods)
				RepeatedApply(new IASTTransform[] {
					//new RemoveExtraParenthesis(),
				}, () => m.All());
		}

		private static void RepeatedApply(IASTTransform[] transforms, Func<IEnumerable<ASTItem>> it)
		{
			var repeat = true;
			while (repeat)
			{
				repeat = false;
				foreach (var x in it())
				{
					foreach (var f in transforms)
						if (f.Transform(x) != x)
						{
							repeat = true;
							break;
						}

					if (repeat)
						break;
				}
			}		
		}
	}
}
