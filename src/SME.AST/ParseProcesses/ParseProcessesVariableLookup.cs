using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SME.AST
{
    // This partial deals with finding variables in the current scope
    public partial class ParseProcesses
    {
        // TODO check if the static constant nameclash still exists: https://github.com/kenkendk/sme/commit/0d919ef8cf440f36f9c15261ebc62819f1387c03#

        /// <summary>
        /// Locates the target for an expression, and throws an exception if not found.
        /// </summary>
        /// <returns>The data element.</returns>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method where the statement is found.</param>
        /// <param name="statement">The statement where the expression is found.</param>
        /// <param name="expression">The expression to examine.</param>
        protected virtual DataElement LocateDataElement(NetworkState network, ProcessState proc, MethodState method, Statement statement, ExpressionSyntax expression)
        {
            var res = TryLocateDataElement(network, proc, method, statement, expression);
            if (res == null)
                throw new Exception($"Unable to locate item for {expression}");

            return res;
        }

        /// <summary>
        /// Locates the target for an expression.
        /// </summary>
        /// <returns>The data element.</returns>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method where the statement is found.</param>
        /// <param name="statement">The statement where the expression is found.</param>
        /// <param name="expression">The expression to examine.</param>
        protected ASTItem TryLocateElement(NetworkState network, ProcessState proc, MethodState method, Statement statement, ExpressionSyntax expression)
        {
            if (expression is InvocationExpressionSyntax)
            {
                var e = expression as InvocationExpressionSyntax;
                var target = e.Expression;
                return LocateDataElement(network, proc, method, statement, target);
            }
            else if (expression is ElementAccessExpressionSyntax)
            {
                var e = expression as ElementAccessExpressionSyntax;
                var target = e.Expression;
                return LocateDataElement(network, proc, method, statement, target);
            }
            else if (expression is IdentifierNameSyntax)
            {
                var e = expression as IdentifierNameSyntax;
                var name = e.Identifier.Text;

                Variable variable;
                if (method != null && method.TryGetVariable(name, out variable))
                    return variable;

                if (method != null)
                {
                    var p = method.Parameters.FirstOrDefault(x => x.Name == name);
                    if (p != null)
                        return p;
                }

                if (proc.Variables.TryGetValue(name, out variable))
                    return variable;

                Constant constant;
                if (proc.Constants.TryGetValue(name, out constant))
                    return constant;

                constant = network.ConstantLookup.Values.FirstOrDefault(x => x.Name.Equals(name));
                if (constant != null)
                    return constant;

                Signal signal;
                if (proc != null && proc.Signals.TryGetValue(name, out signal))
                    return signal;

                Bus bus;
                if (proc != null && proc.BusInstances.TryGetValue(name, out bus))
                    return bus;

                var constsymbol = m_compilation.GetSymbolsWithName(name).FirstOrDefault() as IFieldSymbol;
                if (constsymbol != null && constsymbol.IsStatic && constsymbol.HasConstantValue)
                {
                    var global_constant = new Constant()
                    {
                        DefaultValue = constsymbol.ConstantValue,
                        MSCAType = constsymbol.Type,
                        Name = name,
                        Source = constsymbol,
                        Parent = network
                    };
                    network.ConstantLookup.Add(new Tuple<ProcessState, IFieldSymbol>(proc, constsymbol), global_constant);
                    return global_constant;
                }

                return null;
            }
            else if (expression is MemberAccessExpressionSyntax)
            {
                var e = expression as MemberAccessExpressionSyntax;

                ASTItem current = null;

                var parts = new List<string>();
                ExpressionSyntax ec = e;
                while (ec != null)
                {
                    if (ec is MemberAccessExpressionSyntax)
                    {
                        parts.Add(((MemberAccessExpressionSyntax)ec).Name.Identifier.Text);
                        ec = ((MemberAccessExpressionSyntax)ec).Expression;
                    }
                    else if (ec is ThisExpressionSyntax)
                    {
                        ec = null;
                        break;
                    }
                    else if (ec is BaseExpressionSyntax)
                    {
                        ec = null;
                        break;
                    }
                    else if (ec is IdentifierNameSyntax)
                    {
                        var ins = ec as IdentifierNameSyntax;
                        var ecs = ins.Identifier.Text;
                        var targetproc = network.Processes
                            .FirstOrDefault(x =>
                                x.MSCAType.Name.Equals(ecs) ||
                                x.MSCAType.ToDisplayString().Equals(ecs)
                            );
                        if (targetproc != null)
                        {
                            // This is a static reference
                            current = targetproc;
                            break;
                        }

                        parts.Add(ecs);
                        ec = null;
                        break;
                    }
                    else if (ec is ElementAccessExpressionSyntax)
                    {
                        // For now, an element access within a member access is only supported for buses.
                        // In the future, this should be extended to support structs as well.
                        // TODO Handle this properly, it is an array of buses, so it should lookup the type of the field, and return it.
                        var eae = ec as ElementAccessExpressionSyntax;
                        var ecs = eae.Expression as IdentifierNameSyntax;
                        var idxs = eae.ArgumentList.Arguments.Select(x => "0"); // Hardcoded to 0, as we are after the type, not the value
                        // Otherwise, lookup the index variable in the constants. If it is not found, just go with 0.
                        ec = eae.Expression;
                    }
                    else if (ec is TypeSyntax)
                    {
                        ISymbol dc = method?.MSCAMethod.LoadSymbol(m_semantics) ?? proc?.MSCAType;

                        var ecs = ec.ToString();

                        if (dc != null)
                        {
                            var bt = LoadTypeByName(ecs);
                            if (bt == null)
                                bt = LoadTypeByName(dc.ToDisplayString() + "." + ecs);
                            if (bt == null)
                                bt = LoadTypeByName(dc.ContainingNamespace.ToDisplayString() + "." + ecs);
                            if (bt == null && proc != null && proc.SourceType != null)
                                bt = LoadTypeByName(proc.SourceType.Namespace + "." + ecs);

                            if (bt != null && parts.Count == 1)
                            {
                                var br = bt.ContainingType;
                                var px = br.GetMembers().OfType<IFieldSymbol>().FirstOrDefault(x => x.Name.Equals(parts[0]));

                                // Enum flags are encoded as constants
                                if (br.EnumUnderlyingType != null)
                                {
                                    if (px == null)
                                        throw new Exception($"Unable to find enum value {parts[0]} in {br.ToDisplayString()}");

                                    return new Constant()
                                    {
                                        MSCAType = bt,
                                        DefaultValue = px,
                                        Source = expression
                                    };
                                }
                                else
                                {
                                    // This is a constant of sorts
                                    var pe = network.ConstantLookup.Keys.FirstOrDefault(x => x.Item2.Name.Equals(parts[0]));
                                    if (pe != null)
                                        return network.ConstantLookup[pe];

                                    return network.ConstantLookup[new Tuple<ProcessState, IFieldSymbol>(proc, px)] = new Constant()
                                    {
                                        MSCAType = px.ContainingType,
                                        Name = parts[0],
                                        Source = px,
                                        Parent = network
                                    };
                                }
                            }

                            break;
                        }

                        // Likely a static reference, which is stored as a global constant
                        ec = null;
                    }
                    else
                    {
                        throw new Exception($"Unexpected element in reference chain: {ec.GetType().FullName}");
                    }
                }

                parts.Reverse();
                var fullname = string.Join(".", parts);


                if (parts.First() == "this")
                {
                    parts.RemoveAt(0);
                    if (proc == null)
                        throw new Exception("Attempting to do a resolve of \this\" but no process context is provided");
                    current = proc;
                }

                // Shortcut for getting array of buses length
                if (proc.Constants.ContainsKey(fullname))
                    return proc.Constants[fullname];

                var first = true;
                foreach (var el in parts)
                {
                    var isIsFirst = first;
                    first = false;

                    if (current == null)
                    {
                        var pe = network.ConstantLookup.Keys.FirstOrDefault(x => x.Item2.Name.Equals(el));
                        if (pe != null)
                        {
                            current = network.ConstantLookup[pe];
                            continue;
                        }
                    }

                    if (current is MethodState || (isIsFirst && current == null))
                    {
                        var mt = current as MethodState ?? method;
                        if (mt != null)
                        {
                            Variable temp;
                            if (mt.TryGetVariable(el, out temp))
                            {
                                current = temp;
                                continue;
                            }

                            var p = mt.Parameters.FirstOrDefault(x => x.Name.Equals(el));
                            if (p != null)
                            {
                                current = p;
                                continue;
                            }

                            if (mt.ReturnVariable != null && !string.IsNullOrWhiteSpace(mt.ReturnVariable.Name) && el == mt.ReturnVariable.Name)
                            {
                                current = mt.ReturnVariable;
                                continue;
                            }
                        }
                    }

                    if (current is ProcessState || (isIsFirst && current == null))
                    {
                        var pr = current as ProcessState ?? proc;

                        if (pr != null)
                        {
                            if (pr.BusInstances.ContainsKey(el))
                            {
                                current = pr.BusInstances[el];
                                continue;
                            }

                            if (pr.Constants.ContainsKey(el))
                            {
                                current = pr.Constants[el];
                                continue;
                            }

                            if (pr.Signals.ContainsKey(el))
                            {
                                current = pr.Signals[el];
                                continue;
                            }

                            if (pr.Variables.ContainsKey(el))
                            {
                                current = pr.Variables[el];
                                continue;
                            }

                            if (pr.Methods != null)
                            {
                                var p = pr.Methods.FirstOrDefault(x => x.Name.Equals(el));
                                if (p != null)
                                {
                                    current = p;
                                    continue;
                                }
                            }
                        }
                    }

                    if (current is Bus)
                    {
                        current = ((Bus)current).Signals.FirstOrDefault(x => x.Name.Equals(el));
                        if (current != null)
                            continue;
                    }

                    if (current is Variable)
                    {
                        var vc = current as Variable;
                        var fi = vc.MSCAType.GetMembers().OfType<IFieldSymbol>().FirstOrDefault(x => x.Name.Equals(el));
                        if (fi != null)
                        {
                            current = new Variable()
                            {
                                Name = el,
                                Parent = current,
                                Source = fi,
                                MSCAType = fi.ContainingType,
                                DefaultValue = null
                            };

                            continue;
                        }

                        var pi = vc.MSCAType.GetMembers().OfType<IPropertySymbol>().FirstOrDefault(x => x.Name.Equals(el));
                        if (pi != null)
                        {
                            current = new Variable()
                            {
                                Name = el,
                                Parent = current,
                                Source = pi,
                                MSCAType = pi.ContainingType,
                                DefaultValue = null
                            };

                            continue;
                        }
                    }

                    if (el == "Length" && (current is DataElement) && ((DataElement)current).MSCAType.IsArrayType())
                    {
                        return new Constant()
                        {
                            ArrayLengthSource = current as DataElement,
                            MSCAType = LoadType(typeof(int)),
                            DefaultValue = null,
                            Source = (current as DataElement).Source
                        };
                    }

                    var sy = m_compilation.GetSymbolsWithName(el).FirstOrDefault() as ITypeSymbol;
                    var px = sy.GetMembers().OfType<IFieldSymbol>().FirstOrDefault(x => x.Name.Equals(parts.Last()));
                    if (sy != null && sy.IsEnum())
                        return new Constant()
                        {
                            MSCAType = px.Type,
                            DefaultValue = px,
                            Source = expression
                        };

                    throw new Exception($"Failed lookup at {el} in {fullname}");
                }

                if (current == null)
                    throw new Exception($"Failed to fully resolve {fullname}");

                return current;
            }
            else
            {
                throw new Exception($"Unable to find a data element for an expression of type {expression.GetType().FullName}");
            }
        }

        /// <summary>
        /// Locates the target for an expression.
        /// </summary>
        /// <returns>The data element.</returns>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method where the statement is found.</param>
        /// <param name="statement">The statement where the expression is found.</param>
        /// <param name="expression">The expression to examine.</param>
        protected DataElement TryLocateDataElement(NetworkState network, ProcessState proc, MethodState method, Statement statement, ExpressionSyntax expression)
        {
            var el = TryLocateElement(network, proc, method, statement, expression);
            if (el == null)
                return null;

            if (!(el is DataElement))
                throw new Exception($"Failed to fully resolve {expression.ToString()}, got a result of type {el.GetType().FullName}");

            return (DataElement)el;
        }

        /// <summary>
        /// Registers a temporary variable for use within the method.
        /// </summary>
        /// <returns>The temporary variable.</returns>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method to create the variable in.</param>
        /// <param name="vartype">The data type of the variable.</param>
        /// <param name="source">The source of the variable.</param>
        protected virtual Variable RegisterTemporaryVariable(NetworkState network, ProcessState proc, MethodState method, ITypeSymbol vartype, object source)
        {
            var varname = "tmpvar_" + (network.VariableCount++).ToString();
            var res = new Variable()
            {
                MSCAType = vartype,
                Name = varname,
                Source = source,
                Parent = (ASTItem)method ?? proc
            };

            return method.AddVariable(res);
        }

        /// <summary>
        /// Parses a a field reference and returns the associated variable.
        /// </summary>
        /// <returns>The constant element.</returns>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method where the initializer is found</param>
        /// <param name="vartype">The variable type.</param>
        /// <param name="variable">The field to parse.</param>
        protected virtual DataElement RegisterVariable(NetworkState network, ProcessState proc, MethodState method, ITypeSymbol vartype, VariableDeclaratorSyntax variable)
        {
            var c = new Variable()
            {
                MSCAType = vartype,
                Name = variable.Identifier.Text,
                DefaultValue = variable.Initializer,
                Source = variable,
                Parent = (ASTItem)method ?? proc
            };

            return method.AddVariable(c);
        }

        /// <summary>
        /// Parses a a field reference and returns the associated bus.
        /// </summary>
        /// <returns>The constant element.</returns>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="field">The field to parse.</param>
        protected virtual void RegisterBusReference(NetworkState network, ProcessState proc, IFieldSymbol field)
        {
            var fd = proc.SourceType.GetField(field.Name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.FlattenHierarchy);
            if (fd == null)
                throw new Exception($"No such field: {field.Name} on {proc.SourceType.FullName}");

            var fdinstance = fd.GetValue(proc.SourceInstance.Instance);
            if (fdinstance == null)
                return;
            var businstance = fdinstance.GetType().IsArray ? fdinstance as IBus[] : new IBus[] {fdinstance as IBus};

            var allBusses = proc.InputBusses.Concat(proc.OutputBusses).Concat(proc.InternalBusses);

            var bus = allBusses.FirstOrDefault(x => x.SourceInstances.Zip(businstance).All(y => y.First == y.Second));
                if (bus == null)
                    throw new Exception($"No such bus: {field.ToDisplayString()}");

            proc.BusInstances.Add(field.Name, bus);

            if (bus.SourceInstances.Length > 1 || fd.FieldType.IsArray)
            {
                // Add a constant to the process for the length of the array
                var l = new Constant {
                    MSCAType = m_compilation.GetSpecialType(SpecialType.System_Int32), DefaultValue = bus.SourceInstances.Length,
                    Name = $"{field.Name}.Length",
                    Source = field,
                    Parent = proc
                };
                proc.Constants.Add($"{field.Name}.Length", l);
            }
        }

        /// <summary>
        /// Parses a a field reference and returns the associated variable.
        /// </summary>
        /// <returns>The constant element.</returns>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="field">The field to parse.</param>
        protected virtual DataElement RegisterVariable(NetworkState network, ProcessState proc, IFieldSymbol field)
        {
            DataElement res;

            var mscatype = proc.ResolveGenericType(field.Type);
            object defaultvalue = null;
            proc.SourceInstance.Initialization.TryGetValue(field.Name, out defaultvalue);
            defaultvalue = field.ConstantValue ?? defaultvalue;

            if (field.HasConstantValue || field.IsReadOnly)
            {
                var c = new Constant() {
                    MSCAType = mscatype,
                    DefaultValue = defaultvalue,
                    Name = field.Name,
                    Source = field,
                    Parent = proc
                };
                res = c;
                if (field.DeclaredAccessibility == Accessibility.Public)
                    network.ConstantLookup.Add(new Tuple<ProcessState, IFieldSymbol>(proc, field), c);
                else
                    proc.Constants.Add(field.Name, c);
            }
            else if (field.IsStatic)
            {
                res = null;
            }
            else if (!field.GetAttributes().Any(x => Type.GetType(x.AttributeClass.ToDisplayString()) == typeof(Signal)))
            {
                var c = new Variable()
                {
                    MSCAType = mscatype,
                    DefaultValue = defaultvalue,
                    Name = field.Name,
                    Source = field,
                    Type = null,
                    Parent = proc
                };
                res = c;
                proc.Variables.Add(field.Name, c);
            }
            else
            {
                var c = new Signal()
                {
                    MSCAType = mscatype,
                    DefaultValue = defaultvalue,
                    Name = field.Name,
                    Source = field,
                    Type = null,
                    Parent = proc
                };
                res = c;
                proc.Signals.Add(field.Name, c);
            }

            return res;
        }

        /// <summary>
        /// Parses a a parameter reference and returns a new AST reference.
        /// </summary>
        /// <returns>The constant element.</returns>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method the parameter belongs to.</param>
        /// <param name="parameter">The parameter to parse.</param>
        protected virtual Parameter ParseParameter(NetworkState network, ProcessState proc, MethodState method, ParameterSyntax parameter)
        {
            return new Parameter()
            {
                MSCAType = LoadType(parameter.Type),
                Name = parameter.Identifier.Text,
                DefaultValue = null,
                Source = parameter.LoadSymbol(m_semantics),
                Parent = method
            };
        }

        /// <summary>
        /// Sets the default value for a field.
        /// </summary>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="item">The data element to set the default value for.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="is_static">A flag indicating if the variable default is statically defined.</param>
        protected virtual void SetDataElementDefaultValue(NetworkState network, ProcessState proc, DataElement item, object value, bool is_static)
        {
            item.DefaultValue = value;
        }

        /// <summary>
        /// Locates a bus by reference.
        /// </summary>
        /// <returns>The bus.</returns>
        /// <param name="network">The top-level network.</param>
        /// <param name="proc">The process where the method is located.</param>
        /// <param name="method">The method the expression is found.</param>
        /// <param name="expression">The expression used to initialize the bus.</param>
        protected virtual Bus LocateBus(NetworkState network, ProcessState proc, MethodState method, ExpressionSyntax expression)
        {
            var de = TryLocateElement(network, proc, method, null, expression);
            var det = de.GetType();
            if (det.IsArray && det.GetElementType() == typeof(Bus))
                return de as Bus;

            throw new Exception("Need to walk the tree?");
        }
    }
}
