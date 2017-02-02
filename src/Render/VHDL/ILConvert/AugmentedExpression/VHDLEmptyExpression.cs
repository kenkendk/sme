using System;
using Mono.Cecil;
using ICSharpCode.NRefactory.CSharp;
using SME.Render.Transpiler.ILConvert;

namespace SME.Render.VHDL.ILConvert.AugmentedExpression
{
	public class VHDLEmptyExpression : IVHDLExpression
	{
		public VHDLEmptyExpression(ILConvert.VHDLConverter converter, Expression expression)
		{
			this.Converter = converter;
			this.Expression = expression;
			if (this.Expression is NullReferenceExpression)
			{
				this.ResolvedSourceType = converter.ImportType<UIntPtr>();
				this.ResolvedString = "NIL";
			}
			else
			{
				this.ResolvedString = "";
			}
		}

		#region IVHDLExpression implementation

		public ICSharpCode.NRefactory.CSharp.Expression Expression { get; private set; }
		public VHDLConverter Converter { get; private set; }
		public IAugmentedExpression WrappedExpression { get; private set; }
		public VHDLTypeDescriptor VHDLType { get; private set; }
		public TypeReference ResolvedSourceType { get; private set; }
		public string ResolvedString { get; private set; }

		#endregion
	}
}

