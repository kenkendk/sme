using System;
using System.Collections.Generic;
using System.Linq;
//using Mono.Cecil;
//using Mono.Cecil.Rocks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SME.AST
{
    // This partial part deals with finding the type from expressions
    public partial class ParseProcesses
    {
        /// <summary>
        /// Cache with typenames
        /// </summary>
        protected Dictionary<Type, ITypeSymbol> m_typelookup = new Dictionary<Type, ITypeSymbol>();
        /// <summary>
        /// Cache with assemblies
        /// </summary>
        //protected Dictionary<string, AssemblyDefinition> m_assemblies = new Dictionary<string, AssemblyDefinition>();
        // TODO compilation is used for looking up types...
        public static Compilation m_compilation;

        //protected SemanticModel m_semantic;
        protected IEnumerable<SyntaxTree> m_syntaxtrees;
        protected IEnumerable<SemanticModel> m_semantics;

        /// <summary>
        /// Examines the given expression and returns the resulting output type from the expression
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

                // Catch common translations
                // TODO jeg håber det bare var en decompile ting :)
                /*if (mt != null && (expression as InvocationExpressionSyntax).ArgumentList.Arguments.Count == 1)
                {
                    if (mt.TryGetInferredMemberName() == "op_Implicit" || mt.TryGetInferredMemberName() == "op_Explicit")
                    {
                        var mtm = Decompile(network, proc, method, statement, mt);
                        return ResolveExpressionType(network, proc, method, statement, new ICSharpCode.Decompiler.CSharp.Syntax.CastExpression(ICSharpCode.Decompiler.CSharp.Syntax.AstType.Create(mtm.SourceResultType.FullName), si.ArgumentList.Arguments.First().Clone()));
                    }
                    else if (mt.TryGetInferredMemberName() == "op_Increment")
                        return ResolveExpressionType(network, proc, method, statement, new ICSharpCode.Decompiler.CSharp.Syntax.UnaryOperatorExpression(ICSharpCode.Decompiler.CSharp.Syntax.UnaryOperatorType.Increment, si.Arguments.First().Clone()));
                    else if (mt.TryGetInferredMemberName() == "op_Decrement")
                        return ResolveExpressionType(network, proc, method, statement, new ICSharpCode.Decompiler.CSharp.Syntax.UnaryOperatorExpression(ICSharpCode.Decompiler.CSharp.Syntax.UnaryOperatorType.Decrement, si.Arguments.First().Clone()));
                }*/

                return ResolveExpressionType(network, proc, method, statement, (expression as InvocationExpressionSyntax).Expression);
            }
            else if (expression is ParenthesizedExpressionSyntax)
                return ResolveExpressionType(network, proc, method, statement, (expression as ParenthesizedExpressionSyntax).Expression);
            // TODO den er nu dækket af literalexpressionsyntax
            //else if (expression is ICSharpCode.Decompiler.CSharp.Syntax.NullReferenceExpression)
            //    return null;
            else if (expression is ArrayCreationExpressionSyntax)
                return ResolveExpressionType(network, proc, method, statement, (expression as ArrayCreationExpressionSyntax).Initializer.DescendantNodes().First() as ExpressionSyntax);
            else if (expression is CheckedExpressionSyntax)
                return ResolveExpressionType(network, proc, method, statement, (expression as CheckedExpressionSyntax).Expression);
            // TODO den er nu dækket af CheckedExpressionSyntax
            //else if (expression is ICSharpCode.Decompiler.CSharp.Syntax.UncheckedExpression)
            //    return ResolveExpressionType(network, proc, method, statement, (expression as ICSharpCode.Decompiler.CSharp.Syntax.UncheckedExpression).Expression);
            // TODO idk?
            //else if (expression == ICSharpCode.Decompiler.CSharp.Syntax.Expression.Null)
            //    return null;
            else
                throw new Exception(string.Format("Unsupported expression: {0} ({1})", expression, expression.GetType().FullName));
        }

        protected virtual IEnumerable<ITypeSymbol> LoadGenericTypes(Type t)
        {
            foreach (var p in t.GenericTypeArguments)
                yield return LoadType(p);
        }

        protected virtual ITypeSymbol LoadGenericType(Type t, ITypeParameterSymbol tps)
        {
            foreach (var p in t.GetGenericArguments())
                if (p.Name.Equals(tps.Name))
                    return LoadType(p);
            return null;
        }

        /// <summary>
        /// Loads the specified reflection Type and returns the equivalent CeCil TypeDefinition
        /// </summary>
        /// <returns>The loaded type.</returns>
        /// <param name="t">The type to load.</param>
        protected virtual ITypeSymbol LoadType(Type t)
        {
            if (m_typelookup.ContainsKey(t))
                return m_typelookup[t];

            /*AssemblyDefinition asm;
            m_assemblies.TryGetValue(t.Assembly.Location, out asm);
            if (asm == null)
                asm = m_assemblies[t.Assembly.Location] = AssemblyDefinition.ReadAssembly(t.Assembly.Location);

            if (asm == null)
                return null;

            var res = LoadTypeByName(t.FullName, asm.Modules);*/
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

        protected virtual ITypeSymbol LoadTypeByName(string name)
        {
            return m_compilation.GetTypeByMetadataName(name);
        }

        /*
        /// <summary>
        /// Loads the specified reflection Type and returns the equivalent CeCil TypeDefinition
        /// </summary>
        /// <returns>The loaded type.</returns>
        /// <param name="name">The full name of the type to load.</param>
        /// <param name="modules">The modules to look in</param>
        protected virtual TypeReference LoadTypeByName(string name, params ModuleDefinition[] modules)
        {
            return LoadTypeByName(name, modules.AsEnumerable());
        }

        /// <summary>
        /// Loads the specified reflection Type and returns the equivalent CeCil TypeDefinition
        /// </summary>
        /// <returns>The loaded type.</returns>
        /// <param name="name">The full name of the type to load.</param>
        /// <param name="modules">The modules to look in</param>
        protected virtual TypeReference LoadTypeByName(string name, IEnumerable<ModuleDefinition> modules)
        {
            var parts = name.Split('.', '+', '/').Reverse().ToArray();
            foreach (var e in modules.SelectMany(m => m.GetTypes()))
            {
                var c = e;
                var lastns = string.Empty;
                var failed = false;
                for (var i = 0; i < parts.Length - 1; i++)
                {
                    if (c != null && c.Name == parts[i])
                    {
                        lastns = c.Namespace;
                        c = c.DeclaringType;
                    }
                    else
                    {
                        if (c == null && lastns == string.Join(".", parts.Skip(i).Reverse()))
                            return e;

                        failed = true;
                        break;
                    }
                }

                if (!failed && c == null && lastns == parts[parts.Length - 1])
                    return e;

            }

            return null;
        }
        */

        /// <summary>
        /// Loads the specified AstType and returns the equivalent CeCil TypeDefinition
        /// </summary>
        /// <returns>The loaded type.</returns>
        /// <param name="t">The type to load.</param>
        protected virtual ITypeSymbol LoadType(TypeSyntax t, Method sourcemethod = null)
        {
            if (t is PredefinedTypeSyntax)
                switch (((PredefinedTypeSyntax)t).Keyword.Kind())
                {
                    case SyntaxKind.BoolKeyword: return LoadType(typeof(bool));
                    case SyntaxKind.ByteKeyword: return LoadType(typeof(byte));
                    case SyntaxKind.SByteKeyword: return LoadType(typeof(sbyte));
                    case SyntaxKind.ShortKeyword: return LoadType(typeof(short));
                    case SyntaxKind.UShortKeyword: return LoadType(typeof(ushort));
                    case SyntaxKind.IntKeyword: return LoadType(typeof(int));
                    case SyntaxKind.UIntKeyword: return LoadType(typeof(uint));
                    case SyntaxKind.LongKeyword: return LoadType(typeof(long));
                    case SyntaxKind.ULongKeyword: return LoadType(typeof(ulong));
                    case SyntaxKind.FloatKeyword: return LoadType(typeof(float));
                    case SyntaxKind.DoubleKeyword: return LoadType(typeof(double));
                    // TODO Det er fordi det er void fra en anden assembly... ffs
                    case SyntaxKind.VoidKeyword: return LoadType(typeof(void));
                }
            var res = t.LoadType(m_semantics);
            if (res == null)
                throw new Exception($"Failed to load {t.ToString()}");
            else
                return res;

            /*
            if (t is ICSharpCode.Decompiler.CSharp.Syntax.PrimitiveType)
                switch (((ICSharpCode.Decompiler.CSharp.Syntax.PrimitiveType)t).KnownTypeCode)
                {
                    case ICSharpCode.Decompiler.TypeSystem.KnownTypeCode.Boolean:
                        return LoadType(typeof(bool));
                    case ICSharpCode.Decompiler.TypeSystem.KnownTypeCode.Byte:
                        return LoadType(typeof(byte));
                    case ICSharpCode.Decompiler.TypeSystem.KnownTypeCode.SByte:
                        return LoadType(typeof(sbyte));
                    case ICSharpCode.Decompiler.TypeSystem.KnownTypeCode.Int16:
                        return LoadType(typeof(short));
                    case ICSharpCode.Decompiler.TypeSystem.KnownTypeCode.UInt16:
                        return LoadType(typeof(ushort));
                    case ICSharpCode.Decompiler.TypeSystem.KnownTypeCode.Int32:
                        return LoadType(typeof(int));
                    case ICSharpCode.Decompiler.TypeSystem.KnownTypeCode.UInt32:
                        return LoadType(typeof(uint));
                    case ICSharpCode.Decompiler.TypeSystem.KnownTypeCode.Int64:
                        return LoadType(typeof(long));
                    case ICSharpCode.Decompiler.TypeSystem.KnownTypeCode.UInt64:
                        return LoadType(typeof(ulong));
                    case ICSharpCode.Decompiler.TypeSystem.KnownTypeCode.Single:
                        return LoadType(typeof(float));
                    case ICSharpCode.Decompiler.TypeSystem.KnownTypeCode.Double:
                        return LoadType(typeof(double));
                }

            if (t is ICSharpCode.Decompiler.CSharp.Syntax.SimpleType)
            {
                var st = t as ICSharpCode.Decompiler.CSharp.Syntax.SimpleType;
                var typename = st.Identifier;

                var t0 = typeof(int).Assembly.GetType("System." + typename);
                if (t0 != null)
                    return LoadType(t0);
                if (sourcemethod != null)
                {
                    var t1 = LoadTypeByName(typename, sourcemethod.SourceMethod.Module);
                    if (t1 != null)
                        return t1;
                    t1 = LoadTypeByName(sourcemethod.SourceMethod.DeclaringType.FullName + "." + typename, sourcemethod.SourceMethod.Module);
                    if (t1 != null)
                        return t1;
                    t1 = LoadTypeByName(sourcemethod.SourceMethod.DeclaringType.Namespace + "." + typename, sourcemethod.SourceMethod.Module);
                    if (t1 != null)
                        return t1;

                    if (sourcemethod.Parent as Process != null)
                    {
                        // In some cases the namespace is empty
                        t1 = LoadTypeByName((sourcemethod.Parent as Process).SourceType.Namespace + "." + typename, sourcemethod.SourceMethod.Module);
                        if (t1 != null)
                            return t1;
                    }
                }
            }

            if (t is ICSharpCode.Decompiler.CSharp.Syntax.MemberType)
            {
                var mt = t as ICSharpCode.Decompiler.CSharp.Syntax.MemberType;
                var t0 = LoadTypeByName(mt.ToString(), sourcemethod.SourceMethod.Module);

                if (t0 != null)
                    return t0;

                t0 = LoadTypeByName(sourcemethod.SourceMethod.DeclaringType.Namespace + "." + mt.ToString(), sourcemethod.SourceMethod.Module);
                if (t0 != null)
                    return t0;
            }

            throw new Exception($"Failed to load {t.ToString()}"); */
        }


    }
}
