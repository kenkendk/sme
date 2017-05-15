using System;
using System.Collections.Generic;
using SME.AST;

namespace SME.AST.Transform
{
    /// <summary>
    /// Static container for performing multiple AST transformations on a network
    /// </summary>
    public static class Apply
    {
		/// <summary>
		/// Performs all transformations on a the given network
		/// </summary>
		/// <param name="network">The network to transform.</param>
		/// <param name="directapply">A sequence of visitor classes that are applied without restarting the transformation. This can be used to add metdata or control logic before the actual transformations</param>
		/// <param name="preapply">Method that returns a sequence of transformations to perform before the main transformations</param>
		/// <param name="apply">Method that returns a sequence of transformations to perform</param>
		/// <param name="preapply">Method that returns a sequence of transformations to perform after the main transformations</param>
		public static void Transform(
            this AST.Network network,
			IEnumerable<IASTTransform> directapply = null,
			Func<Method, IASTTransform[]> preapply = null,
            Func<Method, IASTTransform[]> apply = null,
            Func<Method, IASTTransform[]> postapply = null
        )
        {
            directapply = directapply ?? new IASTTransform[0];
            preapply = preapply ?? (x => new IASTTransform[0]);
			apply = apply ?? (x => new IASTTransform[0]);
			postapply = postapply ?? (x => new IASTTransform[0]);

			var methods = new List<Method>();

            foreach (var n in network.All((el, direction) =>
            {
                if (direction == VisitorState.Enter && el is Method)
                {
                    methods.Add(el as Method);
                    return false;
                }

                return true;
            }))
            {
                foreach (var f in directapply)
                    f.Transform(n);
            }

            // Pre-transforms are in Pre-Order
            foreach (var m in methods)
                RepeatedApply(preapply(m), () => m.All());

            // Main transforms are  in Post-Order
            foreach (var m in methods)
                RepeatedApply(apply(m), () => m.DepthFirstPostOrder());

            // Post transforms are in Pre-order
            foreach (var m in methods)
                RepeatedApply(postapply(m), () => m.All());
        }

        private static void RepeatedApply(IASTTransform[] transforms, Func<IEnumerable<ASTItem>> it)
        {
            var repeat = true;
#if DEBUG_TRANSFORMS
			object lastchanger = null;
			ASTItem lastchange = null;
#endif
            while (repeat)
            {
                repeat = false;
#if DEBUG_TRANSFORMS
				Console.WriteLine("**** Restart ****");
#endif

                foreach (var x in it())
                {
                    if (x.Parent == x)
                        throw new Exception("Self-parenting is a bad idea");

                    foreach (var f in transforms)
                        if (f.Transform(x) != x)
                        {
#if DEBUG_TRANSFORMS
							lastchanger = f;
							lastchange = x;

							if (x is AST.Expression)	
								Console.WriteLine(x.GetType().FullName + ": " + (x as AST.Expression).SourceExpression.ToString());
							if (x is AST.Statement)
								Console.WriteLine(x.GetType().FullName + ": " + (x as AST.Statement).SourceStatement.ToString());
						
							Console.WriteLine("........... restarting after change by {0}", f.GetType().FullName);
#endif

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
