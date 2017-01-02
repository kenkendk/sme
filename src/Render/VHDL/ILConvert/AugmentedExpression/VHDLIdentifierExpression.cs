using System;
using System.Linq;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;

namespace SME.Render.VHDL.ILConvert.AugmentedExpression
{
	public class VHDLIdentifierExpression : VHDLTypedExpression<IdentifierExpression>
	{
		public VHDLIdentifierExpression(Converter converter, IdentifierExpression expression)
			: base(converter, expression)
		{
		}

		private Tuple<MemberItem, VHDLTypeDescriptor, string> m_resolvedItem;
		public MemberItem ResolvedItem
		{
			get
			{
				if (m_resolvedItem == null)
					m_resolvedItem = Converter.ResolveLocalOrClassIdentifier(Expression.Identifier, Expression);

				return m_resolvedItem.Item1;
			}
		}

		public override TypeReference ResolvedSourceType
		{
			get
			{
				if (m_resolvedItem == null)
					m_resolvedItem = Converter.ResolveLocalOrClassIdentifier(Expression.Identifier, Expression);
				
				return m_resolvedItem.Item1.ItemType;
			}
		}

		public override VHDLTypeDescriptor VHDLType
		{
			get
			{
				if (m_resolvedItem == null)
					m_resolvedItem = Converter.ResolveLocalOrClassIdentifier(Expression.Identifier, Expression);

				return m_resolvedItem.Item2;
			}
		}

		public string Identifier
		{
			get
			{
				return Expression.Identifier;
			}
		}
			
		protected override string ResolveToString()
		{
			if (ResolvedItem != null)
				return Renderer.ConvertToValidVHDLName(m_resolvedItem.Item3);
			else
				throw new Exception(string.Format("Unknown identifier: {0}", Expression));
		}
	}
}

