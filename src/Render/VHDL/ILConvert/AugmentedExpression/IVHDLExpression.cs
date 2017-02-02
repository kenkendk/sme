using System;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;
using SME.Render.Transpiler.ILConvert;

namespace SME.Render.VHDL.ILConvert.AugmentedExpression
{
	public interface IVHDLExpression : IAugmentedExpression
	{
		VHDLTypeDescriptor VHDLType { get; }
		VHDLConverter Converter { get; }
	}
}

