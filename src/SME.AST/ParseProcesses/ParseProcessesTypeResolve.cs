using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SME.AST
{
    // This partial part deals with finding the type from expressions
    public partial class ParseProcesses
    {
        /// <summary>
        /// Cache with type names
        /// </summary>
        protected Dictionary<Type, ITypeSymbol> m_typelookup = new Dictionary<Type, ITypeSymbol>();
        /// <summary>
        /// Compilation the process belongs to.
        /// </summary>
        public static Compilation m_compilation;
        /// <summary>
        /// Collection of syntax trees from the current compilation.
        /// </summary>
        protected IEnumerable<SyntaxTree> m_syntaxtrees;
        /// <summary>
        /// Collection of semantic models, which are derived from the syntax trees.
        /// </summary>
        protected IEnumerable<SemanticModel> m_semantics;

        /// <summary>
        /// Examines the given expression and returns the resulting output type from the expression.
        /// </summary>
        /// <returns>The expression type.</returns>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method where the statement is found.</param>
        /// <param name="statement">The statement where the expression is found.</param>
        /// <param name="expression">The expression to examine.</param>
        protected ITypeSymbol ResolveExpressionType(NetworkState network, ProcessState proc, MethodState method, Statement statement, ExpressionSyntax expression)
        {
            if (expression is AssignmentExpressionSyntax)
                return ResolveExpressionType(network, proc, method, statement, (expression as AssignmentExpressionSyntax).Left);
            else if (expression is IdentifierNameSyntax)
                return LocateDataElement(network, proc, method, statement, expression as IdentifierNameSyntax).MSCAType;
            else if (expression is MemberAccessExpressionSyntax)
            {
                var el = TryLocateElement(network, proc, method, statement, expression as MemberAccessExpressionSyntax);
                if (el == null)
                    throw new Exception($"Location failed for expression {expression}");
                else if (el is DataElement)
                    return ((DataElement)el).MSCAType;
                else if (el is Method)
                {
                    var rv = ((Method)el).ReturnVariable;
                    if (rv == null)
                        return null;

                    return rv.MSCAType;
                }
                else
                    throw new Exception($"Unexpected result for {expression} {el.GetType().FullName}");
            }
            else if (expression is LiteralExpressionSyntax)
                return LoadType((expression as LiteralExpressionSyntax).Token.Value.GetType());
            else if (expression is BinaryExpressionSyntax)
            {
                var e = expression as BinaryExpressionSyntax;
                var op = e.OperatorToken.Kind();
                if (op.IsCompareOperator() || op.IsLogicalOperator())
                    return LoadType(typeof(bool));

                var lefttype = ResolveExpressionType(network, proc, method, statement, e.Left);
                //var righttype = ResolveExpressionType(network, proc, method, statement, e.Right);

                if (op.IsArithmeticOperator() || op.IsBitwiseOperator())
                {
                    var righttype = ResolveExpressionType(network, proc, method, statement, e.Right);

                    // Custom resolve of the resulting type as dictated by the .Net rules
                    if (righttype.IsSameTypeReference<double>() || lefttype.IsSameTypeReference<double>())
                        return m_compilation.GetSpecialType(SpecialType.System_Double);
                    if (righttype.IsSameTypeReference<float>() || lefttype.IsSameTypeReference<float>())
                        return m_compilation.GetSpecialType(SpecialType.System_Single);
                    if (righttype.IsSameTypeReference<ulong>() || lefttype.IsSameTypeReference<ulong>())
                        return m_compilation.GetSpecialType(SpecialType.System_UInt64);
                    if (righttype.IsSameTypeReference<long>() || lefttype.IsSameTypeReference<long>())
                        return m_compilation.GetSpecialType(SpecialType.System_Int64);

                    if (righttype.IsSameTypeReference<uint>() || lefttype.IsSameTypeReference<uint>())
                    {
                        if (lefttype.IsSameTypeReference<sbyte>() || lefttype.IsSameTypeReference<short>() || righttype.IsSameTypeReference<sbyte>() || righttype.IsSameTypeReference<short>())
                            return m_compilation.GetSpecialType(SpecialType.System_Int64);
                        else
                            return m_compilation.GetSpecialType(SpecialType.System_UInt32);
                    }

                    if (righttype.IsSameTypeReference<int>() || lefttype.IsSameTypeReference<int>())
                    {
                        if (righttype.IsSameTypeReference<uint>() || lefttype.IsSameTypeReference<uint>())
                            return m_compilation.GetSpecialType(SpecialType.System_UInt32);
                        else
                            return m_compilation.GetSpecialType(SpecialType.System_Int32);
                    }

                    if(righttype.IsSameTypeReference<ushort>() || lefttype.IsSameTypeReference<ushort>())
                        return m_compilation.GetSpecialType(SpecialType.System_Int32);
                    if (righttype.IsSameTypeReference<short>() || lefttype.IsSameTypeReference<short>())
                        return m_compilation.GetSpecialType(SpecialType.System_Int32);
                    if (righttype.IsSameTypeReference<sbyte>() || lefttype.IsSameTypeReference<sbyte>())
                        return m_compilation.GetSpecialType(SpecialType.System_Int32);
                    if (righttype.IsSameTypeReference<byte>() || lefttype.IsSameTypeReference<byte>())
                        return m_compilation.GetSpecialType(SpecialType.System_Int32);


                    Console.WriteLine("Warning: unable to determine result type for operation {0} on types {1} and {2}", op, lefttype, righttype);

                    //TODO: Return a larger type, double the bits?
                    return lefttype;
                }
                else
                {
                    // TODO: Find the largest type?
                    return lefttype;
                }
            }
            else if (expression is PostfixUnaryExpressionSyntax)
                return ResolveExpressionType(network, proc, method, statement, (expression as PostfixUnaryExpressionSyntax).Operand);
            else if (expression is PrefixUnaryExpressionSyntax)
                return ResolveExpressionType(network, proc, method, statement, (expression as PrefixUnaryExpressionSyntax).Operand);
            else if (expression is ElementAccessExpressionSyntax)
            {
                var arraytype = ResolveExpressionType(network, proc, method, statement, (expression as ElementAccessExpressionSyntax).Expression);
                return arraytype.GetArrayElementType();
            }
            else if (expression is CastExpressionSyntax)
                return LoadType((expression as CastExpressionSyntax).Type, method);
            else if (expression is ConditionalExpressionSyntax)
                return ResolveExpressionType(network, proc, method, statement, (expression as ConditionalExpressionSyntax).WhenTrue);
            else if (expression is InvocationExpressionSyntax)
            {
                var si = expression as InvocationExpressionSyntax;
                string method_name = "";
                if (si.Expression is MemberAccessExpressionSyntax)
                {
                    var mt = si.Expression as MemberAccessExpressionSyntax;
                    method_name = mt.TryGetInferredMemberName();
                }
                if (si.Expression is IdentifierNameSyntax)
                    method_name = ((IdentifierNameSyntax)si.Expression).Identifier.ValueText;

                var proc_syntax = proc.MSCAType.DeclaringSyntaxReferences.FirstOrDefault().GetSyntax();
                var proc_decl = proc_syntax as ClassDeclarationSyntax;
                var members = proc_decl.Members.OfType<MethodDeclarationSyntax>();
                var m = members.FirstOrDefault(x => x.Identifier.ValueText.Equals(method_name));
                if (m != null)
                    return m_semantics.Select(x => x.GetTypeInfo(m).Type).FirstOrDefault(x => x != null);

                return ResolveExpressionType(network, proc, method, statement, (expression as InvocationExpressionSyntax).Expression);
            }
            else if (expression is ParenthesizedExpressionSyntax)
                return ResolveExpressionType(network, proc, method, statement, (expression as ParenthesizedExpressionSyntax).Expression);
            // TODO handle DirectionExpression (if they exist in roslyn)
            else if (expression is ArrayCreationExpressionSyntax)
                return ResolveExpressionType(network, proc, method, statement, (expression as ArrayCreationExpressionSyntax).Initializer.DescendantNodes().First() as ExpressionSyntax);
            else if (expression is CheckedExpressionSyntax)
                return ResolveExpressionType(network, proc, method, statement, (expression as CheckedExpressionSyntax).Expression);
            else
                throw new Exception(string.Format("Unsupported expression: {0} ({1})", expression, expression.GetType().FullName));
        }

        /// <summary>
        /// Loads the generic types of the given type.
        /// </summary>
        /// <param name="t">The type to find generics in.</param>
        protected virtual IEnumerable<ITypeSymbol> LoadGenericTypes(Type t)
        {
            foreach (var p in t.GenericTypeArguments)
                yield return LoadType(p);
        }

        /// <summary>
        /// Loads the generic type associated with the given generic type parameter.
        /// </summary>
        /// <param name="t">The loaded instance of the type.</param>
        /// <param name="tps">The type parameter to load the type of.</param>
        protected virtual ITypeSymbol LoadGenericType(Type t, ITypeParameterSymbol tps)
        {
            foreach (var p in t.GetGenericArguments())
                if (p.Name.Equals(tps.Name))
                    return LoadType(p);
            return null;
        }

        /// <summary>
        /// Loads the specified reflection Type and returns the equivalent Microsoft.CodeAnalysis TypeDefinition.
        /// </summary>
        /// <returns>The loaded type.</returns>
        /// <param name="t">The type to load.</param>
        protected virtual ITypeSymbol LoadType(Type t)
        {
            if (m_typelookup.ContainsKey(t))
                return m_typelookup[t];

            var res = LoadTypeByName(t.FullName);

            if (res == null && t.IsGenericType)
            {
                var gt = t.GetGenericTypeDefinition();

                res = LoadTypeByName(gt.FullName);

                if (res.IsArrayType())
                {
                    res = m_compilation.CreateArrayTypeSymbol(LoadType(t.GenericTypeArguments[0]));
                }
            }

            if (res == null && t.IsArray)
            {
                var el = t.GetElementType();
                res = m_compilation.CreateArrayTypeSymbol(LoadType(el));
            }

            if (res == null)
                //throw new Exception($"Failed to load {t.FullName}, the following types were found in the assembly: {string.Join(",", asm.Modules.SelectMany(x => x.GetTypes()).Select(x => x.FullName))}");
                throw new Exception($"Failed to load {t.FullName}");

            return m_typelookup[t] = res;
        }

        /// <summary>
        /// Loads the type corresponding to the given name.
        /// </summary>
        /// <param name="name">The name to lookup</param>
        protected virtual ITypeSymbol LoadTypeByName(string name)
        {
            return m_compilation.GetTypeByMetadataName(name);
        }

        /// <summary>
        /// Loads the specified AstType and returns the equivalent Microsoft.CodeAnalysis TypeDefinition.
        /// </summary>
        /// <returns>The loaded type.</returns>
        /// <param name="t">The type to load.</param>
        protected virtual ITypeSymbol LoadType(TypeSyntax t, Method sourcemethod = null)
        {
            if (t is PredefinedTypeSyntax)
                switch (((PredefinedTypeSyntax)t).Keyword.Kind())
                {
                    case SyntaxKind.BoolKeyword:   return LoadType(typeof(bool));
                    case SyntaxKind.ByteKeyword:   return LoadType(typeof(byte));
                    case SyntaxKind.SByteKeyword:  return LoadType(typeof(sbyte));
                    case SyntaxKind.ShortKeyword:  return LoadType(typeof(short));
                    case SyntaxKind.UShortKeyword: return LoadType(typeof(ushort));
                    case SyntaxKind.IntKeyword:    return LoadType(typeof(int));
                    case SyntaxKind.UIntKeyword:   return LoadType(typeof(uint));
                    case SyntaxKind.LongKeyword:   return LoadType(typeof(long));
                    case SyntaxKind.ULongKeyword:  return LoadType(typeof(ulong));
                    case SyntaxKind.FloatKeyword:  return LoadType(typeof(float));
                    case SyntaxKind.DoubleKeyword: return LoadType(typeof(double));
                    case SyntaxKind.VoidKeyword:   return LoadType(typeof(void));
                }
            var res = t.LoadType(m_semantics) ?? m_compilation.GetSymbolsWithName(t.ToString()).FirstOrDefault() as ITypeSymbol;

            if (res == null)
                throw new Exception($"Failed to load {t.ToString()}");
            else
                return res;
        }
    }
}
