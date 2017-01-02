using System;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;
using System.Collections.Generic;

namespace SME.Render.VHDL.ILConvert.AugmentedExpression
{
	public abstract class VHDLTypedExpression<TExpression> : IVHDLExpression
		where TExpression : Expression
	{
		/// <summary>
		/// Gets the underlying expression
		/// </summary>
		/// <value>The expression.</value>
		public TExpression Expression { get; private set; }

		/// <summary>
		/// Gets the parent converter instance
		/// </summary>
		/// <value>The converter.</value>
		public Converter Converter { get; private set; }

		/// <summary>
		/// Gets the wrapped expression, null if not wrapped
		/// </summary>
		/// <value>The wrapped expression.</value>
		public VHDLTypedExpression<TExpression> WrappedExpression { get; set; }

		protected VHDLTypeDescriptor m_vhdlType;

		/// <summary>
		/// Gets the expression VHDL type
		/// </summary>
		/// <value>The type of the VHDL.</value>
		public virtual VHDLTypeDescriptor VHDLType
		{
			get
			{
				if (m_vhdlType == null)
					m_vhdlType = Converter.Information.VHDLTypes.GetVHDLType(ResolvedSourceType);
				
				return m_vhdlType;
			}
		}

		protected TypeReference m_resolvedSourceType;

		/// <summary>
		/// Gets the resolved statement type
		/// </summary>
		/// <value>The type of the resolved source.</value>
		public abstract TypeReference ResolvedSourceType { get; }

		protected string m_resolvedString = null;

		/// <summary>
		/// Gets the resolved (compiled) string
		/// </summary>
		/// <value>The resolved string.</value>
		public string ResolvedString
		{
			get
			{
				if (m_resolvedString == null)
					m_resolvedString = ResolveToString();

				return m_resolvedString;
			}
		}

		protected abstract string ResolveToString();

		public VHDLTypedExpression(Converter converter, TExpression expression)
		{
			this.Converter = converter;
			this.Expression = expression;
		}

		IVHDLExpression IVHDLExpression.WrappedExpression
		{
			get
			{
				return this.WrappedExpression;
			}
		}

		Expression IVHDLExpression.Expression
		{
			get
			{
				return this.Expression;
			}
		}

		protected MemberItem ResolveMemberReference(Expression tg)
		{
			return Converter.ResolveMemberReference(tg);
		}

		protected bool IsVariableExpression(IVHDLExpression s)
		{
			return Converter.IsVariableExpression(s);
		}

		public override string ToString()
		{
			return this.GetType().Name + ": " + ResolvedString;
		}

	}
}

