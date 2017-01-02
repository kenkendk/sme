using System;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;
using System.Collections.Generic;

namespace SME.Render.VHDL.ILConvert.AugmentedExpression
{
	public class VHDLMemberReferenceExpression : VHDLTypedExpression<MemberReferenceExpression>
	{
		public VHDLMemberReferenceExpression(Converter converter, MemberReferenceExpression expression)
			: base(converter, expression)
		{
		}

		private MemberItem m_member;
		public MemberItem Member 
		{
			get
			{
				if (m_member == null)
					m_member = ResolveMemberReference(Expression);
				return m_member;
			}
		}

		public MemberItem BusVariableField
		{
			get
			{

				return ResolveMemberReference(Expression.Target);
			}


		}

		public override TypeReference ResolvedSourceType
		{
			get
			{
				
				return Member.ItemType;
			}
		}

		public bool IsArrayLengthReference
		{
			get
			{
				return !Member.DeclaringType.IsBusType() && !IsConstantReference && Member.DeclaringType != Converter.ProcType && Member.Item.DeclaringType.IsSameTypeReference(typeof(System.Array)) && Member.Item.Name == "Length";
			}
		}

		public override VHDLTypeDescriptor VHDLType
		{
			get
			{
				if (m_vhdlType == null)
				{
					if (IsArrayLengthReference)
						m_vhdlType = Converter.Information.VHDLTypes.GetByName("INTEGER");
					else
						m_vhdlType = Converter.Information.VHDLTypes.GetVHDLType(Member.Item, Member.ItemType);
				}

				return m_vhdlType;
			}
		}			
		public bool IsConstantReference
		{
			get
			{
				return Converter.IsConstantReference(this);
			}
		}

		protected override string ResolveToString()
		{
			if (Member.DeclaringType.IsEnum)
				return Renderer.ConvertToValidVHDLName(Member.DeclaringType.FullName + "." + Member.Name);

			//TODO: Register a class-wide constant for this value?
			if (IsArrayLengthReference)
				return new VHDLPrimitiveExpression(Converter, new PrimitiveExpression(Converter.ResolveArrayLengthOrPrimitive(this.Expression) - 1)).ResolvedString;

			if (Member.DeclaringType.IsBusType() || IsConstantReference)
			{
				var rawname = Member.DeclaringType.FullName + "." + Member.Name;
				if (rawname.StartsWith(Converter.ProcType.FullName + "/"))
					rawname = Member.DeclaringType.Name + "." + Member.Name;
				else if (rawname.StartsWith(Converter.ProcType.Namespace + "."))
					rawname = rawname.Substring(Converter.ProcType.Namespace.Length + 1);
				
				return Renderer.ConvertToValidVHDLName(rawname);
			}
			else
			{				
				if (Member.DeclaringType != Converter.ProcType)
				{
					if (Expression.Target is IdentifierExpression)
						return Renderer.ConvertToValidVHDLName((Expression.Target as IdentifierExpression).Identifier) + "." + Renderer.ConvertToValidVHDLName(Member.Name);

					return Expression.ToString();
				}

				return Renderer.ConvertToValidVHDLName(Member.Name);
			}
		}

	}
}

