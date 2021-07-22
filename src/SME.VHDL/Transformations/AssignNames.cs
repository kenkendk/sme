using System;
using SME.AST;
using SME.AST.Transform;
using Microsoft.CodeAnalysis;

namespace SME.VHDL.Transformations
{
    /// <summary>
    /// This transformation assign names to each named element.
    /// </summary>
    public class AssignNames : IASTTransform
    {
        /// <summary>
        /// The name of the top-level item.
        /// </summary>
        private string TopLevelName = null;

        /// <summary>
        /// Applies the transformation
        /// </summary>
        /// <returns>The transformed item.</returns>
        /// <param name="el">The item to visit.</param>
        public ASTItem Transform(ASTItem el)
        {
            if (el.Name != null && (el is AST.Bus || el is AST.Process || el is AST.DataElement))
                el.Name = Naming.ToValidName(el.Name, is_bus_signal:el is AST.BusSignal);

            if (el is AST.Bus && ((AST.Bus)el).InstanceName != null)
                ((AST.Bus)el).InstanceName = Naming.ToValidName(((AST.Bus)el).InstanceName);
            if (el is AST.Process)
            {
                if (((AST.Process)el).InstanceName == null)
                    ((AST.Process)el).InstanceName = Naming.ToValidName(((AST.Process)el).InstanceName);

                if (TopLevelName == null)
                    TopLevelName = Naming.AssemblyToValidName();

                if (string.Equals(((AST.Process)el).InstanceName, TopLevelName, StringComparison.OrdinalIgnoreCase))
                    ((AST.Process)el).InstanceName = "cls_" + ((AST.Process)el).InstanceName;
            }

            if (el is AST.Constant)
            {
                if (((Constant)el).Source is IFieldSymbol && ((Constant)el).Parent is Network)
                    el.Name = Naming.ToValidName(el.Name);
                else if (el.Parent != null && !string.IsNullOrWhiteSpace(el.Parent.Name))
                    el.Name = Naming.ToValidName(el.Parent.Name + "." + el.Name);
            }

            return el;
        }
    }
}
