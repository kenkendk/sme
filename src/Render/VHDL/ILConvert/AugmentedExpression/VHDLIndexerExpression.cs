using System;
using ICSharpCode.NRefactory.CSharp;
using System.Linq;
using Mono.Cecil;

namespace SME.Render.VHDL.ILConvert.AugmentedExpression
{
	public class VHDLIndexerExpression : VHDLTypedExpression<IndexerExpression>
	{
		public VHDLIndexerExpression(Converter converter, IndexerExpression expression)
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
				return Target.ResolvedSourceType.GetArrayElementType();
			}
		}

		public override VHDLTypeDescriptor VHDLType
		{
			get
			{
				return Converter.Information.VHDLTypes.GetByName(Target.VHDLType.ElementName);
			}
		}

		protected override string ResolveToString()
		{
			if (Expression.Arguments.Count != 1)
				throw new Exception(string.Format("Unable to express multidimensional indexer for {0}", Expression));

			// Optimization: If the inner type is numeric, skip the cast to 32bit int which is required for
			// CIL indexing, but not VHDL
			var ixp = Converter.ResolveExpression(Expression.Arguments.First());
			if (ixp is VHDLCastExpression && (ixp.VHDLType.IsSigned || ixp.VHDLType.IsUnsigned))
				ixp = Converter.ResolveExpression(VHDLCastExpression.RemoveUIntPtrCast(Converter, (ixp as VHDLCastExpression).Expression.Expression));

			var iexp = Converter.WrapConverted(ixp, VHDLTypes.INTEGER);
			return string.Format("{0}({1})", Target.ResolvedString, iexp.ResolvedString);
		}
	}
}

