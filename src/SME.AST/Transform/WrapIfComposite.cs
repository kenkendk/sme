using System;
using System.Linq;

namespace SME.AST.Transform
{
    /// <summary>
    /// Puts a parenthesized expression around an expression that cannot be non-wrapped.
    /// </summary>
    public class WrapIfComposite : IASTTransform
    {
        /// <summary>
        /// The normal types that do not have to be wrapped.
        /// </summary>
        private static readonly Type[] CORE_TYPES = new [] {
            typeof(IndexerExpression),
            typeof(MemberReferenceExpression),
            typeof(MethodReferenceExpression),
            typeof(PrimitiveExpression),
            typeof(IdentifierExpression),
            typeof(IndexerExpression),
            typeof(InvocationExpression),
            typeof(ParenthesizedExpression),
            typeof(CastExpression),
            typeof(EmptyExpression)
        };

        /// <summary>
        /// The current set of types that are not mapped.
        /// </summary>
        protected readonly Type[] SIMPLE_TYPES;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SME.AST.Transform.WrapIfComposite"/> class, using the basic types for non-wrapping.
        /// </summary>
        public WrapIfComposite()
        {
            SIMPLE_TYPES = CORE_TYPES;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SME.AST.Transform.WrapIfComposite"/> class.
        /// </summary>
        /// <param name="additionaltypes">Any additional type types to consider as pre-wrapped.</param>
        /// <param name="replace">If set to <c>true</c> the given list replaces the basic types, if set to <c>false</c> the given types are appended to the basic types.</param>
        public WrapIfComposite(Type[] additionaltypes, bool replace = false)
        {
            SIMPLE_TYPES = (replace ? new Type[0] : CORE_TYPES).Concat(additionaltypes).ToArray();
        }

        /// <summary>
        /// Applies the transformation.
        /// </summary>
        /// <returns>The transformed item.</returns>
        /// <param name="item">The item to visit.</param>
        public virtual ASTItem Transform(ASTItem item)
        {
            var exp = item as Expression;
            if (exp == null)
                return item;

            // If this is top-level, we do not want a wrapping
            if (exp.Parent is Statement || exp.Parent is ParenthesizedExpression)
                return item;

            if (!SIMPLE_TYPES.Any(x => exp.GetType().IsAssignableFrom(x)) && !(exp.Parent is AssignmentExpression))
            {
                var np = new ParenthesizedExpression()
                {
                    Expression = exp,
                    Parent = exp.Parent,
                    Name = exp.Name,
                    SourceExpression = exp.SourceExpression,
                    SourceResultType = exp.SourceResultType
                };

                exp.ReplaceWith(np);
                exp.Parent = np;

                return np;
            }

            return item;
        }
    }
}
