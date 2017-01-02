using System;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;

namespace SME.Render.VHDL.ILConvert.AugmentedExpression
{
	public class VHDLConditionalExpression : VHDLTypedExpression<ConditionalExpression>
	{
		public VHDLConditionalExpression(Converter converter, ConditionalExpression expression)
			: base(converter, expression)
		{
		}

		private IVHDLExpression m_condition;
		private IVHDLExpression m_true;
		private IVHDLExpression m_false;

		private void ResolveExpressions()
		{
			m_condition = Converter.ResolveExpression(Expression.Condition);
			m_true = Converter.ResolveExpression(Expression.TrueExpression);
			m_false = Converter.WrapConverted(Converter.ResolveExpression(Expression.FalseExpression), m_true.VHDLType);
		}

		public override TypeReference ResolvedSourceType
		{
			get
			{
				return True.ResolvedSourceType;
			}
		}

		public override VHDLTypeDescriptor VHDLType
		{
			get
			{
				return True.VHDLType;
			}
		}

		public IVHDLExpression Condition
		{
			get
			{
				if (m_condition == null)
					ResolveExpressions();
				return m_condition;
			}
		}

		public IVHDLExpression True
		{
			get
			{
				if (m_true == null)
					ResolveExpressions();
				return m_true;
			}
		}

		public IVHDLExpression False
		{
			get
			{
				if (m_false == null)
					ResolveExpressions();
				return m_false;
			}
		}

		protected override string ResolveToString()
		{
			if (Converter.SUPPORTS_VHDL_2008)
			{
				// Simpler with a conditional
				return string.Format("{0} when {1} else {2}", True.ResolvedString, Condition.ResolvedString, False.ResolvedString);
			}
			else
			{
				// We need to use a temp
				var varname = Converter.RegisterTemporaryVariable(this.ResolvedSourceType);

				Converter.OutputIfElseStatement(
					new IfElseStatement(
						Expression.Condition.Clone(), 
						new ExpressionStatement(new AssignmentExpression(new IdentifierExpression(varname), Expression.TrueExpression.Clone())),
						Expression.FalseExpression == ICSharpCode.NRefactory.CSharp.Expression.Null ? null : 
						new ExpressionStatement(new AssignmentExpression(new IdentifierExpression(varname), Expression.FalseExpression.Clone()))
					)
				);

				return Converter.WrapConverted(Converter.ResolveExpression(new IdentifierExpression(varname)), this.VHDLType).ResolvedString;
			}
		}
	}
}

