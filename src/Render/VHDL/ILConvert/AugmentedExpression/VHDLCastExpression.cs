using System;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;
using System.Linq;

namespace SME.Render.VHDL.ILConvert.AugmentedExpression
{
	public class VHDLCastExpression : VHDLTypedExpression<CastExpression>
	{
		public VHDLCastExpression(Converter converter, CastExpression expression)
			: base(converter, expression)
		{
		}

		public override TypeReference ResolvedSourceType
		{
			get
			{
				if (m_resolvedSourceType == null)
				{
					m_resolvedSourceType = Converter.ResolveType(Expression.Type);
					if (m_resolvedSourceType == null)
					{
						var components = Expression.Type.ToString().Split('.');
						var targetref = new TypeReference(string.Join(".", components.Take(components.Length - 1)), components.Last(), Converter.ProcType.Module, Converter.ProcType.Scope);
						m_resolvedSourceType = targetref.Resolve();
					}
				}
				return m_resolvedSourceType;
			}
		}

		public static Expression RemoveUIntPtrCast(Converter converter, Expression child)
		{
			// This fixes a case where the NRefactory code injects a cast to UIntPtr, even though none is present in the code, nor the IL 
			var wrap = false;
			var self = child;
			if (self is ParenthesizedExpression)
			{
				self = (self as ParenthesizedExpression).Expression;
				wrap = true;
			}

			if (self is CastExpression && converter.ResolveType((self as CastExpression).Type).IsSameTypeReference(converter.ImportType<UIntPtr>()))
				self = (self as CastExpression).Expression.Clone();
			else
				self = child;

			if (wrap && self != child)
				self = new ParenthesizedExpression(self);

			return self;
		}

		public static Expression RemoveDoubleCast(Converter converter, VHDLCastExpression self, Expression child)
		{
			// This fixes a case where the code has double castings that introduce unwanted parenthesis 

			var tmp = converter.ResolveExpression(child);
			if (tmp is VHDLCastExpression && tmp.ResolvedSourceType == self.ResolvedSourceType)
				return (tmp as VHDLCastExpression).Expression.Expression;

			return child;
		}

		protected override string ResolveToString()
		{
			if (ResolvedSourceType == null)
				throw new Exception(string.Format("Could not resolve type: {0}", Expression.Type));

			var child = Expression.Expression;
			child = RemoveDoubleCast(Converter, this, child);

			// Fix native format stored (or really parsed by Cecil) as a different type than expected
			if (child is PrimitiveExpression)
			{
				var v = (child as PrimitiveExpression).Value;
				if (v is int && ResolvedSourceType.IsType<ulong>())
					child = new PrimitiveExpression((ulong)(uint)(int)v);
				else if (v is long && ResolvedSourceType.IsType<ulong>())
					child = new PrimitiveExpression((ulong)(long)v);
				else if (v is int && ResolvedSourceType.IsType<uint>())
					child = new PrimitiveExpression((uint)(int)v);
			}

			return Converter.WrapConverted(Converter.ResolveExpression(RemoveUIntPtrCast(Converter, child)), VHDLType, true).ResolvedString;
		}
	}
}

