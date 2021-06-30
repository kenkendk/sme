using System;
using System.Collections.Generic;
using SME.AST;
using SME.AST.Transform;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SME.VHDL.Transformations
{
    /// <summary>
    /// Injects VHDL type conversions to ensure the output
    /// is valid VHDL.
    /// </summary>
    public class InjectTypeConversions : IASTTransform
    {
        /// <summary>
        /// The render state.
        /// </summary>.
        private readonly RenderState State;
        /// <summary>
        /// The method being compiled.
        /// </summary>
        private readonly Method Method;

        /// <summary>
        /// Lookup table with expressions already processed.
        /// </summary>
        private readonly Dictionary<ASTItem, string> m_done = new Dictionary<ASTItem, string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SME.VHDL.Transformations.InjectTypeConversions"/> class.
        /// </summary>
        /// <param name="state">The render state.</param>
        /// <param name="method">The method being rendered.</param>
        public InjectTypeConversions(RenderState state, Method method)
        {
            State = state;
            Method = method;
        }

        /// <summary>
        /// Applies the transformation.
        /// </summary>
        /// <returns>The transformed item.</returns>
        /// <param name="el">The item to visit.</param>
        public ASTItem Transform(ASTItem el)
        {
            if (m_done.ContainsKey(el))
                return el;

            var res = el;
            if (el is AST.BinaryOperatorExpression)
            {
                var boe = ((AST.BinaryOperatorExpression)el);
                var tvhdl = State.VHDLType(boe);

                var le = boe.Left;
                var re = boe.Right;

                var le_type = State.VHDLType(le);
                var re_type = State.VHDLType(re);

                ITypeSymbol tvhdlsource = boe.SourceResultType;

                if ((boe.Operator.IsArithmeticOperator() || boe.Operator.IsCompareOperator()))
                {
                    if (tvhdl != VHDLTypes.INTEGER)
                        tvhdl = State.TypeScope.NumericEquivalent(tvhdl, false) ?? tvhdl;
                }
                else if (boe.Operator == SyntaxKind.LessThanLessThanToken || boe.Operator == SyntaxKind.GreaterThanGreaterThanToken)
                {
                    // Handle later
                }
                else if (boe.Operator.IsBitwiseOperator())
                    tvhdl = State.TypeScope.SystemEquivalent(tvhdl);


                var lhstype = le_type;
                var rhstype = re_type;

                var lhssource = boe.Left.SourceResultType;
                var rhssource = boe.Right.SourceResultType;

                if (boe.Operator.IsLogicalOperator())
                {
                    // Force both sides to booleans
                    lhstype = VHDLTypes.BOOL;
                    rhstype = VHDLTypes.BOOL;
                    lhssource = rhssource = boe.SourceResultType.LoadType(typeof(bool));
                }
                else if (boe.Operator.IsArithmeticOperator() || boe.Operator.IsBitwiseOperator())
                {
                    // Force both sides to the result type
                    lhstype = tvhdl;
                    rhstype = tvhdl;
                }
                else if (boe.Operator.IsCompareOperator())
                {
                    // Use whatever is not unconstrained integer for the compare
                    if (lhstype == VHDLTypes.INTEGER)
                        lhstype = rhstype;
                    else
                        rhstype = lhstype;
                }

                if (boe.Operator == SyntaxKind.LessThanLessThanToken || boe.Operator == SyntaxKind.GreaterThanGreaterThanToken)
                {
                    rhstype = VHDLTypes.INTEGER;
                    if (lhstype == VHDLTypes.INTEGER)
                    {
                        var p = boe.Parent;
                        while (p is AST.ParenthesizedExpression)
                            p = p.Parent;

                        lhstype = State.VHDLType(p as AST.Expression);
                        lhssource = ((AST.Expression)p).SourceResultType;
                    }
                    else
                        lhstype = State.TypeScope.NumericEquivalent(lhstype);
                }

                // Overrides for special types
                switch (boe.Operator)
                {
                case SyntaxKind.LessThanLessThanToken:
                case SyntaxKind.GreaterThanGreaterThanToken:
                    rhssource = boe.SourceResultType.LoadType(typeof(int));
                    rhstype = VHDLTypes.INTEGER;
                    break;
                }

                var newleft = VHDLTypeConversion.ConvertExpression(State, Method, boe.Left, lhstype, lhssource, false);
                var newright = VHDLTypeConversion.ConvertExpression(State, Method, boe.Right, rhstype, rhssource, false);

                if (boe.Operator.IsLogicalOperator() || boe.Operator.IsCompareOperator())
                    State.TypeLookup[boe] = VHDLTypes.BOOL;
                else
                    State.TypeLookup[boe] = tvhdl;

                if (boe.Operator == SyntaxKind.AsteriskToken && tvhdl != VHDLTypes.INTEGER)
                {
                    VHDLTypeConversion.WrapExpression(State, boe, string.Format("resize({0}, {1})", "{0}", tvhdl.Length), tvhdl);
                    m_done[boe] = string.Empty;
                    return null;
                }

                if (newleft != le || newright != re)
                    return null;
            }
            else if (el is AST.AssignmentExpression)
            {
                var ase = ((AST.AssignmentExpression)el);
                var tvhdl = State.VHDLType(ase.Left);

                var r = ase.Right;
                ase.SourceResultType = ase.Left.SourceResultType;
                var n = VHDLTypeConversion.ConvertExpression(State, Method, ase.Right, tvhdl, ase.Left.SourceResultType, false);
                State.TypeLookup[ase] = State.TypeLookup[n] = tvhdl;
                if (n != r)
                    return null;
            }
            else if (el is AST.CastExpression)
            {
                var cse = ((AST.CastExpression)el);
                var tvhdl = State.VHDLType(cse);

                if (cse.Parent is AST.BinaryOperatorExpression)
                {
                    var pboe = cse.Parent as AST.BinaryOperatorExpression;
                    if (pboe.Right == cse && (pboe.Operator == SyntaxKind.LessThanLessThanToken || pboe.Operator == SyntaxKind.GreaterThanGreaterThanToken))
                        tvhdl = VHDLTypes.INTEGER;
                }

                res = cse.ReplaceWith(
                    VHDLTypeConversion.ConvertExpression(State, Method, cse.Expression, tvhdl, cse.SourceResultType, true)
                );
                State.TypeLookup[cse] = State.TypeLookup[res] = tvhdl;
                return res;
            }
            else if (el is AST.IndexerExpression)
            {
                var ie = ((AST.IndexerExpression)el);
                var tvhdl = VHDLTypes.INTEGER;
                var r = ie.IndexExpression;
                var n = VHDLTypeConversion.ConvertExpression(State, Method, ie.IndexExpression, tvhdl, ie.SourceResultType.LoadType(typeof(int)), false);
                State.TypeLookup[n] = tvhdl;
                State.TypeLookup[ie] = State.TypeScope.GetByName(State.VHDLType(ie.Target).ElementName);
                if (n != r)
                    return null;
            }
            else if (el is AST.IfElseStatement)
            {
                var ies = el as AST.IfElseStatement;
                var tvhdl = VHDLTypes.BOOL;
                var r = ies.Condition;
                var n = VHDLTypeConversion.ConvertExpression(State, Method, ies.Condition, tvhdl, r.SourceResultType.LoadType(typeof(bool)), false);
                State.TypeLookup[n] = tvhdl;
                if (n != r)
                    return null;
            }
            else if (el is AST.UnaryOperatorExpression)
            {
                var uoe = el as AST.UnaryOperatorExpression;
                if (uoe.Operator == SyntaxKind.ExclamationToken)
                {
                    var tvhdl = VHDLTypes.BOOL;
                    var n = VHDLTypeConversion.ConvertExpression(State, Method, uoe.Operand, tvhdl, uoe.SourceResultType.LoadType(typeof(bool)), false);
                    State.TypeLookup[n] = tvhdl;
                    if (n != uoe.Operand)
                        return null;
                }
            }
            else if (el is AST.InvocationExpression)
            {
                var ie = el as AST.InvocationExpression;
                var method = (AST.Method)ie.Target;
                var changed = false;

                for (var i = 0; i < ie.ArgumentExpressions.Length; i++)
                {
                    var a = ie.ArgumentExpressions[i];
                    if (a is AST.PrimitiveExpression)
                    {
                        var tvhdl = State.TypeScope.GetVHDLType(method.Parameters[i].MSCAType);
                        changed |= VHDLTypeConversion.ConvertExpression(State, Method, a, tvhdl, a.SourceResultType, false) != a;
                    }
                }
            }

            return res;
        }
    }
}
