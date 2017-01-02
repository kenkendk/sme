using System;

namespace SME.Render.VHDL.ILConvert.AugmentedExpression
{
	public class VHDLConvertedExpression : IVHDLExpression
	{
		private IVHDLExpression m_parent;
		private VHDLTypeDescriptor m_targettype;
		private string m_template;

		public VHDLConvertedExpression(IVHDLExpression expression, VHDLTypeDescriptor targettype, string template, bool needswrapping = false)
		{
			m_parent = expression;
			m_targettype = targettype;
			m_template = template;
			NeedsWrapping = needswrapping;
		}

		public bool NeedsWrapping { get; private set; }

		#region IVHDLExpression implementation
		public ICSharpCode.NRefactory.CSharp.Expression Expression { get { return m_parent.Expression; } }
		public Converter Converter { get { return m_parent.Converter; } }
		public IVHDLExpression WrappedExpression { get { return m_parent; } }
		public VHDLTypeDescriptor VHDLType { get { return m_targettype; } }
		public Mono.Cecil.TypeReference ResolvedSourceType { get { return m_parent.ResolvedSourceType; } }
		public string ResolvedString
		{
			get
			{
				return string.Format(m_template, m_parent.ResolvedString);
			}
		}
		#endregion

		public override string ToString()
		{
			return ResolvedString;
		}
	}
}

