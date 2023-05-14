using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SME.AST
{
    /// <summary>
    /// A collection of static extension methods.
    /// </summary>
    public static class HelperMethods
    {

        /// <summary>
        /// Returns true, if the given type is an array type.
        /// </summary>
        /// <param name="its">The type to check.</param>
        public static bool IsArrayType(this ITypeSymbol its)
        {
            return its is IArrayTypeSymbol || its.IsFixedArrayType();
        }

        /// <summary>
        /// Returns true, if the given type is a type of the type parameter.
        /// </summary>
        /// <typeparam name="T">The type parameter to compare against.<typeparam>
        /// <param name="its">The given type.</param>
        public static bool IsType<T>(this ITypeSymbol its)
        {
            return SymbolEqualityComparer.Default.Equals(LoadType(its, typeof(T)), its);
        }

        /// <summary>
        /// Returns true, if the given type is a Bus type.
        /// </summary>
        /// <param name="td">The type to evaluate.</param>
        public static bool IsBusType(this ITypeSymbol td)
        {
            if (td.IsArrayType()) {
                var at = (IArrayTypeSymbol)td;
                return at.ElementType.IsBusType();
            } else {
                return td.Interfaces.Any(x => SymbolEqualityComparer.Default.Equals(ParseProcesses.m_compilation.GetTypeByMetadataName(typeof(IBus).FullName), x));
            }
        }
        /// <summary>
        /// Returns <c>true</c> if the type has an attribute of the given type.
        /// </summary>
        /// <param name="bt">The type to evaluate.</param>
        /// <typeparam name="T">The attribute type to check for.</typeparam>
        public static bool HasAttribute<T>(this Type bt)
        {
            return HasAttribute(bt, typeof(T));
        }

        /// <summary>
        /// Returns <c>true</c>, if the type has an attribute of the given type.
        /// </summary>
        /// <param name="bt">The type to evaluate.</param>
        /// <param name="attrtype">The attribute type to check for.</param>
        public static bool HasAttribute(this Type bt, Type attrtype)
        {
            return bt.GetCustomAttributes(attrtype, true).Any();
        }

        /// <summary>
        /// Returns true, if the given symbol has the given attribute.
        /// </summary>
        /// <param name="its">The given symbol.</param>
        /// <param name="t">The given attribute.</param>
        public static bool HasAttribute(this ISymbol its, Type t)
        {
            return its.GetAttribute(t) != null;
        }

        /// <summary>
        /// Returns true, if the given symbol has the given attribute.
        /// </summary>
        /// <typeparam name="T">The given attribute.</typeparam>
        /// <param name="its">The given symbol</param>
        public static bool HasAttribute<T>(this ISymbol its)
        {
            return its.GetAttribute<T>() != null;
        }

        /// <summary>
        /// Gets the attribute of the given type, associated with the given symbol.
        /// </summary>
        /// <param name="its">The given symbol.</param>
        /// <param name="t">The given type.</param>
        public static AttributeData GetAttribute(this ISymbol its, Type t)
        {
            return its.GetAttributes(t).FirstOrDefault();
        }

        /// <summary>
        /// Gets the attribute of the given type, associated with the given symbol.
        /// </summary>
        /// <typeparam name="T">The given attribute type.</typeparam>
        /// <param name"its">The given symbol.</param>
        public static AttributeData GetAttribute<T>(this ISymbol its)
        {
            return its.GetAttributes<T>().FirstOrDefault();
        }

        /// <summary>
        /// Gets the collection of attributes of the given type, associated with the given symbol.
        /// </summary>
        /// <param name="its">The given symbol.</param>
        /// <param name="t">The given type.</param>
        public static IEnumerable<AttributeData> GetAttributes(this ISymbol its, Type t)
        {
            return its.GetAttributes()
                .Where(x => Type.GetType(x.AttributeClass.ToDisplayString()) == t);
        }

        /// <summary>
        /// Gets the collection of attributes of the given type, associated with the given symbol.
        /// </summary>
        /// <typeparam name="T">The given attribute type.</typeparam>
        /// <param name="its">The given symbol.</param>
        public static IEnumerable<AttributeData> GetAttributes<T>(this ISymbol its)
        {
            var target = typeof(T);
            var asm = target.Assembly;
            return its.GetAttributes().Where(x => asm.GetType(x.AttributeClass.GetFullMetadataName()) == target);
        }

        /// <summary>
        /// Loads the symbol of the given syntax node, by looking in the given semantic models.
        /// </summary>
        /// <param name="sn">The given syntax node.</param>
        /// <param name="m_semantics">The given semantic models.</param>
        public static ISymbol LoadSymbol(this SyntaxNode sn, IEnumerable<SemanticModel> m_semantics)
        {
            return m_semantics
                .Select(x =>
                {
                    try
                    {
                        return x.GetDeclaredSymbol(sn);
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                })
                .FirstOrDefault(x => x != null);
        }

        /// <summary>
        /// Loads the type of the given syntax node, by looking in the given semantic models.
        /// </summary>
        /// <param name="sn">The given syntax node.</param>
        /// <param name="m_semantics">The given semantic models.</param>
        public static ITypeSymbol LoadType(this SyntaxNode sn, IEnumerable<SemanticModel> m_semantics)
        {
            return m_semantics
                .Select(x =>
                {
                    try
                    {
                        return x.GetTypeInfo(sn).Type;
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                })
                .FirstOrDefault(x => x != null);
        }

        /// <summary>
        /// Loads a data flow analysis of the given method, by looking through the given semantic models.
        /// </summary>
        /// <param name="mds">The given method.</param>
        /// <param name="m_semantics">The given semantic models.</param>
        public static DataFlowAnalysis LoadDataFlow(this MethodDeclarationSyntax mds, IEnumerable<SemanticModel> m_semantics)
        {
            return m_semantics
                .Select(x =>
                {
                    try
                    {
                        return x.AnalyzeDataFlow(mds.Body);
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                })
                .FirstOrDefault(x => x != null);
        }

        /// <summary>
        /// Gets the full name of the given symbol.
        /// </summary>
        /// <param name="isy">The given symobl.</param>
        public static string GetFullMetadataName(this ISymbol isy)
        {
            string res = isy.Name;
            var tmp = isy.ContainingNamespace;
            while (!tmp.IsGlobalNamespace)
            {
                res = $"{tmp.Name}.{res}";
                tmp = tmp.ContainingNamespace;
            }
            return res;
        }

        /// <summary>
        /// Gets the syntax node corresponding to the given symbol.
        /// </summary>
        /// <param name="isy">The given symbol.</param>
        public static SyntaxNode GetSyntax(this ISymbol isy)
        {
            return isy.DeclaringSyntaxReferences.First().GetSyntax();
        }

        /// <summary>
        /// Gets the class declaration of the given symbol, if any.
        /// </summary>
        /// <param name="isy">The given symbol.</param>
        public static ClassDeclarationSyntax GetClassDecl(this ISymbol isy)
        {
            return isy.GetSyntax() as ClassDeclarationSyntax;
        }

        /// <summary>
        /// Returns true, if the two given symbols are equal.
        /// <summary>
        /// <param name="a">The first symbol to compare.</param>
        /// <param name="b">The second symbol to compare.</param>
        public static bool IsSameTypeReference(this ITypeSymbol a, ITypeSymbol b)
        {
            return SymbolEqualityComparer.Default.Equals(a, b);
        }

        /// <summary>
        /// Returns true, if the given symbol is equal to the given reflection type.
        /// </summary>
        /// <param name="a">The given symbol.</param>
        /// <param name="b">The given reflection type.</param>
        public static bool IsSameTypeReference(this ITypeSymbol a, Type b)
        {
            var itb = ParseProcesses.m_compilation.GetTypeByMetadataName(b.FullName);
            return a.IsSameTypeReference(itb);
        }

        /// <summary>
        /// Returns true, if the given symbol is equal to the given type parameter.
        /// </summary>
        /// <typeparam name="T">The given type parameter.</typeparameter>
        /// <param name="a">The given symbol.</param>
        public static bool IsSameTypeReference<T>(this ITypeSymbol a)
        {
            return a.IsSameTypeReference(typeof(T));
        }

        /// <summary>
        /// Returns all properties found in the type and its base types.
        /// </summary>
        /// <returns>The properties found.</returns>
        /// <param name="self">The type to get the fields from.</param>
        public static IEnumerable<System.Reflection.PropertyInfo> GetPropertiesRecursive(this Type self, System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly | System.Reflection.BindingFlags.Public)
        {
            var x = self;
            while (x != null)
            {
                foreach (var f in x.GetProperties(flags))
                    yield return f;

                var interfaces = x.GetInterfaces();

                if (interfaces != null)
                    foreach (var n in interfaces)
                        foreach (var f in n.GetProperties(flags))
                            yield return f;

                x = x.BaseType;
            }
        }

        /// <summary>
        /// Argument input/output types.
        /// </summary>
        public enum ArgumentInOut
        {
            /// <summary>
            /// The argument is an exclusive input argument.
            /// </summary>
            In,
            /// <summary>
            /// The argument is an exclusive output argument.
            /// </summary>
            Out,
            /// <summary>
            /// The argument is both an input and an output argument.
            /// </summary>
            InOut
        }

        /// <summary>
        /// Returns true, if the given type is an array.
        /// </summary>
        /// <param name="t">The type to examine.</param>
        public static bool IsArrayType(this Type t)
        {
            return t.IsArray || t.IsFixedArrayType();
        }

        /// <summary>
        /// Returns true, if the supplied symbol is a fixed array.
        /// </summary>
        /// <param name="tr">The symbol to examine.</param>
        public static bool IsFixedArrayType(this ITypeSymbol tr)
        {
            if (tr is INamedTypeSymbol)
            {
                var it = tr as INamedTypeSymbol;
                return it.IsGenericType && tr.IsSameTypeReference(typeof(IFixedArray<>));
            }
            else
                return false;
        }

        /// <summary>
        /// Returns true, if the supplied type is a fixed array.
        /// </summary>
        /// <param name="t">The type to examine.</param>
        public static bool IsFixedArrayType(this Type t)
        {
            return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IFixedArray<>);
        }

        /// <summary>
        /// Gets the type of the array elements.
        /// </summary>
        /// <param name="tr">The type reference to examine.</param>
        public static ITypeSymbol GetArrayElementType(this ITypeSymbol tr)
        {
            if (tr is IArrayTypeSymbol)
                return (tr as IArrayTypeSymbol).ElementType;
            else if (tr.IsFixedArrayType())
                return (tr as INamedTypeSymbol).TypeParameters.First();
            else
                throw new Exception($"GetArrayElementType called on non-array: {tr.Name}");
        }

        /// <summary>
        /// Gets the type of the array elements.
        /// </summary>
        /// <param name="t">The type to examine.</param>
        public static Type GetArrayElementType(this Type t)
        {
            if (t.IsArray)
                return t.GetElementType();
            else if (t.IsFixedArrayType())
                return t.GetGenericArguments().First();
            else
                throw new Exception($"GetArrayElementType called on non-array: {t.FullName}");
        }

        /// <summary>
        /// Gets the length of the given fixed array through its attributes.
        /// </summary>
        /// <param name="its">The given fixed array</param>
        public static int GetFixedArrayLength(this ITypeSymbol its)
        {
            var attr = its.GetAttribute<FixedArrayLengthAttribute>();
            var arg = attr.ConstructorArguments.First().Value;
            return (int)Convert.ChangeType(arg, typeof(int));
        }

        /// <summary>
        /// Gets the length of the fixed array.
        /// </summary>
        /// <returns>The fixed array length.</returns>
        /// <param name="member">The member to examine.</param>
        public static int GetFixedArrayLength(this MemberDeclarationSyntax member, IEnumerable<SemanticModel> m_semantics)
        {
            var attr = member.LoadSymbol(m_semantics).GetAttribute<FixedArrayLengthAttribute>();
            var arg = attr.ConstructorArguments.First().Value;
            return (int)Convert.ChangeType(arg, typeof(int));
        }

        /// <summary>
        /// Gets the length of the fixed array.
        /// </summary>
        /// <returns>The fixed array length.</returns>
        /// <param name="member">The member to examine.</param>
        public static int GetFixedArrayLength(this System.Reflection.MemberInfo member)
        {
            var attr = member.GetCustomAttributes(typeof(FixedArrayLengthAttribute), false).Cast<FixedArrayLengthAttribute>().First();
            return attr.Length;
        }

        /// <summary>
        /// Gets the length of a fixed-length array.
        /// </summary>
        /// <returns>The fixed array length.</returns>
        /// <param name="element">The element to get the length for.</param>
        public static int GetArrayLength(DataElement element)
        {
            if (element is Constant)
                element = ((Constant)element).ArrayLengthSource ?? element;

            if (element.DefaultValue is Array)
                return ((Array)element.DefaultValue).Length;
            else if (element.DefaultValue is EmptyArrayCreateExpression)
                return ResolveIntegerValue(((EmptyArrayCreateExpression)element.DefaultValue).SizeExpression);
            else if (element.DefaultValue is ArrayCreateExpression)
                return ((ArrayCreateExpression)element.DefaultValue).ElementExpressions.Length;
            else if (element.Source is System.Reflection.MemberInfo)
                return GetFixedArrayLength((System.Reflection.MemberInfo)element.Source);

            throw new Exception($"Unable to get size of array: {element.Name}");
        }

        /// <summary>
        /// Loads the specified reflection Type and returns the equivalent Microsoft.CodeAnalysis symbol.
        /// </summary>
        /// <returns>The loaded type.</returns>
        /// <param name="source">The source that provides the context.</param>
        /// <param name="t">The type to load.</param>
        public static ITypeSymbol LoadType(this ITypeSymbol source, Type t)
        {
            return ParseProcesses.m_compilation.GetTypeByMetadataName(t.FullName);
        }

        /// <summary>
        /// Returns a value indicating what directions an argument has
        /// </summary>
        /// <returns>The argument directions.</returns>
        /// <param name="n">The argument to examine.</param>
        public static ArgumentInOut GetArgumentInOut(this IParameterSymbol n, DataFlowAnalysis MSCAFlow)
        {
            // TODO A fix had been applied to the decompiler version, to add support to pass objects by reference to functions. It is kept here in case it doesn't work any more.
            /*
            var inarg = (n.Attributes & ParameterAttributes.In) == ParameterAttributes.In || n.IsIn;
            var outarg = (n.Attributes & ParameterAttributes.Out) == ParameterAttributes.Out || n.IsOut;
            var inoutarg = (inarg && outarg) || (!n.IsIn && !n.IsOut && n.ParameterType.IsByReference);
            var inoutoverride = n.ParameterType.IsArray && !((n.Attributes & ParameterAttributes.Out) == ParameterAttributes.Out || (n.Attributes & ParameterAttributes.In) == ParameterAttributes.In);
            return inoutarg || inoutoverride ? ArgumentInOut.InOut : (outarg ? ArgumentInOut.Out : ArgumentInOut.In);
            */
            var inarg = n.HasAttribute<System.Runtime.InteropServices.InAttribute>() || (MSCAFlow.Succeeded && MSCAFlow.DataFlowsIn.Contains(n));
            var outarg = n.HasAttribute<System.Runtime.InteropServices.OutAttribute>() || (MSCAFlow.Succeeded && MSCAFlow.DataFlowsOut.Contains(n));
            var inoutarg = inarg && outarg;
            var inoutoverride = inarg || outarg;
            var isarray = n.Type.IsArrayType();
            return inoutarg || (isarray && !inoutoverride) ? ArgumentInOut.InOut : (inarg ? ArgumentInOut.In : ArgumentInOut.Out);
        }

        /// <summary>
        /// Returns true, if the given symbol is an enum type.
        /// </summary>
        /// <param name="its">The given symbol.</param>
        public static bool IsEnum(this ITypeSymbol its)
        {
            if (its is INamedTypeSymbol)
                return ((INamedTypeSymbol)its).EnumUnderlyingType != null;
            else
                return false;
        }

        /// <summary>
        /// Returns the target variable or signal, or null.
        /// </summary>
        /// <returns>The target variable or signal.</returns>
        /// <param name="self">The item to examine.</param>
        public static DataElement GetTarget(this ASTItem self)
        {
            if (self == null)
                return null;
            if (self is DataElement)
                return (DataElement)self;
            if (self is IdentifierExpression)
                return ((IdentifierExpression)self).Target;
            if (self is MemberReferenceExpression)
                return GetTarget(((MemberReferenceExpression)self).Target);
            if (self is WrappingExpression)
                return GetTarget(((WrappingExpression)self).Expression);
            if (self is CustomExpression)
                return ((CustomExpression)self).GetTarget();

            return null;
        }

        /// <summary>
        /// Removes parenthesis and type casts to get the underlying item.
        /// </summary>
        /// <returns>The unwrapped expression.</returns>
        /// <param name="self">The expression to unwrap.</param>
        public static Expression GetUnwrapped(this Expression self)
        {
            var cur = self;
            while (cur != null)
            {
                if (cur is WrappingExpression)
                    cur = ((WrappingExpression)cur).Expression;
                else if (cur is CustomExpression && ((CustomExpression)cur).Children.Length == 1)
                    cur = ((CustomExpression)cur).Children[0];
                else
                    break;
            }

            return cur ?? self;
        }

        /// <summary>
        /// Sets the target value.
        /// </summary>
        /// <returns>The target variable or signal.</returns>
        /// <param name="self">The item to set the element on.</param>
        /// <param name="target">The value to set.</param>
        public static void SetTarget(this ASTItem self, DataElement target)
        {
            if (self is IdentifierExpression)
                ((IdentifierExpression)self).Target = target;
            else if (self is MemberReferenceExpression)
                SetTarget(((MemberReferenceExpression)self).Target, target);
            else if (self is WrappingExpression)
                SetTarget(((WrappingExpression)self).Expression, target);
            else if (self is Expression && self != ((Expression)self).GetUnwrapped())
                SetTarget(((Expression)self).GetUnwrapped(), target);
            else
                throw new Exception($"Unable to set target on item of type {self.GetType().FullName}");
        }

        /// <summary>
        /// Reverse walks the tree to find the next parent of the given type or null.
        /// </summary>
        /// <returns>The nearest parent or null.</returns>
        /// <param name="self">The item ot get the parent for.</param>
        /// <typeparam name="T">The data type to look for.</typeparam>
        public static T GetNearestParent<T>(this ASTItem self)
            where T : ASTItem
        {
            return (T)GetNearestParent(self, typeof(T));
        }

        /// <summary>
        /// Reverse walks the tree to find the next parent of the given type or null.
        /// </summary>
        /// <returns>The nearest parent or null.</returns>
        /// <param name="self">The item ot get the parent for.</param>
        /// <param name="parentType">The data type to look for.</param>
        public static ASTItem GetNearestParent(this ASTItem self, Type parentType)
        {
            if (self == null)
                return null;
            var p = self.Parent;
            while (p != null && !parentType.IsAssignableFrom(p.GetType()))
                p = p.Parent;

            return p;
        }

        /// <summary>
        /// Extracts an integer value from a data element.
        /// </summary>
        /// <returns>The integer value.</returns>
        /// <param name="expression">The expression to extract the value from.</param>
        private static int ResolveIntegerValue(Expression expression)
        {
            expression = expression.GetUnwrapped();
            if (expression is PrimitiveExpression)
                return (int)Convert.ChangeType(((AST.PrimitiveExpression)expression).Value, typeof(int));
            var target = expression.GetTarget();
            if (target.DefaultValue != null)
                return (int)Convert.ChangeType(target.DefaultValue, typeof(int));
            if (target is Constant && ((Constant)target).ArrayLengthSource != null)
                return GetArrayLength(target);

            throw new Exception($"Cannot extract integer value from: {expression.SourceExpression}");
        }

        /// <summary>
        /// Returns the start, end and increment integer values for a statically sized for loop.
        /// </summary>
        /// <returns>The static for loop start, end and increment values.</returns>
        /// <param name="self">The statement to extract the values for.</param>
        public static Tuple<int, int, int> GetStaticForLoopValues(this AST.ForStatement self)
        {
            int start, end, incr;

            if (!(self.Initializer is AST.PrimitiveExpression) && !(self.Initializer.GetTarget() is Constant))
                throw new Exception($"Unable to statically expand loop initializer: {self.Initializer.SourceExpression}");
            if (!(self.Condition is AST.BinaryOperatorExpression))
                throw new Exception($"Unable to statically expand loop initializer: {self.Condition.SourceExpression}");
            if (((BinaryOperatorExpression)self.Condition).Operator != SyntaxKind.LessThanToken)
                throw new Exception($"Can only statically expand loops with a less-than operator: {self.Condition.SourceExpression}");

            start = ResolveIntegerValue(self.Initializer);

            var cond_left = ((BinaryOperatorExpression)self.Condition).Left.GetUnwrapped();
            var cond_right = ((BinaryOperatorExpression)self.Condition).Right.GetUnwrapped();
            if (cond_left.GetTarget() != self.LoopIndex)
                throw new Exception($"Can only statically expand loops where the left side of the condition is the loop variable");

            if (cond_right is PrimitiveExpression || cond_right.GetTarget() != null)
                end = ResolveIntegerValue(cond_right);
            else if (cond_right is BinaryOperatorExpression)
            {
                var boe = cond_right as BinaryOperatorExpression;
                if (boe.Operator != SyntaxKind.PlusToken && boe.Operator != SyntaxKind.MinusToken )
                    throw new Exception($"Can only statically expand loops if the condition is a simple add/subtract operation: {boe.SourceExpression}");

                var lefttarget = boe.Left.GetTarget();
                if (lefttarget == null)
                    throw new Exception($"Can only statically expand loops if the condition is a simple add/subtract operation: {boe.SourceExpression}");

                var left_opr = ResolveIntegerValue(boe.Left);
                var right_opr = ResolveIntegerValue(boe.Right);

                if (boe.Operator == SyntaxKind.PlusToken)
                    end = left_opr + right_opr;
                else
                    end = left_opr - right_opr;
            }
            else
                throw new Exception($"Can only statically expand loops if the condition is a simple add/subtract operation: {self.Condition.SourceExpression}");

            if (self.Increment is UnaryOperatorExpression)
            {
                var uoe = self.Increment as UnaryOperatorExpression;
                if (uoe.Operand.GetTarget() != self.LoopIndex)
                    throw new Exception($"The item in the loop increment must be the loop variable: {self.Increment.SourceExpression}");
                if (uoe.Operator != SyntaxKind.PlusPlusToken)
                    throw new Exception($"The item in the loop increment must be the loop variable with post increment: {self.Increment.SourceExpression}");
                incr = 1;
            }
            else if (self.Increment is AssignmentExpression)
            {
                var boe = self.Increment as AssignmentExpression;
                if (boe.Left.GetTarget() != self.LoopIndex)
                    throw new Exception($"The item in the loop increment must be the loop variable: {self.Increment.SourceExpression}");
                if (boe.Operator == SyntaxKind.EqualsToken && boe.Right.GetUnwrapped() is BinaryOperatorExpression)
                {
                    var boee = boe.Right.GetUnwrapped() as BinaryOperatorExpression;
                    if (boee.Operator != SyntaxKind.PlusToken)
                        throw new Exception($"The item in the loop increment must be a simple addition: {self.Increment.SourceExpression}");
                    if (boee.Left.GetTarget() != self.LoopIndex)
                        throw new Exception($"The item in the loop increment must be the loop variable: {self.Increment.SourceExpression}");

                    incr = ResolveIntegerValue(boee.Right);
                }
                else
                {
                    if (boe.Operator != SyntaxKind.PlusToken)
                        throw new Exception($"The item in the loop increment must be a simple addition: {self.Increment.SourceExpression}");
                    incr = ResolveIntegerValue(boe.Right);
                }
            }
            else
                throw new Exception($"Can only statically expand loops if the increment is a simple constant: {self.Condition.SourceExpression}");

            return new Tuple<int, int, int>(start, end, incr);
        }
    }
}
