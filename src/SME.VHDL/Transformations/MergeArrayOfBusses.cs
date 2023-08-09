using System;
using System.Collections.Generic;
using System.Linq;
using SME.AST;
using SME.AST.Transform;
using Microsoft.CodeAnalysis;

namespace SME.VHDL.Transformations
{
    /// <summary>
    /// This transformation merges instance references to the same bus into a single bus reference.
    /// It also handles the indices, ensuring that they're correct.
    /// </summary>
    public class MergeArrayOfBusses : IASTTransform
    {
        /// <summary>
        /// The render state.
        /// </summary>
        private readonly RenderState state;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SME.VHDL.Transformations.MergeArrayOfBusses"/> class.
        /// </summary>
        public MergeArrayOfBusses(RenderState state)
        {
            this.state = state;
        }

        /// <summary>
        /// Applies the transformation.
        /// </summary>
        /// <returns>The transformed item.</returns>
        /// <param name="el">The item to visit.</param>
        public ASTItem Transform(ASTItem el)
        {
            // This transformation only applies to processes
            var proc = el as AST.Process;
            if (proc == null)
                return el;

            update(ref proc.InputBusses);
            update(ref proc.OutputBusses);
            update(ref proc.InternalBusses);

            return proc;
        }

        public void update(ref Dictionary<AST.Bus, int[]> busses)
        {
            // Check all of the busses
            Dictionary<AST.Bus, int[]> new_busses = new Dictionary<AST.Bus, int[]>();
            foreach (var (bus, _) in busses)
            {
                var largest = bus.SourceInstances
                    .Select(x => (state.Network as ParseProcesses.NetworkState).BusInstanceLookup[x])
                    .Distinct()
                    .OrderBy(x => x.SourceInstances.Count())
                    .LastOrDefault();
                var indices = bus.SourceInstances
                    .Select(x => Array.IndexOf(largest.SourceInstances, x))
                    .ToArray();

                new_busses.Add(largest, indices);
            }

            foreach (var (bus, indices) in new_busses)
                busses[bus] = indices;
        }
    }
}