using System;
using ICSharpCode.NRefactory.CSharp;

namespace SME.Render.VHDL.ILConvert.AugmentedExpression
{
	public class VHDLParenthesizedExpression : VHDLTypedExpression<ParenthesizedExpression>
	{
		public VHDLParenthesizedExpression(Converter converter, ParenthesizedExpression expression)
			: base(converter, expression)
		{
		}


		private IVHDLExpression m_target;

		public IVHDLExpression Target
		{
			get
			{
				if (m_target == null)
					m_target = Converter.ResolveExpression(Expression.Expression);
				return m_target;
			}
		}

		public override Mono.Cecil.TypeReference ResolvedSourceType
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
			//return string.Format("({0})", Target.ResolvedString);
			//return Converter.WrapIfComposite(Target).ResolvedString;
			return Target.ResolvedString;
		}
	}
}

