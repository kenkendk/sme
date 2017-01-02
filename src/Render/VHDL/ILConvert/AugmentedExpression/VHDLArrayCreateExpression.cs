using System;
using System.Linq;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;

namespace SME.Render.VHDL.ILConvert.AugmentedExpression
{
	public class VHDLArrayCreateExpression : VHDLTypedExpression<ArrayCreateExpression>
	{
		private readonly MemberItem m_target;
		public VHDLArrayCreateExpression(Converter converter, ArrayCreateExpression expression)
			: base(converter, expression)
		{
			var parent = expression.Parent as AssignmentExpression;
			if (parent == null)
				throw new ArgumentException("Unable to resolve array creation without an assignment target");

			m_target = Converter.ResolveMemberReference(parent.Left);
			m_resolvedSourceType = m_target.ItemType;
			m_vhdlType = Converter.Information.VHDLTypes.GetVHDLType(m_target.Item, m_resolvedSourceType);
		}

		public override TypeReference ResolvedSourceType { get { return m_resolvedSourceType; } }

		/*public override VHDLTypeDescriptor VHDLType
		{
			get
			{
				if (m_vhdlType == null)
				{
					var eltype = Converter.Information.VHDLTypes.GetVHDLType(ResolvedSourceType);
				}

				return m_vhdlType;
			}
		}

		public override TypeReference ResolvedSourceType
		{
			get
			{
				if (m_resolvedSourceType == null)
				{

					var ti = Expression.Annotation<ICSharpCode.Decompiler.Ast.TypeInformation>();

					if (ti != null && ti.InferredType != null)
						return m_resolvedSourceType = ti.InferredType;

					var init = Expression.Initializer;
					var children = Children;

					var basetype = Converter.ResolveType((Expression.Type as ComposedType).BaseType);
					var res = basetype.Resolve().IsArray;
					basetype.GetElementType();


					Console.WriteLine(basetype.GetType().FullName);
					// TODO: Return the array type?
					//m_resolvedSourceType = basetype;
				}
				return m_resolvedSourceType;
			}
		}*/

		private IVHDLExpression[] m_children;

		public IVHDLExpression[] Children
		{
			get
			{
				if (m_children == null)
					m_children = Expression.Initializer.Elements.ToArray().Select(x => Converter.ResolveExpression(x)).ToArray();

				return m_children;
			}
		}

		protected override string ResolveToString()
		{
			if (ResolvedSourceType == null)
				throw new Exception(string.Format("Could not resolve type: {0}", Expression.Type));

			return "(" + string.Join(", ", Children.Select(x => Converter.WrapConverted(x, Converter.Information.VHDLTypes.GetByName(this.VHDLType.ElementName)).ResolvedString)) + ")";
		}

	}
}
