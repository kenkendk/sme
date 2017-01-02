using System;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;

namespace SME.Render.VHDL.ILConvert
{
	public class MemberItem
	{
		public enum MemberType
		{
			Variable,
			Unknown,
			Field,
			Property,
			Method
		}

		public MemberType Type { get; private set; }
		public string Name { get; private set; }
		public Expression Expression { get; private set; }
		public TypeReference ItemType { get; private set; }
		public IMemberDefinition Item { get; private set; }
		public TypeDefinition DeclaringType { get; private set; }

		public MemberItem(IdentifierExpression expr, TypeReference itemtype)
		{
			this.Expression = expr;
			this.Name = expr.Identifier;
			this.ItemType = itemtype;
			this.Type = MemberType.Variable;
		}

		public MemberItem(Expression expr, IMemberDefinition member, TypeDefinition declaringtype)
		{
			this.Expression = expr;
			this.Item = member;
			this.Name = member.Name;
			this.Type = MemberType.Unknown;
			this.DeclaringType = declaringtype;

			if (member is FieldDefinition)
			{
				this.Type = MemberType.Field;
				this.ItemType = (member as FieldDefinition).FieldType;
			}
			else if (member is PropertyDefinition)
			{
				this.Type = MemberType.Property;
				this.ItemType = (member as PropertyDefinition).PropertyType;
			}
			else if (member is MethodDefinition)
			{
				this.Type = MemberType.Method;
				this.ItemType = (member as MethodDefinition).ReturnType;
			}

			ResolveGenericParameterType(declaringtype);

			ResolveOuterscope(declaringtype);

		}

		private void ResolveOuterscope(TypeDefinition declaringtype)
		{
			if (Item.DeclaringType != declaringtype && ItemType.IsBusType() && declaringtype.SelfAndBases().Contains(Item.DeclaringType))
			{
				if (Item.DeclaringType == Item.DeclaringType.Module.Import(typeof(Process)).Resolve() || Item.DeclaringType == Item.DeclaringType.Module.Import(typeof(SimpleProcess)).Resolve())
					return;

				var superclass = declaringtype.NestedTypes.Where(x => x.IsAssignableFrom(ItemType)).FirstOrDefault();
				if (superclass == null)
					Console.WriteLine("Unable to find implementing super-interface for {0}", Name);
				else
					ItemType = superclass;
			}
		}

		private void ResolveGenericParameterType(TypeDefinition declaringtype)
		{
			// For a generic parameter, we search upwards until we find a matching name
			if (this.ItemType.IsGenericParameter)
			{

				var cur = declaringtype;
				var name = this.ItemType.FullName;
				var map = new Dictionary<string, TypeReference>();

				while (cur != null)
				{
					TypeReference bt = cur;
					while (bt != null)
					{
						if (bt is GenericInstanceType)
						{
							UpdateGenericNameMap(map, bt as GenericInstanceType);
							if (map.ContainsKey(name))
							{
								this.ItemType = map[name];
								return;
							}
						}

						bt = bt.Resolve().BaseType;
					}

					cur = cur.DeclaringType;
				}

				Console.WriteLine("Failed to resolve generic property: {0} ({1})", this.Name, this.ItemType.FullName);
			}
		}


		private void UpdateGenericNameMap(Dictionary<string, TypeReference> map, GenericInstanceType g)
		{
			var gr = g.ElementType.Resolve();
			var trs = g.GenericArguments.ToArray();
			var names = gr.GenericParameters.ToArray();
			for (var i = 0; i < names.Length; i++)
				map[names[i].FullName] = trs[i];
		}

		public CustomAttribute GetAttribute<T>()
		{
			return Item.GetAttribute<T>();
		}

		public bool IsVariable { get { return Item.IsVariable(); } }
		public bool IsTopLevelInput { get { return Item.IsTopLevelInput(); } }
		public bool IsTopLevelOutput { get { return Item.IsTopLevelOutput(); } }

		public string ToVHDLName(TypeDefinition scope, Expression target)
		{
			return Item.ToVHDLName(scope, target, DeclaringType);
		}
	}
}

