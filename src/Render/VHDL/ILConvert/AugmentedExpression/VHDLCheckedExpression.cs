using System;
using ICSharpCode.NRefactory.CSharp;

namespace SME.Render.VHDL.ILConvert.AugmentedExpression
{
	public class VHDLCheckedExpression : VHDLTypedExpression<CheckedExpression>
	{
		public VHDLCheckedExpression(Converter converter, CheckedExpression expression)
			: base(converter, expression)
		{
			Console.WriteLine("Warning: \"checked\" is not supported and will be ignored for expression: {0}", expression);
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
			return Target.ResolvedString;
		}
	}
}
