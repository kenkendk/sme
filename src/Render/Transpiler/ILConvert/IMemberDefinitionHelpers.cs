using System;
using Mono.Cecil;
using ICSharpCode.NRefactory.CSharp;

namespace SME.Render.Transpiler.ILConvert
{
	public static class IMemberDefinitionHelpers
	{
		public static bool IsVariable(this IMemberDefinition f)
		{
			return !f.DeclaringType.IsBusType() && f.GetAttribute<SignalAttribute>() == null;
		}

		public static bool IsTopLevelInput(this IMemberDefinition f)
		{
			return f.DeclaringType.IsBusType() && f.DeclaringType.GetAttribute<TopLevelInputBusAttribute>() != null;
		}

		public static bool IsTopLevelOutput(this IMemberDefinition f)
		{
			return f.DeclaringType.IsBusType() && f.DeclaringType.GetAttribute<TopLevelOutputBusAttribute>() != null;
		}

		public static string ToValidName(this IMemberDefinition f, IGlobalInformation info, TypeDefinition scope, Expression target, TypeDefinition declaringtype = null)
		{
			declaringtype = declaringtype ?? f.DeclaringType;

			if (declaringtype.IsEnum)
			{
				return info.ToValidName(declaringtype.FullName + "_" + f.Name);
			}
			else
			{
				if (declaringtype.IsBusType() || f is MethodDefinition || (f is FieldDefinition && (f as FieldDefinition).IsStatic))
				{
					var name = declaringtype.FullName + "_" + f.Name;
					var asmname = scope.Module.Assembly.Name.Name + '.';
					if (name.StartsWith(scope.FullName + "/", StringComparison.Ordinal))
						name = name.Substring(scope.FullName.Length + 1);
					else if (name.StartsWith(asmname, StringComparison.Ordinal))
						name = name.Substring(asmname.Length);
					return info.ToValidName(name);
				}
				else
				{
					return info.ToValidName(target.ToString()) + "." + info.ToValidName(f.Name);
				}
			}
		}
	}
}

