using System;
using Mono.Cecil;
using ICSharpCode.NRefactory.CSharp;

namespace SME.Render.VHDL.ILConvert
{
	public static class IMemberDefinitionHelpers
	{
		public static bool IsVariable(this IMemberDefinition f)
		{
			return !f.DeclaringType.IsBusType() && f.GetAttribute<VHDLSignalAttribute>() == null;
		}

		public static bool IsTopLevelInput(this IMemberDefinition f)
		{
			return f.DeclaringType.IsBusType() && f.DeclaringType.GetAttribute<TopLevelInputBusAttribute>() != null;
		}

		public static bool IsTopLevelOutput(this IMemberDefinition f)
		{
			return f.DeclaringType.IsBusType() && f.DeclaringType.GetAttribute<TopLevelOutputBusAttribute>() != null;
		}

		public static string ToVHDLName(this IMemberDefinition f, TypeDefinition scope, Expression target, TypeDefinition declaringtype = null)
		{
			declaringtype = declaringtype ?? f.DeclaringType;

			if (declaringtype.IsEnum)
			{
				return Renderer.ConvertToValidVHDLName(declaringtype.FullName + "_" + f.Name);
			}
			else
			{
				if (declaringtype.IsBusType() || f is MethodDefinition || (f is FieldDefinition && (f as FieldDefinition).IsStatic))
				{
					var name = declaringtype.FullName + "_" + f.Name;
					var asmname = scope.Module.Assembly.Name.Name + '.';
					if (name.StartsWith(scope.FullName + "/"))
						name = name.Substring(scope.FullName.Length + 1);
					else if (name.StartsWith(asmname))
						name = name.Substring(asmname.Length);
					return Renderer.ConvertToValidVHDLName(name);
				}
				else
				{
					return Renderer.ConvertToValidVHDLName(target.ToString()) + "." + Renderer.ConvertToValidVHDLName(f.Name);
				}
			}
		}
	}
}

