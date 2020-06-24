using System;
using System.Linq;
using System.Collections.Generic;
using SME.AST;

namespace SME.CPP
{
    public class RenderStateProcess
    {
        /// <summary>
        /// The parent render state
        /// </summary>
        public readonly RenderState Parent;

        /// <summary>
        /// The process used in this render state
        /// </summary>
        public readonly AST.Process Process;

        /// <summary>
        /// The type scope used to resolve CPP types
        /// </summary>
        public readonly CppTypeScope TypeScope;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SME.CPP.RenderStateProcess"/> class.
        /// </summary>
        /// <param name="parent">The parent render state.</param>
        /// <param name="process">The process to render.</param>
        public RenderStateProcess(RenderState parent, AST.Process process)
        {
            Parent = parent;
            Process = process;
            TypeScope = parent.TypeScope;
        }
    }
}
