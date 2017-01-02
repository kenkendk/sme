using System;
using ICSharpCode.NRefactory.CSharp;
using System.Linq;
using Mono.Cecil;

namespace SME.Render.VHDL.ILConvert.AugmentedExpression
{
	public class VHDLInvocationExpression : VHDLTypedExpression<InvocationExpression>
	{
		public VHDLInvocationExpression(Converter converter, InvocationExpression expression)
			: base(converter, expression)
		{
		}

		private IVHDLExpression m_target;

		public IVHDLExpression Target
		{
			get
			{
				if (m_target == null)
					m_target = Converter.ResolveExpression(Expression.Target);
				return m_target;
			}
		}

		public override TypeReference ResolvedSourceType
		{
			get
			{
				return Target.ResolvedSourceType;
			}
		}

		public override VHDLTypeDescriptor VHDLType
		{
			get
			{
				return Target.VHDLType;
			}
		}

		protected override string ResolveToString()
		{
			var member = ResolveMemberReference(Expression.Target);
			if (member.Item.DeclaringType.IsSameTypeReference(Converter.ImportType<Process>()))
			{
				if (member.Name == "PrintDebug")
					return string.Format("-- print({0})", string.Join(", ", Expression.Arguments.Select(x => x.ToString())).Replace(Environment.NewLine, ""));
				else
					return "-- " + Expression.ToString();
			}

			var method = Converter.ResolveExpression(Expression.Target);
			var args = Expression.Arguments.Zip((member.Item as MethodDefinition).Parameters, (a,b) => new { Parameter = b, Value = a }).Select(n => Converter.WrapConverted(Converter.ResolveExpression(n.Value), Converter.Information.VHDLTypes.GetVHDLType(n.Parameter)).ResolvedString).ToArray();

			if (member.GetAttribute<VHDLCompileAttribute>() != null)
			{
				var mdef = member.Item as MethodDefinition;
				Converter.RegisterMethodForCompilation(member.Item as MethodDefinition);
			}


			return string.Format("{0}({1})", method.ResolvedString, string.Join(", ", args));		
		}
	}
}

