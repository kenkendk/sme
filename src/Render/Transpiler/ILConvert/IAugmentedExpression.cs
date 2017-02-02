using System;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;

namespace SME.Render.Transpiler.ILConvert
{
	public interface IAugmentedExpression
	{
		Expression Expression { get; }
		IAugmentedExpression WrappedExpression { get; }
		TypeReference ResolvedSourceType { get; }
		string ResolvedString { get; }
	}
}
