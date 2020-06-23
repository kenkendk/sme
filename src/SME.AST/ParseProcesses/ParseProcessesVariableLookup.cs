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

				Signal signal;
				if (proc != null && proc.Signals.TryGetValue(name, out signal))
					return signal;

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
						//parts.Add("this");
						ec = null;
						break;
					}
					else if (ec is BaseExpressionSyntax)
					{
						//parts.Add("base");
						ec = null;
						break;
					}
					else if (ec is IdentifierNameSyntax)
					{
						parts.Add(((IdentifierNameSyntax)ec).Identifier.Text);
						ec = null;
						break;
					}
					else if (ec is TypeSyntax) // ICSharpCode.Decompiler.CSharp.Syntax.TypeReferenceExpression)
					{
						//TypeDefinition dc = null;
						ISymbol dc = null;
						if (method != null)
							dc = m_semantics.Select(x => x.GetDeclaredSymbol(method.MSCAMethod)).Where(x => x != null).First();
						else if (proc != null)
							dc = proc.MSCAType;

						var ecs = ec.ToString();

						if (dc != null)
						{
							if (ecs == dc.ToDisplayString() || ecs == dc.Name)
							{
								ec = null;
								//parts.Add("this");
								break;
							}

							var targetproc = network.Processes.FirstOrDefault(x => x.MSCAType.Name == ecs || x.MSCAType.ToDisplayString() == ecs);
							if (targetproc != null)
							{
								// This is a static reference
								current = null; //targetproc;
								break;
							}

							var bt = LoadTypeByName(ecs);
							if (bt == null)
								bt = LoadTypeByName(dc.ToDisplayString() + "." + ecs);
							if (bt == null)
								bt = LoadTypeByName(dc.ContainingNamespace.ToDisplayString() + "." + ecs);
                            // In some cases dc.Namespace is empty ...
                            if (bt == null && proc != null && proc.SourceType != null)
                                bt = LoadTypeByName(proc.SourceType.Namespace + "." + ecs);

							if (bt != null && parts.Count == 1)
							{
								var br = bt.ContainingType;
								var px = br.GetMembers().OfType<IFieldSymbol>().FirstOrDefault(x => x.Name == parts[0]);

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
									var pe = network.ConstantLookup.Keys.FirstOrDefault(x => x.Name == parts[0]);
									if (pe != null)
										return network.ConstantLookup[pe];

									return network.ConstantLookup[px] = new Constant()
									{
										MSCAType = px.ContainingType,
										Name = parts[0],
										Source = px,
										Parent = network
									};
								}

								//parts.AddRange(bt.FullName.Split('.').Reverse());
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

				var first = true;
				foreach (var el in parts)
				{
					var isIsFirst = first;
					first = false;

					if (current == null)
					{
						var pe = network.ConstantLookup.Keys.FirstOrDefault(x => x.Name == el);
						if (pe != null)
						{
							current = network.ConstantLookup[pe];
							continue;
						}
					}

					if (current is MethodState || (isIsFirst && current == null))
					{
						//if (method.LocalRenames.ContainsKey(el))
						//	el = method.LocalRenames[el];

						var mt = current as MethodState ?? method;
						if (mt != null)
						{
                            Variable temp;
                            if (mt.TryGetVariable(el, out temp))
							{
								current = temp;
								continue;
							}

							var p = mt.Parameters.FirstOrDefault(x => x.Name == el);
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
								var p = pr.Methods.FirstOrDefault(x => x.Name == el);
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
						current = ((Bus)current).Signals.FirstOrDefault(x => x.Name == el);
						if (current != null)
							continue;
					}

					if (current is Variable)
					{
						var fi = ((Variable)current).MSCAType.ContainingType.GetMembers().OfType<IFieldSymbol>().FirstOrDefault(x => x.Name == el);
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

						var pi = ((Variable)current).MSCAType.ContainingType.GetMembers().OfType<IPropertySymbol>().FirstOrDefault(x => x.Name == el);
						if (pi != null)
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
					}

					if (el == "Length" && (current is DataElement) && ((DataElement)current).MSCAType.ContainingType is IArrayTypeSymbol)
					{
						return new Constant()
						{
							ArrayLengthSource = current as DataElement,
							MSCAType = LoadType(typeof(int)),
							DefaultValue = null,
							Source = (current as DataElement).Source
						};
					}

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
		/// Registers a temporary variable for use within the method
		/// </summary>
		/// <returns>The temporary variable.</returns>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method to create the variable in.</param>
		/// <param name="vartype">The data type of the variable.</param>
		/// <param name="source">The source of the variable</param>
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
		/// Parses a a field reference and returns the associated variable
		/// </summary>
		/// <returns>The constant element.</returns>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method where the initializer is found</param>
		/// <param name="vartype">The variable type</param>
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
		/// Parses a a field reference and returns the associated bus
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

            var businstance = fd.GetValue(proc.SourceInstance.Instance);
            if (businstance == null)
                return;

            var allBusses = proc.InputBusses.Concat(proc.OutputBusses).Concat(proc.InternalBusses);
            if (businstance.GetType().IsArray)
            {
                var a = (Array)businstance;
                for (var i = 0; i < a.Length; i++)
                {
                    var v = a.GetValue(i);
                    var bus = allBusses.FirstOrDefault(x => x.SourceInstance == v);
                    if (bus == null)
                        throw new Exception($"No such bus: {field.ToDisplayString()}[{i}]");
                    proc.BusInstances.Add(field.Name + $"[{i}]", bus);
                }
            }
            else
            {
                var bus = allBusses.FirstOrDefault(x => x.SourceInstance == businstance);
                if (bus == null)
                    throw new Exception($"No such bus: {field.ToDisplayString()}");

                proc.BusInstances.Add(field.Name, bus);
            }
		}


		/// <summary>
		/// Parses a a field reference and returns the associated variable
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


			if (field.HasConstantValue)
			{
				var c = new Constant() {
					MSCAType = mscatype,
                    DefaultValue = defaultvalue,
					Name = field.Name,
					Source = field,
					Parent = proc
				};
				res = c;
				proc.Constants.Add(field.Name, c);
				network.ConstantLookup.Add(field, c);
			}
			// TODO jeg er ikke sikker på om .IsInitOnly er det samme som .IsReadOnly
			else if (field.IsStatic && field.IsReadOnly)
			{
				var c = new Constant()
				{
					MSCAType = mscatype,
                    DefaultValue = defaultvalue,
					Name = field.Name,
					Source = field,
					Parent = proc
				};
				res = c;
                network.ConstantLookup.Add(field, c);
			}
			else if (field.IsStatic)
			{
				//Don't care
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
		/// Parses a a parameter reference and returns a new AST reference
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
				MSCAType = m_semantics.Select(x => x.GetTypeInfo(parameter).Type).First(x => x != null),
				Name = parameter.Identifier.Text,
				DefaultValue = null,
				Source = parameter,
				Parent = method
			};
		}

		/// <summary>
		/// Sets the default value for a field
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
		/// Locates a bus by reference
		/// </summary>
		/// <returns>The bus.</returns>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method the expression is found.</param>
		/// <param name="expression">The expression used to initialize the bus.</param>
		protected virtual Bus LocateBus(NetworkState network, ProcessState proc, MethodState method, ExpressionSyntax expression)
		{
			var de = TryLocateElement(network, proc, method, null, expression);
			if (de is AST.Bus)
				return de as AST.Bus;

			throw new Exception("Need to walk the tree?");
		}
	}
}
