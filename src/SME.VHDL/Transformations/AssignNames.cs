using System;
using System.Linq;
using SME.AST;

namespace SME.VHDL.Transformations
{
	/// <summary>
	/// This transformation assign names to each named element
	/// </summary>
	public class AssignNames : IASTTransform
	{
		/// <summary>
		/// Applies the transformation
		/// </summary>
		/// <returns>The transformed item.</returns>
		/// <param name="el">The item to visit.</param>
		public ASTItem Transform(ASTItem el)
		{
			if (el.Name != null && (el is AST.Bus || el is AST.Process || el is AST.DataElement))
				el.Name = Naming.ToValidName(el.Name);

			if (el is AST.Process)
			{
				var customname = ((AST.Process)el).SourceType.GetCustomAttributes(typeof(VHDLComponentAttribute), false).FirstOrDefault() as VHDLComponentAttribute;
				if (customname != null)
					el.Name = customname.Name;
			}

			if (el is AST.Constant)
			{
				if (((Constant)el).Source is Mono.Cecil.FieldDefinition)
					el.Name = Naming.ToValidName((((Constant)el).Source as Mono.Cecil.FieldDefinition).DeclaringType.FullName + "." + el.Name);
				else
					el.Name = Naming.ToValidName(el.Parent.Name + "." + el.Name);
			}

			return el;
		}
	}
}
