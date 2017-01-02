using System;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;

namespace SME.Render.VHDL.ILConvert.AugmentedExpression
{
	public interface IVHDLExpression
	{
		Expression Expression { get; }
		Converter Converter { get; }
		IVHDLExpression WrappedExpression { get; }
		VHDLTypeDescriptor VHDLType { get; }
		TypeReference ResolvedSourceType { get; }
		string ResolvedString { get; }
	}
}

