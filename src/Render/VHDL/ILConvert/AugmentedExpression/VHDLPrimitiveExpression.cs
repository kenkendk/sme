using System;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;

namespace SME.Render.VHDL.ILConvert.AugmentedExpression
{
	public class VHDLPrimitiveExpression : VHDLTypedExpression<PrimitiveExpression>
	{
		public VHDLPrimitiveExpression(Converter converter, PrimitiveExpression expression)
			: base(converter, expression)
		{
		}

		public override TypeReference ResolvedSourceType
		{
			get
			{
				if (m_resolvedSourceType == null)
				{
					if (Expression.Value == null)
						m_resolvedSourceType = Converter.ImportType<UIntPtr>();
					else
						m_resolvedSourceType = Converter.ImportType(Expression.Value.GetType());
				}

				return m_resolvedSourceType;
			}
		}

		public override VHDLTypeDescriptor VHDLType
		{
			get
			{
				if (ResolvedSourceType.IsSameTypeReference<ulong>() || ResolvedSourceType.IsSameTypeReference<long>())
				{
					var value = Expression.Value;
					if (value is ulong && (ulong)value > int.MaxValue)
						return Converter.Information.VHDLTypes.GetStdLogicVector(64);
					else if (value is long && ((long)value > int.MaxValue || (long)value < int.MinValue))
						return Converter.Information.VHDLTypes.GetStdLogicVector(64);
				}


				var rt = Converter.Information.VHDLTypes.GetVHDLType(ResolvedSourceType);
				if (rt.IsSigned || rt.IsUnsigned)
					return VHDLTypes.INTEGER;
				
				return rt;
			}

		}

		protected override string ResolveToString()
		{
			if (Expression.Value == null)
				return "NIL";

			if (Expression.Value is bool)
			{
				if ((bool)Expression.Value == false)
					return "'0'";
				else if ((bool)Expression.Value == true)
					return "'1'";
				else
					throw new Exception(string.Format("Unsupported primitive: {0}", Expression));
			}
			else if (ResolvedSourceType.IsPrimitive)
			{
				if (VHDLType.IsStdLogicVector && !(VHDLType.IsNumeric || VHDLType.IsSystemType))
				{
					string binstr = null;
					// For larger values, we must encode them as vectors
					if (Expression.Value is ulong)
					{
						var uvalue = (ulong)Expression.Value;
						if (uvalue > int.MaxValue)
						{
							binstr =
								Convert.ToString((int)((uvalue >> 32) & 0xffffffff), 2).PadLeft(32, '0') +
								Convert.ToString((int)(uvalue & 0xffffffff), 2).PadLeft(32, '0');
						}
					}
					else
					{
						var lvalue = (long)Expression.Value;
						if (lvalue > int.MaxValue || lvalue < int.MinValue)
						{
							binstr =
								Convert.ToString((int)((lvalue >> 32) & 0xffffffff), 2).PadLeft(32, '0') +
								Convert.ToString((int)(lvalue & 0xffffffff), 2).PadLeft(32, '0');
						}
					}

					return string.Format("STD_LOGIC_VECTOR'(\"{0}\")", binstr);
				}
				else
					return Expression.Value.ToString();
			}
			else
				throw new Exception(string.Format("Unsupported primitive: {0}", Expression));			
		}
	}
}

