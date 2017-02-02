﻿using System;
using ICSharpCode.NRefactory.CSharp;

namespace SME.Render.VHDL.ILConvert.AugmentedExpression
{
	public class VHDLUncheckedExpression: VHDLTypedExpression<UncheckedExpression>
	{
		public VHDLUncheckedExpression(VHDLConverter converter, UncheckedExpression expression)
			: base(converter, expression)
		{
			// Unchecked is what VHDL does, so no need to handle it
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