using System;
using System.Linq;
using SME.AST;
using SME.AST.Transform;

namespace SME.VHDL.Transformations
{
	/// <summary>
	/// This transformation assign names to each named element
	/// </summary>
	public class AssignNames : IASTTransform
	{
        /// <summary>
        /// The name of the top-level item
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
				el.Name = Naming.ToValidName(el.Name);

            if (el is AST.Bus && ((AST.Bus)el).InstanceName != null)
                ((AST.Bus)el).InstanceName = Naming.ToValidName(((AST.Bus)el).InstanceName);
            if (el is AST.Process)
            {
                if (((AST.Process)el).InstanceName == null)
                    ((AST.Process)el).InstanceName = Naming.ToValidName(((AST.Process)el).InstanceName);

                if (TopLevelName == null)
                    TopLevelName = Naming.AssemblyToValidName(new[] { ((AST.Process)el).SourceInstance.Instance });

                if (string.Equals(((AST.Process)el).InstanceName, TopLevelName, StringComparison.OrdinalIgnoreCase))
                    ((AST.Process)el).InstanceName = "cls_" + ((AST.Process)el).InstanceName;
            }

			if (el is AST.Constant)
			{
				if (((Constant)el).Source is Mono.Cecil.FieldDefinition)
					el.Name = Naming.ToValidName((((Constant)el).Source as Mono.Cecil.FieldDefinition).DeclaringType.FullName + "." + el.Name);
				else if (el.Parent != null && !string.IsNullOrWhiteSpace(el.Parent.Name))
					el.Name = Naming.ToValidName(el.Parent.Name + "." + el.Name);
			}

			return el;
		}
	}
}
